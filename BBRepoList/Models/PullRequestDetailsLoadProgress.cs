namespace BBRepoList.Models;

/// <summary>
/// Progress snapshot for open pull request details loading.
/// </summary>
public sealed class PullRequestDetailsLoadProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestDetailsLoadProgress"/> class.
    /// </summary>
    /// <param name="loadedRepositories">Completed repositories count.</param>
    /// <param name="totalRepositories">Total repositories count.</param>
    public PullRequestDetailsLoadProgress(int loadedRepositories, int totalRepositories)
    {
        if (loadedRepositories < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(loadedRepositories), "Loaded repositories cannot be negative.");
        }

        if (totalRepositories < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRepositories), "Total repositories cannot be negative.");
        }

        if (loadedRepositories > totalRepositories)
        {
            throw new ArgumentOutOfRangeException(
                nameof(loadedRepositories),
                "Loaded repositories cannot exceed total repositories.");
        }

        LoadedRepositories = loadedRepositories;
        TotalRepositories = totalRepositories;
    }

    /// <summary>
    /// Completed repositories count.
    /// </summary>
    public int LoadedRepositories { get; }

    /// <summary>
    /// Total repositories count.
    /// </summary>
    public int TotalRepositories { get; }
}
