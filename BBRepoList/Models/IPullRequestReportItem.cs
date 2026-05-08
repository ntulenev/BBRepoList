namespace BBRepoList.Models;

/// <summary>
/// Common pull request data used by report renderers.
/// </summary>
internal interface IPullRequestReportItem
{
    /// <summary>
    /// Repository display name.
    /// </summary>
    string RepositoryName { get; }

    /// <summary>
    /// Repository slug in workspace scope.
    /// </summary>
    string? RepositorySlug { get; }

    /// <summary>
    /// Pull request identifier in repository scope.
    /// </summary>
    int PullRequestId { get; }

    /// <summary>
    /// Pull request title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Pull request creation timestamp.
    /// </summary>
    DateTimeOffset OpenedOn { get; }

    /// <summary>
    /// Pull request description text.
    /// </summary>
    string? DescriptionText { get; }

    /// <summary>
    /// Pull request author display name.
    /// </summary>
    string? AuthorDisplayName { get; }

    /// <summary>
    /// TTFR value computed as the period from PR opening to first non-author activity.
    /// </summary>
    TimeSpan? TimeToFirstResponse { get; }

    /// <summary>
    /// Number of comments detected in pull request activity.
    /// </summary>
    int CommentsCount { get; }

    /// <summary>
    /// Number of active reviewers who currently request changes.
    /// </summary>
    int RequestChangesCount { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently requests changes.
    /// </summary>
    bool HasCurrentUserRequestChanges { get; }

    /// <summary>
    /// Number of active reviewers who currently approve the pull request.
    /// </summary>
    int ApprovalsCount { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently approves the pull request.
    /// </summary>
    bool HasCurrentUserApproval { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has commented in activity.
    /// </summary>
    bool HasCurrentUserDiscussion { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has any tracked pull request activity.
    /// </summary>
    bool HasCurrentUserActivity { get; }

    /// <summary>
    /// Calculates how long it has been since the latest pull request activity.
    /// </summary>
    /// <param name="asOf">Time boundary for calculation.</param>
    /// <returns>Non-negative duration since last activity, or <see langword="null"/> when no activity exists.</returns>
    TimeSpan? GetLastActivityAge(DateTimeOffset asOf);

    /// <summary>
    /// Returns whether pull request description length is below minimal required length.
    /// </summary>
    /// <param name="minimalDescriptionTextLength">Minimal allowed description length.</param>
    /// <returns><see langword="true"/> when description should be marked.</returns>
    bool HasShortOrMissingDescription(int minimalDescriptionTextLength);
}
