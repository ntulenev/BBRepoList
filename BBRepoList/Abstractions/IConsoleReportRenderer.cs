using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Renders repository report sections to the console.
/// </summary>
public interface IConsoleReportRenderer
{
    /// <summary>
    /// Renders repository summary table.
    /// </summary>
    /// <param name="repositories">Repositories to render.</param>
    void RenderRepositoriesTable(IReadOnlyList<Repository> repositories);

    /// <summary>
    /// Renders repositories with open pull requests when any exist.
    /// </summary>
    /// <param name="repositories">Repositories to inspect and render.</param>
    void RenderPullRequestSnapshotsTableIfAny(IReadOnlyList<Repository> repositories);

    /// <summary>
    /// Renders recently merged pull requests when enabled and any exist.
    /// </summary>
    /// <param name="mergedPullRequests">Recently merged pull requests to render.</param>
    void RenderMergedPullRequestsTableIfAny(IReadOnlyList<MergedPullRequest> mergedPullRequests);

    /// <summary>
    /// Renders open pull request details when enabled and any exist.
    /// </summary>
    /// <param name="pullRequestDetails">Open pull request details to render.</param>
    void RenderPullRequestDetailsReportIfAny(IReadOnlyList<PullRequestDetail> pullRequestDetails);

    /// <summary>
    /// Renders abandoned repositories when enabled and any exist.
    /// </summary>
    /// <param name="repositories">Repositories to inspect and render.</param>
    void RenderAbandonedRepositoriesTableIfAny(IReadOnlyList<Repository> repositories);

    /// <summary>
    /// Renders Bitbucket request telemetry summary when enabled and any requests were tracked.
    /// </summary>
    /// <param name="snapshot">Bitbucket request telemetry snapshot.</param>
    void RenderTelemetrySummary(BitbucketTelemetrySnapshot snapshot);
}
