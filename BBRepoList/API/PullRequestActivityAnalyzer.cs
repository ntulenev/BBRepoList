using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Builds report activity summaries from raw pull request activity entries.
/// </summary>
public sealed class PullRequestActivityAnalyzer : IPullRequestActivityAnalyzer
{
    /// <inheritdoc />
    public PullRequestActivitySummary CreateSummary(
        IReadOnlyList<PullRequestActivityEntry> activities,
        PullRequestSnapshot pullRequest,
        BitbucketId currentUserId)
    {
        ArgumentNullException.ThrowIfNull(activities);

        var firstNonAuthorActivityOn = activities
            .Where(activity => pullRequest.AuthorId is null
                               || activity.ActorId != pullRequest.AuthorId.Value)
            .OrderBy(static activity => activity.HappenedOn)
            .Select(static activity => (DateTimeOffset?)activity.HappenedOn)
            .FirstOrDefault();
        var lastActivityOn = activities
            .OrderByDescending(static activity => activity.HappenedOn)
            .Select(static activity => (DateTimeOffset?)activity.HappenedOn)
            .FirstOrDefault();
        var hasCurrentUserDiscussion = activities.Any(activity =>
            activity.IsComment
            && activity.ActorId == currentUserId);
        var commentsCount = activities.Count(static activity => activity.IsComment);

        return new PullRequestActivitySummary(
            firstNonAuthorActivityOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            commentsCount);
    }
}
