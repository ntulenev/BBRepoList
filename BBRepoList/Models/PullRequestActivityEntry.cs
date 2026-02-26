namespace BBRepoList.Models;

internal readonly record struct PullRequestActivityEntry(
    BitbucketId ActorId,
    DateTimeOffset HappenedOn,
    bool IsComment);
