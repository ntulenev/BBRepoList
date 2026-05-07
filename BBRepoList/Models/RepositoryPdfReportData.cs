namespace BBRepoList.Models;

/// <summary>
/// Aggregated data used by PDF report generation.
/// </summary>
public sealed class RepositoryPdfReportData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryPdfReportData"/> class.
    /// </summary>
    /// <param name="workspace">Workspace name.</param>
    /// <param name="filterPhrase">Filter phrase.</param>
    /// <param name="abandonedMonthsThreshold">Abandoned repository threshold in months.</param>
    /// <param name="loadAbandonedRepositoriesStatistics">Whether abandoned repositories statistics should be included.</param>
    /// <param name="ttfrThresholdHours">TTFR threshold in hours.</param>
    /// <param name="minimalDescriptionTextLength">Minimal pull request description text length.</param>
    /// <param name="loadMergedPullRequests">Whether recently merged pull request report should be included.</param>
    /// <param name="mergedPullRequestsDays">Number of days included in recently merged pull request report.</param>
    /// <param name="generatedAt">Generation timestamp.</param>
    /// <param name="repositories">Repositories included in report.</param>
    /// <param name="mergedPullRequests">Recently merged pull request report rows.</param>
    /// <param name="pullRequestDetails">Open pull request details report rows.</param>
    public RepositoryPdfReportData(
        string workspace,
        string? filterPhrase,
        int abandonedMonthsThreshold,
        bool loadAbandonedRepositoriesStatistics,
        int ttfrThresholdHours,
        int minimalDescriptionTextLength,
        bool loadMergedPullRequests,
        int mergedPullRequestsDays,
        DateTimeOffset generatedAt,
        IReadOnlyList<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(mergedPullRequests);
        ArgumentNullException.ThrowIfNull(pullRequestDetails);

        if (mergedPullRequestsDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mergedPullRequestsDays),
                "Merged pull requests days must be greater than zero.");
        }

        Workspace = workspace.Trim();
        FilterPhrase = string.IsNullOrWhiteSpace(filterPhrase) ? null : filterPhrase.Trim();
        AbandonedMonthsThreshold = abandonedMonthsThreshold;
        LoadAbandonedRepositoriesStatistics = loadAbandonedRepositoriesStatistics;
        TtfrThresholdHours = ttfrThresholdHours;
        MinimalDescriptionTextLength = minimalDescriptionTextLength;
        LoadMergedPullRequests = loadMergedPullRequests;
        MergedPullRequestsDays = mergedPullRequestsDays;
        GeneratedAt = generatedAt;
        Repositories = repositories;
        MergedPullRequests = mergedPullRequests;
        PullRequestDetails = pullRequestDetails;
    }

    /// <summary>
    /// Workspace name.
    /// </summary>
    public string Workspace { get; }

    /// <summary>
    /// Optional filter phrase used by user.
    /// </summary>
    public string? FilterPhrase { get; }

    /// <summary>
    /// Abandoned repository threshold in months.
    /// </summary>
    public int AbandonedMonthsThreshold { get; }

    /// <summary>
    /// Whether abandoned repositories statistics should be included.
    /// </summary>
    public bool LoadAbandonedRepositoriesStatistics { get; }

    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Repositories included in report.
    /// </summary>
    public IReadOnlyList<Repository> Repositories { get; }

    /// <summary>
    /// TTFR threshold in hours.
    /// </summary>
    public int TtfrThresholdHours { get; }

    /// <summary>
    /// Minimal pull request description text length.
    /// </summary>
    public int MinimalDescriptionTextLength { get; }

    /// <summary>
    /// Whether recently merged pull request report should be included.
    /// </summary>
    public bool LoadMergedPullRequests { get; }

    /// <summary>
    /// Number of days included in recently merged pull request report.
    /// </summary>
    public int MergedPullRequestsDays { get; }

    /// <summary>
    /// Recently merged pull request report rows.
    /// </summary>
    public IReadOnlyList<MergedPullRequest> MergedPullRequests { get; }

    /// <summary>
    /// Open pull request details report rows.
    /// </summary>
    public IReadOnlyList<PullRequestDetail> PullRequestDetails { get; }
}
