using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Recently merged pull request report settings.
/// </summary>
public sealed class MergedPullRequestsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether recently merged pull requests report should be loaded.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets or sets how many days back from report opening time should be included.
    /// </summary>
    [Range(1, 366)]
    public int Days { get; init; } = 1;

    /// <summary>
    /// Gets or sets maximum number of concurrent repository requests for merged pull request loading.
    /// </summary>
    [Range(1, 64)]
    public int LoadThreshold { get; init; } = 8;
}
