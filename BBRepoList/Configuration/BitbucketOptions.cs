using System.ComponentModel.DataAnnotations;

using BBRepoList.Models;

namespace BBRepoList.Configuration;

/// <summary>
/// Configuration settings for Bitbucket API access.
/// </summary>
public sealed class BitbucketOptions
    : IValidatableObject
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
    /// PDF report settings.
    /// </summary>
    [Required]
    public PdfOptions Pdf { get; init; } = new();

    /// <summary>
    /// HTML report settings.
    /// </summary>
    [Required]
    public HtmlOptions Html { get; init; } = new();

    /// <summary>
    /// Pull request details report settings.
    /// </summary>
    [Required]
    public PullRequestDetailsOptions PullRequestDetails { get; init; } = new();

    /// <summary>
    /// Whether open pull request statistics should be loaded.
    /// </summary>
    public bool LoadOpenPullRequestsStatistics { get; init; } = true;

    /// <summary>
    /// Maximum number of concurrent open pull request statistics requests.
    /// </summary>
    [Range(1, 64)]
    public int OpenPullRequestsLoadThreshold { get; init; } = 8;

    /// <summary>
    /// Inactivity threshold in months to treat a repository as abandoned.
    /// </summary>
    [Range(1, 120)]
    public int AbandonedMonthsThreshold { get; init; } = 12;

    /// <summary>
    /// Whether abandoned repositories statistics should be loaded.
    /// </summary>
    public bool LoadAbandonedRepositoriesStatistics { get; init; } = true;

    /// <summary>
    /// Default repository name search mode.
    /// </summary>
    [EnumDataType(typeof(RepositorySearchMode))]
    public RepositorySearchMode RepositorySearchMode { get; init; } = RepositorySearchMode.Contains;

    /// <summary>
    /// Optional repository search phrase to use without prompting in console.
    /// </summary>
    public string? RepositorySearchPhrase { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (PullRequestDetails is null)
        {
            yield break;
        }

        var nestedResults = new List<ValidationResult>();
        _ = Validator.TryValidateObject(
            PullRequestDetails,
            new ValidationContext(PullRequestDetails),
            nestedResults,
            validateAllProperties: true);

        foreach (var result in nestedResults)
        {
            yield return result;
        }
    }
}
