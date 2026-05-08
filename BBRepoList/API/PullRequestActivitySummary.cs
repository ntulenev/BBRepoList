namespace BBRepoList.API;

/// <summary>
/// Aggregated pull request activity values used to build report rows.
/// </summary>
/// <param name="FirstNonAuthorActivityOn">First activity timestamp by a user other than the PR author.</param>
/// <param name="LastActivityOn">Latest pull request activity timestamp.</param>
/// <param name="HasCurrentUserDiscussion">Whether current authenticated user has commented in activity.</param>
/// <param name="CommentsCount">Comment count detected in pull request activity.</param>
public sealed record PullRequestActivitySummary(
    DateTimeOffset? FirstNonAuthorActivityOn,
    DateTimeOffset? LastActivityOn,
    bool HasCurrentUserDiscussion,
    int CommentsCount);
