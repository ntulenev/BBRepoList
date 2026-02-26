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

        await foreach (var repository in _api.GetRepositoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (filterPattern.Filter(repository))
            {
                matched++;
                matchedRepositories.Add(repository);
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        if (!_loadOpenPullRequestsStatistics)
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
        IProgress<PullRequestDetailsLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (!_loadPullRequestDetails || repositories.Count == 0)
        {
            return [];
        }

        var repositoriesToInspect = repositories
            .Where(static repository => repository.CanLoadOpenPullRequestDetails)
            .ToList();

        if (repositoriesToInspect.Count == 0)
        {
            return [];
        }

        var detailsByRepository = new IReadOnlyList<PullRequestDetail>?[repositoriesToInspect.Count];
        var loadedRepositories = 0;

        progress?.Report(new PullRequestDetailsLoadProgress(0, repositoriesToInspect.Count));

        await Parallel.ForEachAsync(
            Enumerable.Range(0, repositoriesToInspect.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _pullRequestDetailsLoadThreshold,
                CancellationToken = cancellationToken
            },
            async (index, token) =>
            {
                token.ThrowIfCancellationRequested();

                var repository = repositoriesToInspect[index];
                var details = await _prApi
                    .GetOpenPullRequestDetailsAsync(repository, currentUserId, token)
                    .ConfigureAwait(false);

                detailsByRepository[index] = details;

                var currentLoaded = Interlocked.Increment(ref loadedRepositories);
                progress?.Report(new PullRequestDetailsLoadProgress(currentLoaded, repositoriesToInspect.Count));
            }).ConfigureAwait(false);

        var pullRequestDetails = new List<PullRequestDetail>();
        for (var index = 0; index < detailsByRepository.Length; index++)
        {
            if (detailsByRepository[index] is { } details)
            {
                pullRequestDetails.AddRange(details);
            }
        }

        return
        [
            .. pullRequestDetails
                .OrderByDescending(static detail => detail.OpenedOn)
                .ThenBy(static detail => detail.RepositoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static detail => detail.PullRequestId)
        ];
    }

    private readonly IBitbucketRepoApiClient _api;
    private readonly IBitbucketPRApiClient _prApi;
    private readonly bool _loadOpenPullRequestsStatistics;
    private readonly int _openPullRequestsLoadThreshold;
    private readonly bool _loadPullRequestDetails;
    private readonly int _pullRequestDetailsLoadThreshold;
}

