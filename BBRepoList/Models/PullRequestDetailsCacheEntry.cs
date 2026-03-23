namespace BBRepoList.Models;

/// <summary>
/// Cached pull request detail data derived from pull request activity.
/// </summary>
/// <param name="PullRequestId">Pull request identifier within repository scope.</param>
/// <param name="Fingerprint">Fingerprint built from the lightweight open pull request response.</param>
/// <param name="FirstNonAuthorActivityOn">First activity timestamp by non-author.</param>
/// <param name="LastActivityOn">Latest pull request activity timestamp.</param>
/// <param name="HasCurrentUserDiscussion">Whether current authenticated user has commented in activity.</param>
/// <param name="CommentsCount">Comment count detected in pull request activity.</param>
public sealed record PullRequestDetailsCacheEntry(
    int PullRequestId,
    string Fingerprint,
    DateTimeOffset? FirstNonAuthorActivityOn,
    DateTimeOffset? LastActivityOn,
    bool HasCurrentUserDiscussion,
    int CommentsCount);
