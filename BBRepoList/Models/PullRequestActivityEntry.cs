namespace BBRepoList.Models;

internal readonly record struct PullRequestActivityEntry(
    string ActorUuid,
    DateTimeOffset HappenedOn,
    bool IsComment);
