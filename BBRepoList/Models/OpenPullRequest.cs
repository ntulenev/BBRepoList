namespace BBRepoList.Models;

internal readonly record struct OpenPullRequest(
    int Id,
    string Title,
    DateTimeOffset CreatedOn,
    BitbucketId? AuthorId);
