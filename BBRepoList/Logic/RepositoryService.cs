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

    private readonly IBitbucketRepoApiClient _api;
    private readonly IBitbucketPRApiClient _prApi;
    private readonly bool _loadOpenPullRequestsStatistics;
    private readonly int _openPullRequestsLoadThreshold;
}

