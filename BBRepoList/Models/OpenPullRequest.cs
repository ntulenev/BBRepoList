namespace BBRepoList.Models;

/// <summary>
/// Lightweight open pull request projection used during PR detail loading.
/// </summary>
/// <param name="Id">Pull request identifier within repository scope.</param>
/// <param name="Title">Pull request title.</param>
/// <param name="CreatedOn">Pull request creation timestamp.</param>
/// <param name="DescriptionText">Pull request description text.</param>
/// <param name="AuthorId">Pull request author identifier when available.</param>
/// <param name="AuthorDisplayName">Pull request author display name when available.</param>
/// <param name="RequestChangesCount">Active request changes count.</param>
/// <param name="HasCurrentUserRequestChanges">Whether current user currently requests changes.</param>
/// <param name="ApprovalsCount">Active approvals count.</param>
/// <param name="HasCurrentUserApproval">Whether current user currently approves the pull request.</param>
internal readonly record struct OpenPullRequest(
    int Id,
    string Title,
    DateTimeOffset CreatedOn,
    string? DescriptionText = null,
    BitbucketId? AuthorId = null,
    string? AuthorDisplayName = null,
    int RequestChangesCount = 0,
    bool HasCurrentUserRequestChanges = false,
    int ApprovalsCount = 0,
    bool HasCurrentUserApproval = false);
