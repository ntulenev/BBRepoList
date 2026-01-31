namespace BBRepoList.Models;

/// <summary>
/// Page container for repository results.
/// </summary>
public sealed class RepoPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepoPage"/> class.
    /// </summary>
    /// <param name="values">Repository items on this page.</param>
    /// <param name="next">URL for the next page, if any.</param>
    public RepoPage(IReadOnlyList<Repository> values, Uri? next)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Any(static v => v is null))
        {
            throw new ArgumentException("Repository list cannot contain null entries.", nameof(values));
        }

        Values = values;
        Next = next;
    }

    /// <summary>
    /// Repository items on this page.
    /// </summary>
    public IReadOnlyList<Repository> Values { get; }

    /// <summary>
    /// URL for the next page, if any.
    /// </summary>
    public Uri? Next { get; }
}
