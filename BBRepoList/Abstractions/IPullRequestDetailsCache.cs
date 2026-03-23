using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Stores pull request detail cache entries between application runs.
/// </summary>
public interface IPullRequestDetailsCache
{
    /// <summary>
    /// Reads cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached entries, or an empty collection when cache is unavailable.</returns>
    Task<IReadOnlyList<PullRequestDetailsCacheEntry>> ReadEntriesAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="entries">Entries to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveEntriesAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes cached pull request detail entries for a repository and current user.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken);
}
