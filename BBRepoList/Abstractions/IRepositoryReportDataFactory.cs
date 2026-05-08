using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Creates repository report data for report renderers.
/// </summary>
public interface IRepositoryReportDataFactory
{
    /// <summary>
    /// Creates repository report data from loaded repositories and pull request rows.
    /// </summary>
    /// <param name="repositories">Repositories included in report.</param>
    /// <param name="mergedPullRequests">Recently merged pull request report rows.</param>
    /// <param name="pullRequestDetails">Open pull request details report rows.</param>
    /// <param name="filterPattern">Repository filter pattern used for the run.</param>
    /// <param name="generatedAt">Report generation timestamp.</param>
    /// <returns>Repository report data.</returns>
    RepositoryReportData Create(
        IReadOnlyList<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern,
        DateTimeOffset generatedAt);
}
