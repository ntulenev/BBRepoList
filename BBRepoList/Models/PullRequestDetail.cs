namespace BBRepoList.Models;

/// <summary>
/// Open pull request details used by reporting.
/// </summary>
public sealed class PullRequestDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDetail"/> class.
    /// </summary>
    /// <param name="repository">Repository that owns the pull request.</param>
    /// <param name="pullRequestId">Pull request identifier in repository scope.</param>
    /// <param name="title">Pull request title.</param>
    /// <param name="openedOn">Pull request creation timestamp.</param>
    /// <param name="authorId">Pull request author identifier.</param>
    /// <param name="firstNonAuthorActivityOn">First activity timestamp by non-author.</param>
    /// <param name="lastActivityOn">Latest pull request activity timestamp.</param>
    /// <param name="hasCurrentUserDiscussion">Whether current authenticated user has commented in activity.</param>
    /// <param name="descriptionText">Pull request description text.</param>
    /// <param name="requestChangesCount">Active request changes count for the pull request.</param>
    /// <param name="hasCurrentUserRequestChanges">Whether current authenticated user currently requests changes.</param>
    /// <param name="approvalsCount">Active approvals count for the pull request.</param>
    /// <param name="hasCurrentUserApproval">Whether current authenticated user currently approves the pull request.</param>
    public PullRequestDetail(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        BitbucketId? authorId,
        DateTimeOffset? firstNonAuthorActivityOn,
        DateTimeOffset? lastActivityOn,
        bool hasCurrentUserDiscussion,
        string? descriptionText = null,
        int requestChangesCount = 0,
        bool hasCurrentUserRequestChanges = false,
        int approvalsCount = 0,
        bool hasCurrentUserApproval = false)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (pullRequestId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pullRequestId), "Pull request id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Pull request title cannot be empty.", nameof(title));
        }

        if (requestChangesCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestChangesCount),
                "Request changes count cannot be negative.");
        }

        if (approvalsCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approvalsCount),
                "Approvals count cannot be negative.");
        }

        Repository = repository;
        PullRequestId = pullRequestId;
        Title = title.Trim();
        OpenedOn = openedOn;
        DescriptionText = string.IsNullOrWhiteSpace(descriptionText) ? null : descriptionText.Trim();
        AuthorId = authorId;
        FirstNonAuthorActivityOn = firstNonAuthorActivityOn;
        LastActivityOn = lastActivityOn;
        HasCurrentUserDiscussion = hasCurrentUserDiscussion;
        RequestChangesCount = requestChangesCount;
        HasCurrentUserRequestChanges = requestChangesCount > 0 && hasCurrentUserRequestChanges;
        ApprovalsCount = approvalsCount;
        HasCurrentUserApproval = approvalsCount > 0 && hasCurrentUserApproval;
    }

    /// <summary>
    /// Repository that owns the pull request.
    /// </summary>
    public Repository Repository { get; }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string RepositoryName => Repository.Name;

    /// <summary>
    /// Repository slug in workspace scope.
    /// </summary>
    public string? RepositorySlug => Repository.Slug;

    /// <summary>
    /// Repository creation timestamp.
    /// </summary>
    public DateTimeOffset? RepositoryCreatedOn => Repository.CreatedOn;

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
    /// Pull request description text.
    /// </summary>
    public string? DescriptionText { get; }

    /// <summary>
    /// Pull request author identifier.
    /// </summary>
    public BitbucketId? AuthorId { get; }

    /// <summary>
    /// First activity timestamp by non-author.
    /// </summary>
    public DateTimeOffset? FirstNonAuthorActivityOn { get; }

    /// <summary>
    /// Latest pull request activity timestamp.
    /// </summary>
    public DateTimeOffset? LastActivityOn { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has commented in activity.
    /// </summary>
    public bool HasCurrentUserDiscussion { get; }

    /// <summary>
    /// Gets the number of active reviewers who currently request changes.
    /// </summary>
    public int RequestChangesCount { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently requests changes.
    /// </summary>
    public bool HasCurrentUserRequestChanges { get; }

    /// <summary>
    /// Gets the number of active reviewers who currently approve the pull request.
    /// </summary>
    public int ApprovalsCount { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently approves the pull request.
    /// </summary>
    public bool HasCurrentUserApproval { get; }

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

    /// <summary>
    /// Calculates how long it has been since the latest pull request activity.
    /// </summary>
    /// <param name="asOf">Time boundary for calculation.</param>
    /// <returns>Non-negative duration since last activity, or <see langword="null"/> when no activity exists.</returns>
    public TimeSpan? GetLastActivityAge(DateTimeOffset asOf) =>
        LastActivityOn is null
            ? null
            : TimeSpan.FromTicks(Math.Max((asOf - LastActivityOn.Value).Ticks, 0));

    /// <summary>
    /// Returns whether pull request description length is below minimal required length.
    /// </summary>
    /// <param name="minimalDescriptionTextLength">Minimal allowed description length.</param>
    /// <returns><see langword="true"/> when description should be marked.</returns>
    public bool HasShortOrMissingDescription(int minimalDescriptionTextLength)
    {
        if (minimalDescriptionTextLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimalDescriptionTextLength),
                "Minimal description length cannot be negative.");
        }

        var descriptionLength = DescriptionText?.Length ?? 0;
        return descriptionLength < minimalDescriptionTextLength;
    }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has any tracked pull request activity.
    /// </summary>
    public bool HasCurrentUserActivity =>
        HasCurrentUserDiscussion
        || HasCurrentUserRequestChanges
        || HasCurrentUserApproval;
}
