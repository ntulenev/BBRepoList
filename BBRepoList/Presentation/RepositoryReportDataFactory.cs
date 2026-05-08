using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Presentation;

/// <summary>
/// Default repository report data factory.
/// </summary>
public sealed class RepositoryReportDataFactory : IRepositoryReportDataFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryReportDataFactory"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryReportDataFactory(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public RepositoryReportData Create(
        IReadOnlyList<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(mergedPullRequests);
        ArgumentNullException.ThrowIfNull(pullRequestDetails);

        return new RepositoryReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            _options.LoadAbandonedRepositoriesStatistics,
            _options.PullRequestDetails.TtfrThresholdHours,
            _options.PullRequestDetails.MinimalDescriptionTextLength,
            _options.MergedPullRequests.IsEnabled,
            _options.MergedPullRequests.Days,
            generatedAt,
            repositories,
            mergedPullRequests,
            pullRequestDetails);
    }

    private readonly BitbucketOptions _options;
}
