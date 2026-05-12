namespace BBRepoList.Models;

/// <summary>
/// Loaded data for a repository report run.
/// </summary>
public sealed class RepositoryReportGenerationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryReportGenerationResult"/> class.
    /// </summary>
    /// <param name="repositories">Repositories included in report.</param>
    /// <param name="mergedPullRequests">Recently merged pull request report rows.</param>
    /// <param name="pullRequestDetails">Open pull request detail report rows.</param>
    /// <param name="reportData">Presentation-neutral report data.</param>
    public RepositoryReportGenerationResult(
        IReadOnlyList<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        RepositoryReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(mergedPullRequests);
        ArgumentNullException.ThrowIfNull(pullRequestDetails);
        ArgumentNullException.ThrowIfNull(reportData);

        Repositories = repositories;
        MergedPullRequests = mergedPullRequests;
        PullRequestDetails = pullRequestDetails;
        ReportData = reportData;
    }

    /// <summary>
    /// Repositories included in report.
    /// </summary>
    public IReadOnlyList<Repository> Repositories { get; }

    /// <summary>
    /// Recently merged pull request report rows.
    /// </summary>
    public IReadOnlyList<MergedPullRequest> MergedPullRequests { get; }

    /// <summary>
    /// Open pull request detail report rows.
    /// </summary>
    public IReadOnlyList<PullRequestDetail> PullRequestDetails { get; }

    /// <summary>
    /// Presentation-neutral report data.
    /// </summary>
    public RepositoryReportData ReportData { get; }
}
