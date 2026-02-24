using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Pull request details report settings.
/// </summary>
public sealed class PullRequestDetailsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether pull request details report should be loaded.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets or sets TTFR threshold in hours for alerting.
    /// </summary>
    [Range(1, 168)]
    public int TtfrThresholdHours { get; init; } = 4;
}
