using BBRepoList.Abstractions;
using BBRepoList.Models;

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
    public RepositoryService(IBitbucketApiClient api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
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
                var enriched = await _api.PopulateOpenPullRequestCountAsync(repository, cancellationToken).ConfigureAwait(false);
                all.Add(enriched);
                matched++;
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        return all;
    }

    private readonly IBitbucketApiClient _api;
}
