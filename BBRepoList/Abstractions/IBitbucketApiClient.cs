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

    /// <summary>
    /// Populates repository open pull requests count when available.
    /// </summary>
    /// <param name="repository">Repository to enrich.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Repository enriched with open pull requests count when resolved.</returns>
    Task<Repository> PopulateOpenPullRequestCountAsync(Repository repository, CancellationToken cancellationToken);

    /// <summary>
    /// Loads open pull request details for repository report.
    /// </summary>
    /// <param name="repository">Repository to inspect.</param>
    /// <param name="currentUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Open pull request details list for provided repository.</returns>
    Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        Repository repository,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);
}
