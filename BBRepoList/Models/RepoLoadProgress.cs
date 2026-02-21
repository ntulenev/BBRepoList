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
    /// <param name="isLoadingPullRequestStatistics">Whether pull request statistics are currently loading.</param>
    /// <param name="pullRequestStatisticsLoaded">Completed pull request statistics requests.</param>
    /// <param name="pullRequestStatisticsTotal">Total pull request statistics requests.</param>
    public RepoLoadProgress(
        int seen,
        int matched,
        bool isLoadingPullRequestStatistics = false,
        int pullRequestStatisticsLoaded = 0,
        int pullRequestStatisticsTotal = 0)
    {
        if (seen < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seen), "Seen cannot be negative.");
        }

        if (matched < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matched), "Matched cannot be negative.");
        }

        if (pullRequestStatisticsLoaded < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pullRequestStatisticsLoaded), "Loaded PR statistics cannot be negative.");
        }

        if (pullRequestStatisticsTotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pullRequestStatisticsTotal), "Total PR statistics cannot be negative.");
        }

        if (pullRequestStatisticsLoaded > pullRequestStatisticsTotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pullRequestStatisticsLoaded),
                "Loaded PR statistics cannot exceed total PR statistics.");
        }

        Seen = seen;
        Matched = matched;
        IsLoadingPullRequestStatistics = isLoadingPullRequestStatistics;
        PullRequestStatisticsLoaded = pullRequestStatisticsLoaded;
        PullRequestStatisticsTotal = pullRequestStatisticsTotal;
    }

    /// <summary>
    /// Total repositories seen so far.
    /// </summary>
    public int Seen { get; }

    /// <summary>
    /// Total repositories matched so far.
    /// </summary>
    public int Matched { get; }

    /// <summary>
    /// Gets a value indicating whether pull request statistics are currently loading.
    /// </summary>
    public bool IsLoadingPullRequestStatistics { get; }

    /// <summary>
    /// Completed pull request statistics requests.
    /// </summary>
    public int PullRequestStatisticsLoaded { get; }

    /// <summary>
    /// Total pull request statistics requests.
    /// </summary>
    public int PullRequestStatisticsTotal { get; }
}
