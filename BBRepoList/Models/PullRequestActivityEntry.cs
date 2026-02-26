namespace BBRepoList.Models;

/// <summary>
/// Parsed pull request activity entry used for reporting calculations.
/// </summary>
/// <param name="ActorId">Bitbucket identifier of the activity author.</param>
/// <param name="HappenedOn">Activity timestamp.</param>
/// <param name="IsComment">Whether the activity is comment-related.</param>
internal readonly record struct PullRequestActivityEntry(
    BitbucketId ActorId,
    DateTimeOffset HappenedOn,
    bool IsComment);
