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
        string? searchPhrase,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var all = new List<Repository>();
        var hasFilter = !string.IsNullOrWhiteSpace(searchPhrase);

        var seen = 0;
        var matched = 0;

        await foreach (var repository in _api.GetRepositoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            seen++;

            if (hasFilter)
            {
                if (repository.Name.Contains(searchPhrase!, StringComparison.OrdinalIgnoreCase))
                {
                    all.Add(repository);
                    matched++;
                }
            }
            else
            {
                all.Add(repository);
                matched++;
            }

            progress?.Report(new RepoLoadProgress(seen, matched));
        }

        return all;
    }

    private readonly IBitbucketApiClient _api;
}
