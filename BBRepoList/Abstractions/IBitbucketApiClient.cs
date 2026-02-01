using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Bitbucket API client for retrieving repository data.
/// </summary>
public interface IBitbucketApiClient
{
    /// <summary>
    /// Streams repositories for the configured Bitbucket workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Repositories stream.</returns>
    IAsyncEnumerable<Repository> GetRepositoriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the authenticated user profile for the current credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated Bitbucket user.</returns>
    Task<BitbucketUser> AuthSelfCheckAsync(CancellationToken cancellationToken);
}
