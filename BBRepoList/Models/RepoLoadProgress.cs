namespace BBRepoList.Models;

/// <summary>
/// Progress snapshot for repository loading.
/// </summary>
public sealed class RepoLoadProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepoLoadProgress"/> class.
    /// </summary>
    /// <param name="seen">Total repositories seen so far.</param>
    /// <param name="matched">Total repositories matched so far.</param>
    public RepoLoadProgress(int seen, int matched)
    {
        if (seen < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seen), "Seen cannot be negative.");
        }

        if (matched < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matched), "Matched cannot be negative.");
        }

        Seen = seen;
        Matched = matched;
    }

    /// <summary>
    /// Total repositories seen so far.
    /// </summary>
    public int Seen { get; }

    /// <summary>
    /// Total repositories matched so far.
    /// </summary>
    public int Matched { get; }
}
