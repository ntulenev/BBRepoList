namespace BBRepoList.Models;

/// <summary>
/// Bitbucket telemetry snapshot for the current run.
/// </summary>
/// <param name="IsEnabled">Whether telemetry is enabled.</param>
/// <param name="TotalRequests">Total number of tracked Bitbucket API requests.</param>
/// <param name="RequestStatistics">Aggregated request statistics by API.</param>
public sealed record BitbucketTelemetrySnapshot(
    bool IsEnabled,
    int TotalRequests,
    IReadOnlyList<BitbucketApiRequestStatistic> RequestStatistics);
