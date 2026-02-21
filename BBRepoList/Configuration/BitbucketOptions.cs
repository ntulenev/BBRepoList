using System.ComponentModel.DataAnnotations;

namespace BBRepoList.Configuration;

/// <summary>
/// Configuration settings for Bitbucket API access.
/// </summary>
public sealed class BitbucketOptions
{
    /// <summary>
    /// Base Bitbucket API URL.
    /// </summary>
    [Required]
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Bitbucket workspace identifier.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Workspace { get; init; }

    /// <summary>
    /// Authentication email for Bitbucket API.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string AuthEmail { get; init; }

    /// <summary>
    /// Authentication API token for Bitbucket API.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string AuthApiToken { get; init; }

    /// <summary>
    /// Number of repositories per page.
    /// </summary>
    [Range(1, 100)]
    public int PageLen { get; init; }

    /// <summary>
    /// Number of retries for transient Bitbucket API errors.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; init; }

    /// <summary>
    /// Whether open pull request statistics should be loaded.
    /// </summary>
    public bool LoadOpenPullRequestsStatistics { get; init; } = true;

    /// <summary>
    /// Inactivity threshold in months to treat a repository as abandoned.
    /// </summary>
    [Range(1, 120)]
    public int AbandonedMonthsThreshold { get; init; } = 12;
}
