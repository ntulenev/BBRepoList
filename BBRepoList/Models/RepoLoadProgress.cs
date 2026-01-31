namespace BBRepoList.Models;

/// <summary>
/// Progress snapshot for repository loading.
/// </summary>
public sealed class RepoLoadProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepoLoadProgress"/> class.
    /// </summary>
    /// <param name="pages">Number of pages loaded so far.</param>
    /// <param name="seen">Total repositories seen so far.</param>
    /// <param name="matched">Total repositories matched so far.</param>
    public RepoLoadProgress(int pages, int seen, int matched)
    {
        if (pages < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pages), "Pages cannot be negative.");
        }

        if (seen < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seen), "Seen cannot be negative.");
        }

        if (matched < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matched), "Matched cannot be negative.");
        }

        Pages = pages;
        Seen = seen;
        Matched = matched;
    }

    /// <summary>
    /// Number of pages loaded so far.
    /// </summary>
    public int Pages { get; }

    /// <summary>
    /// Total repositories seen so far.
    /// </summary>
    public int Seen { get; }

    /// <summary>
    /// Total repositories matched so far.
    /// </summary>
    public int Matched { get; }
}
