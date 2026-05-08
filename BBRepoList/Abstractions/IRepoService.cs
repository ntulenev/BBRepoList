using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Provides access to Bitbucket repositories with optional filtering.
/// </summary>
public interface IRepoService
{
    /// <summary>
    /// Retrieves repositories with an optional name filter.
    /// </summary>
    /// <param name="filterPattern">Repository name filter pattern.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching repositories.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves open pull request details for repositories that have open pull requests.
    /// </summary>
    /// <param name="repositories">Source repositories to inspect.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user id.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Open pull request details sorted for reporting.</returns>
    Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        IReadOnlyList<Repository> repositories,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves recently merged pull requests for repositories.
    /// </summary>
    /// <param name="repositories">Source repositories to inspect.</param>
    /// <param name="mergedSince">Inclusive lower bound for pull request merge timestamp.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user id.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recently merged pull requests sorted for reporting.</returns>
    Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        IReadOnlyList<Repository> repositories,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken);
}
