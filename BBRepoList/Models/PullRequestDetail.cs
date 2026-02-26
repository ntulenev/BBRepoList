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
    /// <param name="hasCurrentUserDiscussion">Whether current authenticated user has commented in activity.</param>
    public PullRequestDetail(
        Repository repository,
        int pullRequestId,
        string title,
        DateTimeOffset openedOn,
        BitbucketId? authorId,
        DateTimeOffset? firstNonAuthorActivityOn,
        bool hasCurrentUserDiscussion)
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

        Repository = repository;
        PullRequestId = pullRequestId;
        Title = title.Trim();
        OpenedOn = openedOn;
        AuthorId = authorId;
        FirstNonAuthorActivityOn = firstNonAuthorActivityOn;
        HasCurrentUserDiscussion = hasCurrentUserDiscussion;
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
    /// Pull request author identifier.
    /// </summary>
    public BitbucketId? AuthorId { get; }

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
