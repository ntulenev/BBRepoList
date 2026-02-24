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
    /// <param name="ttfrThresholdHours">TTFR threshold in hours.</param>
    /// <param name="generatedAt">Generation timestamp.</param>
    /// <param name="repositories">Repositories included in report.</param>
    /// <param name="pullRequestDetails">Open pull request details report rows.</param>
    public RepositoryPdfReportData(
        string workspace,
        string? filterPhrase,
        int abandonedMonthsThreshold,
        int ttfrThresholdHours,
        DateTimeOffset generatedAt,
        IReadOnlyList<Repository> repositories,
        IReadOnlyList<PullRequestDetail> pullRequestDetails)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(pullRequestDetails);

        Workspace = workspace.Trim();
        FilterPhrase = string.IsNullOrWhiteSpace(filterPhrase) ? null : filterPhrase.Trim();
        AbandonedMonthsThreshold = abandonedMonthsThreshold;
        TtfrThresholdHours = ttfrThresholdHours;
        GeneratedAt = generatedAt;
        Repositories = repositories;
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
    /// Open pull request details report rows.
    /// </summary>
    public IReadOnlyList<PullRequestDetail> PullRequestDetails { get; }
}
