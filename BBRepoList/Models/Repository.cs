namespace BBRepoList.Models;

/// <summary>
/// Repository domain model.
/// </summary>
public sealed class Repository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Repository"/> class.
    /// </summary>
    /// <param name="name">Repository display name.</param>
    /// <param name="createdOn">Repository creation date/time.</param>
    /// <param name="lastUpdatedOn">Repository last update date/time.</param>
    /// <param name="openPullRequestsCount">Open pull requests count.</param>
    /// <param name="slug">Repository slug in workspace scope.</param>
    public Repository(
        string name,
        DateTimeOffset? createdOn = null,
        DateTimeOffset? lastUpdatedOn = null,
        int? openPullRequestsCount = null,
        string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        CreatedOn = createdOn;
        LastUpdatedOn = lastUpdatedOn;
        OpenPullRequestsCount = openPullRequestsCount;
        Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
    }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Repository creation date/time.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; }

    /// <summary>
    /// Repository last update date/time.
    /// </summary>
    public DateTimeOffset? LastUpdatedOn { get; }

    /// <summary>
    /// Open pull requests count.
    /// </summary>
    public int? OpenPullRequestsCount { get; }

    /// <summary>
    /// Repository slug in workspace scope.
    /// </summary>
    public string? Slug { get; }
}
