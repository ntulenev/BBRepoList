using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.Models;

namespace BBRepoList.Caching;

/// <summary>
/// Coordinates pull request details cache access and cache entry validation.
/// </summary>
public sealed class PullRequestDetailsCacheService : IPullRequestDetailsCacheService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDetailsCacheService"/> class.
    /// </summary>
    /// <param name="cache">Pull request details cache storage.</param>
    public PullRequestDetailsCacheService(IPullRequestDetailsCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, PullRequestDetailsCacheEntry>> ReadEntriesByPullRequestIdAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        var cacheEntries = await _cache
            .ReadEntriesAsync(workspace, repositorySlug, currentUserId, cancellationToken)
            .ConfigureAwait(false);

        return cacheEntries
            .GroupBy(static entry => entry.PullRequestId)
            .ToDictionary(static group => group.Key, static group => group.Last());
    }

    /// <inheritdoc />
    public Task SaveEntriesAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries,
        CancellationToken cancellationToken) =>
        _cache.SaveEntriesAsync(workspace, repositorySlug, currentUserId, entries, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken) =>
        _cache.DeleteAsync(workspace, repositorySlug, currentUserId, cancellationToken);

    /// <inheritdoc />
    public bool TryCreateActivitySummary(
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<int, PullRequestDetailsCacheEntry> entriesByPullRequestId,
        out PullRequestActivitySummary activitySummary,
        out PullRequestDetailsCacheEntry cacheEntry)
    {
        ArgumentNullException.ThrowIfNull(entriesByPullRequestId);

        activitySummary = null!;
        cacheEntry = null!;

        if (string.IsNullOrWhiteSpace(pullRequest.CacheFingerprint)
            || !entriesByPullRequestId.TryGetValue(pullRequest.Id, out var existingEntry)
            || !string.Equals(existingEntry.Fingerprint, pullRequest.CacheFingerprint, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            activitySummary = new PullRequestActivitySummary(
                existingEntry.FirstNonAuthorActivityOn,
                existingEntry.LastActivityOn,
                existingEntry.HasCurrentUserDiscussion,
                existingEntry.CommentsCount);
            cacheEntry = existingEntry;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public PullRequestDetailsCacheEntry CreateEntry(
        PullRequestSnapshot pullRequest,
        PullRequestActivitySummary activitySummary)
    {
        ArgumentNullException.ThrowIfNull(activitySummary);

        return new PullRequestDetailsCacheEntry(
            pullRequest.Id,
            pullRequest.CacheFingerprint ?? string.Empty,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            activitySummary.CommentsCount);
    }

    private readonly IPullRequestDetailsCache _cache;
}
