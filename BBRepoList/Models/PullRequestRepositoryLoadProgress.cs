namespace BBRepoList.Models;

/// <summary>
/// Progress snapshot for pull request loading by repository.
/// </summary>
public sealed class PullRequestRepositoryLoadProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestRepositoryLoadProgress"/> class.
    /// </summary>
    /// <param name="loadedRepositories">Completed repositories count.</param>
    /// <param name="totalRepositories">Total repositories count.</param>
    public PullRequestRepositoryLoadProgress(int loadedRepositories, int totalRepositories)
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
