using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Coordinates pull request details cache access and cache entry validation.
/// </summary>
public interface IPullRequestDetailsCacheService
{
    /// <summary>
    /// Reads cached pull request detail entries keyed by pull request identifier.
    /// </summary>
    /// <param name="workspace">Bitbucket workspace identifier.</param>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached entries keyed by pull request identifier.</returns>
    Task<IReadOnlyDictionary<int, PullRequestDetailsCacheEntry>> ReadEntriesByPullRequestIdAsync(
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

    /// <summary>
    /// Attempts to reuse cached activity-derived values for a pull request snapshot.
    /// </summary>
    /// <param name="pullRequest">Current lightweight pull request snapshot.</param>
    /// <param name="entriesByPullRequestId">Cached entries keyed by pull request identifier.</param>
    /// <param name="activitySummary">Cached activity summary when available.</param>
    /// <param name="cacheEntry">Cache entry to keep when available.</param>
    /// <returns><see langword="true"/> when the cached entry matches the pull request fingerprint.</returns>
    bool TryCreateActivitySummary(
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<int, PullRequestDetailsCacheEntry> entriesByPullRequestId,
        out PullRequestActivitySummary activitySummary,
        out PullRequestDetailsCacheEntry cacheEntry);

    /// <summary>
    /// Creates a cache entry from current pull request and activity data.
    /// </summary>
    /// <param name="pullRequest">Current lightweight pull request snapshot.</param>
    /// <param name="activitySummary">Activity-derived pull request summary.</param>
    /// <returns>Cache entry to persist.</returns>
    PullRequestDetailsCacheEntry CreateEntry(
        PullRequestSnapshot pullRequest,
        PullRequestActivitySummary activitySummary);
}
