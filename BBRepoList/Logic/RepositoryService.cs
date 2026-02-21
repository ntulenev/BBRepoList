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
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var all = new List<Repository>();

        var seen = 0;
        var matched = 0;

        await foreach (var repository in _api.GetRepositoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (filterPattern.Filter(repository))
            {
                var result = _loadOpenPullRequestsStatistics
                    ? await _api.PopulateOpenPullRequestCountAsync(repository, cancellationToken).ConfigureAwait(false)
                    : repository;

                all.Add(result);
                matched++;
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        return all;
    }

    private readonly IBitbucketApiClient _api;
    private readonly bool _loadOpenPullRequestsStatistics;
}
