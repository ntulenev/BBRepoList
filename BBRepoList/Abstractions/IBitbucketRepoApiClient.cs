using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Bitbucket API client abstraction for repository operations.
/// </summary>
public interface IBitbucketRepoApiClient
{
    /// <summary>
    /// Streams repositories for the configured Bitbucket workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous stream of repositories.</returns>
    IAsyncEnumerable<Repository> GetRepositoriesAsync(CancellationToken cancellationToken);
}

