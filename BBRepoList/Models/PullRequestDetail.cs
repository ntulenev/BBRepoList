namespace BBRepoList.Models;

/// <summary>
/// Open pull request details used by reporting.
/// </summary>
public sealed class PullRequestDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDetail"/> class.
    /// </summary>
    /// <param name="repositoryName">Repository display name.</param>
    /// <param name="repositorySlug">Repository slug in workspace scope.</param>
    /// <param name="repositoryCreatedOn">Repository creation timestamp.</param>
    /// <param name="pullRequestId">Pull request identifier in repository scope.</param>
    /// <param name="title">Pull request title.</param>
    /// <param name="openedOn">Pull request creation timestamp.</param>
    /// <param name="authorUuid">Pull request author UUID.</param>
    /// <param name="firstNonAuthorActivityOn">First activity timestamp by non-author.</param>
    /// <param name="hasCurrentUserDiscussion">Whether current authenticated user has commented in activity.</param>
    public PullRequestDetail(
        string repositoryName,
        string? repositorySlug,
        DateTimeOffset? repositoryCreatedOn,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        string? authorUuid,
        DateTimeOffset? firstNonAuthorActivityOn,
        bool hasCurrentUserDiscussion)
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
        {
            throw new ArgumentException("Repository name cannot be empty.", nameof(repositoryName));
        }

        if (pullRequestId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pullRequestId), "Pull request id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Pull request title cannot be empty.", nameof(title));
        }

        RepositoryName = repositoryName.Trim();
        RepositorySlug = string.IsNullOrWhiteSpace(repositorySlug) ? null : repositorySlug.Trim();
        RepositoryCreatedOn = repositoryCreatedOn;
        PullRequestId = pullRequestId;
        Title = title.Trim();
        OpenedOn = openedOn;
        AuthorUuid = string.IsNullOrWhiteSpace(authorUuid) ? null : authorUuid.Trim();
        FirstNonAuthorActivityOn = firstNonAuthorActivityOn;
        HasCurrentUserDiscussion = hasCurrentUserDiscussion;
    }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string RepositoryName { get; }

    /// <summary>
    /// Repository slug in workspace scope.
    /// </summary>
    public string? RepositorySlug { get; }

    /// <summary>
    /// Repository creation timestamp.
    /// </summary>
    public DateTimeOffset? RepositoryCreatedOn { get; }

    /// <summary>
    /// Pull request identifier in repository scope.
    /// </summary>
    public int PullRequestId { get; }

    /// <summary>
    /// Pull request title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Pull request creation timestamp.
    /// </summary>
    public DateTimeOffset OpenedOn { get; }

    /// <summary>
    /// Pull request author UUID.
    /// </summary>
    public string? AuthorUuid { get; }

    /// <summary>
    /// First activity timestamp by non-author.
    /// </summary>
    public DateTimeOffset? FirstNonAuthorActivityOn { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has commented in activity.
    /// </summary>
    public bool HasCurrentUserDiscussion { get; }

    /// <summary>
    /// Gets TTFR value computed as the period from PR opening to first non-author activity.
    /// </summary>
    public TimeSpan? TimeToFirstResponse =>
        FirstNonAuthorActivityOn is null
            ? null
            : TimeSpan.FromTicks(Math.Max((FirstNonAuthorActivityOn.Value - OpenedOn).Ticks, 0));

    /// <summary>
    /// Calculates how long the pull request has been open.
    /// </summary>
    /// <param name="asOf">Time boundary for calculation.</param>
    /// <returns>Non-negative open duration.</returns>
    public TimeSpan GetOpenDuration(DateTimeOffset asOf) =>
        TimeSpan.FromTicks(Math.Max((asOf - OpenedOn).Ticks, 0));
}
