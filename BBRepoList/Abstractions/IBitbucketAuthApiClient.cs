using System.Net.Http.Headers;

using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Bitbucket API client abstraction for authentication operations.
/// </summary>
public interface IBitbucketAuthApiClient
{
    /// <summary>
    /// Retrieves the authenticated user profile for the current credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated Bitbucket user.</returns>
    Task<BitbucketUser> AuthSelfCheckAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Builds a basic auth header value for Bitbucket API requests.
    /// </summary>
    /// <param name="authEmail">Authentication email.</param>
    /// <param name="authApiToken">Authentication API token.</param>
    /// <returns>Authorization header value.</returns>
    AuthenticationHeaderValue BuildAuthHeader(string authEmail, string authApiToken);
}
