namespace BBRepoList.Models;

/// <summary>
/// Lightweight open pull request projection used during PR detail loading.
/// </summary>
/// <param name="Id">Pull request identifier within repository scope.</param>
/// <param name="Title">Pull request title.</param>
/// <param name="CreatedOn">Pull request creation timestamp.</param>
/// <param name="AuthorId">Pull request author identifier when available.</param>
internal readonly record struct OpenPullRequest(
    int Id,
    string Title,
    DateTimeOffset CreatedOn,
    BitbucketId? AuthorId);
