using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Bitbucket API client for retrieving repository data.
/// </summary>
public interface IBitbucketApiClient
{
    /// <summary>
    /// Loads a repository page from the specified URL.
    /// </summary>
    /// <param name="url">Absolute Bitbucket API URL for the repositories page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Repository page response.</returns>
    Task<RepoPage> GetRepositoriesPageAsync(Uri url, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the authenticated user profile for the current credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated Bitbucket user.</returns>
    Task<BitbucketUser> AuthSelfCheckAsync(CancellationToken cancellationToken);
}
