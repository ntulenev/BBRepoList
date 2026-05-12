using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Coordinates repository report data loading and file report rendering.
/// </summary>
public interface IRepositoryReportWorkflow
{
    /// <summary>
    /// Loads repositories, pull request rows, and builds report data.
    /// </summary>
    /// <param name="filterPattern">Repository filter pattern used for the run.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user id.</param>
    /// <param name="reportOpenedAt">UTC timestamp captured at the start of the report run.</param>
    /// <param name="generatedAt">Local timestamp used in generated report data.</param>
    /// <param name="repositoryProgress">Optional repository loading progress reporter.</param>
    /// <param name="pullRequestDetailsProgress">Optional open pull request details loading progress reporter.</param>
    /// <param name="mergedPullRequestsProgress">Optional recently merged pull requests loading progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded repositories, pull request rows, and report data.</returns>
    Task<RepositoryReportGenerationResult> GenerateAsync(
        FilterPattern filterPattern,
        BitbucketId currentUserId,
        DateTimeOffset reportOpenedAt,
        DateTimeOffset generatedAt,
        IProgress<RepoLoadProgress>? repositoryProgress,
        IProgress<PullRequestRepositoryLoadProgress>? pullRequestDetailsProgress,
        IProgress<PullRequestRepositoryLoadProgress>? mergedPullRequestsProgress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Renders configured file reports.
    /// </summary>
    /// <param name="reportData">Report data to render.</param>
    void RenderReports(RepositoryReportData reportData);
}
