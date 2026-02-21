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
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryService(IBitbucketApiClient api, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(options);

        _api = api;
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

        var enrichedRepositories = new Repository[matchedRepositories.Count];

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
                var enrichedRepository = await _api
                    .PopulateOpenPullRequestCountAsync(matchedRepositories[index], token)
                    .ConfigureAwait(false);
                enrichedRepositories[index] = enrichedRepository;

                var currentLoaded = Interlocked.Increment(ref prStatisticsLoaded);
                progress?.Report(new RepoLoadProgress(
                    seen,
                    matched,
                    isLoadingPullRequestStatistics: true,
                    pullRequestStatisticsLoaded: currentLoaded,
                    pullRequestStatisticsTotal: matchedRepositories.Count));
            }).ConfigureAwait(false);

        return enrichedRepositories;
    }

    private readonly IBitbucketApiClient _api;
    private readonly bool _loadOpenPullRequestsStatistics;
    private readonly int _openPullRequestsLoadThreshold;
}
