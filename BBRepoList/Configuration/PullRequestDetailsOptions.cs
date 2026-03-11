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

    /// <summary>
    /// Gets or sets minimal pull request description length.
    /// PRs with empty description or text shorter than this value are marked in reports.
    /// </summary>
    [Range(0, 10000)]
    public int MinimalDescriptionTextLength { get; init; } = 1;

    /// <summary>
    /// Gets or sets maximum number of concurrent repository requests for pull request details loading.
    /// </summary>
    [Range(1, 64)]
    public int LoadThreshold { get; init; } = 8;
}
