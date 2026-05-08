namespace BBRepoList.Models;

/// <summary>
/// Common pull request data used by report renderers.
/// </summary>
internal interface IPullRequestReportItem
{
    string RepositoryName { get; }

    string? RepositorySlug { get; }

    int PullRequestId { get; }

    string Title { get; }

    DateTimeOffset OpenedOn { get; }

    string? DescriptionText { get; }

    string? AuthorDisplayName { get; }

    TimeSpan? TimeToFirstResponse { get; }

    int CommentsCount { get; }

    int RequestChangesCount { get; }

    bool HasCurrentUserRequestChanges { get; }

    int ApprovalsCount { get; }

    bool HasCurrentUserApproval { get; }

    bool HasCurrentUserDiscussion { get; }

    bool HasCurrentUserActivity { get; }

    TimeSpan? GetLastActivityAge(DateTimeOffset asOf);

    bool HasShortOrMissingDescription(int minimalDescriptionTextLength);
}
