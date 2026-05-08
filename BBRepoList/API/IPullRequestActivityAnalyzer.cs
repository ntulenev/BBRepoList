using BBRepoList.Models;

namespace BBRepoList.API;

/// <summary>
/// Builds report activity summaries from raw pull request activity entries.
/// </summary>
public interface IPullRequestActivityAnalyzer
{
    /// <summary>
    /// Builds an activity summary for a pull request snapshot.
    /// </summary>
    /// <param name="activities">Raw pull request activity entries.</param>
    /// <param name="pullRequest">Pull request snapshot.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user id.</param>
    /// <returns>Aggregated activity summary.</returns>
    PullRequestActivitySummary CreateSummary(
        IReadOnlyList<PullRequestActivityEntry> activities,
        PullRequestSnapshot pullRequest,
        BitbucketId currentUserId);
}
