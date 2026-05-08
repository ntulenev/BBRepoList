using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Logic;

/// <summary>
/// Loads repositories from Bitbucket with optional name filtering.
/// </summary>
public sealed class RepositoryService : IRepoService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryService"/> class.
    /// </summary>
    /// <param name="api">Bitbucket API client.</param>
    /// <param name="prApi">Bitbucket pull request API client.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryService(IBitbucketRepoApiClient api, IBitbucketPRApiClient prApi, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(prApi);
        ArgumentNullException.ThrowIfNull(options);

        _api = api;
        _prApi = prApi;
        _loadOpenPullRequestsStatistics = options.Value.LoadOpenPullRequestsStatistics;
        _openPullRequestsLoadThreshold = options.Value.OpenPullRequestsLoadThreshold;
        _loadPullRequestDetails = options.Value.PullRequestDetails.IsEnabled;
        _pullRequestDetailsLoadThreshold = options.Value.PullRequestDetails.LoadThreshold;
        _loadMergedPullRequests = options.Value.MergedPullRequests.IsEnabled;
        _mergedPullRequestsLoadThreshold = options.Value.MergedPullRequests.LoadThreshold;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var matchedRepositories = new List<Repository>();

        var seen = 0;
        var matched = 0;

        await foreach (var repository in _api.GetRepositoriesAsync(filterPattern, cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (filterPattern.Filter(repository))
            {
                matched++;
                matchedRepositories.Add(repository);
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        if (!_loadOpenPullRequestsStatistics || _loadPullRequestDetails)
        {
            return matchedRepositories;
        }

        if (matchedRepositories.Count == 0)
        {
            return matchedRepositories;
        }

        progress?.Report(new RepoLoadProgress(
            seen,
            matched,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 0,
            pullRequestStatisticsTotal: matchedRepositories.Count));

        var prStatisticsLoaded = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, matchedRepositories.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _openPullRequestsLoadThreshold,
                CancellationToken = cancellationToken
            },
            async (index, token) =>
            {
                await _prApi
                    .PopulateOpenPullRequestCountAsync(matchedRepositories[index], token)
                    .ConfigureAwait(false);

                var currentLoaded = Interlocked.Increment(ref prStatisticsLoaded);
                progress?.Report(new RepoLoadProgress(
                    seen,
                    matched,
                    isLoadingPullRequestStatistics: true,
                    pullRequestStatisticsLoaded: currentLoaded,
                    pullRequestStatisticsTotal: matchedRepositories.Count));
            }).ConfigureAwait(false);

        return matchedRepositories;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        IReadOnlyList<Repository> repositories,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (!_loadPullRequestDetails || repositories.Count == 0)
        {
            return [];
        }

        var pullRequestDetails = await LoadPullRequestsByRepositoryAsync(
            repositories,
            _pullRequestDetailsLoadThreshold,
            static (service, repository, userId, _, token) =>
                service._prApi.GetOpenPullRequestDetailsAsync(repository, userId, token),
            currentUserId,
            boundary: null,
            progress,
            cancellationToken).ConfigureAwait(false);

        return
        [
            .. pullRequestDetails
                .OrderByDescending(static detail => detail.OpenedOn)
                .ThenBy(static detail => detail.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static detail => detail.PullRequestId)
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        IReadOnlyList<Repository> repositories,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (!_loadMergedPullRequests || repositories.Count == 0)
        {
            return [];
        }

        var mergedPullRequests = await LoadPullRequestsByRepositoryAsync(
            repositories,
            _mergedPullRequestsLoadThreshold,
            static (service, repository, userId, boundary, token) =>
                service._prApi.GetMergedPullRequestsAsync(repository, boundary!.Value, userId, token),
            currentUserId,
            mergedSince,
            progress,
            cancellationToken).ConfigureAwait(false);

        return
        [
            .. mergedPullRequests
                .OrderByDescending(static pullRequest => pullRequest.MergedOn)
                .ThenBy(static pullRequest => pullRequest.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static pullRequest => pullRequest.PullRequestId)
        ];
    }

    private async Task<IReadOnlyList<TPullRequest>> LoadPullRequestsByRepositoryAsync<TPullRequest>(
        IReadOnlyList<Repository> repositories,
        int maxDegreeOfParallelism,
        Func<RepositoryService, Repository, BitbucketId, DateTimeOffset?, CancellationToken, Task<IReadOnlyList<TPullRequest>>> loadPullRequests,
        BitbucketId currentUserId,
        DateTimeOffset? boundary,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(loadPullRequests);

        var repositoriesToInspect = repositories
            .Where(static repository => repository.CanLoadPullRequests)
            .ToList();

        if (repositoriesToInspect.Count == 0)
        {
            return [];
        }

        var pullRequestsByRepository = new IReadOnlyList<TPullRequest>?[repositoriesToInspect.Count];
        var loadedRepositories = 0;

        progress?.Report(new PullRequestRepositoryLoadProgress(0, repositoriesToInspect.Count));

        await Parallel.ForEachAsync(
            Enumerable.Range(0, repositoriesToInspect.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (index, token) =>
            {
                token.ThrowIfCancellationRequested();

                var repository = repositoriesToInspect[index];
                var pullRequests = await loadPullRequests(this, repository, currentUserId, boundary, token)
                    .ConfigureAwait(false);

                pullRequestsByRepository[index] = pullRequests;

                var currentLoaded = Interlocked.Increment(ref loadedRepositories);
                progress?.Report(new PullRequestRepositoryLoadProgress(currentLoaded, repositoriesToInspect.Count));
            }).ConfigureAwait(false);

        var pullRequestsByAllRepositories = new List<TPullRequest>();
        for (var index = 0; index < pullRequestsByRepository.Length; index++)
        {
            if (pullRequestsByRepository[index] is { } pullRequests)
            {
                pullRequestsByAllRepositories.AddRange(pullRequests);
            }
        }

        return pullRequestsByAllRepositories;
    }

    private readonly IBitbucketRepoApiClient _api;
    private readonly IBitbucketPRApiClient _prApi;
    private readonly bool _loadOpenPullRequestsStatistics;
    private readonly int _openPullRequestsLoadThreshold;
    private readonly bool _loadPullRequestDetails;
    private readonly int _pullRequestDetailsLoadThreshold;
    private readonly bool _loadMergedPullRequests;
    private readonly int _mergedPullRequestsLoadThreshold;
}

