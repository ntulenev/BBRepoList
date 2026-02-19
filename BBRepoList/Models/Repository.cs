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
    public Repository(string name, DateTimeOffset? createdOn = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        CreatedOn = createdOn;
    }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Repository creation date/time.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; }
}
