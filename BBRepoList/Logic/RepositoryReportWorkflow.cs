using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Logic;

/// <summary>
/// Default repository report workflow.
/// </summary>
public sealed class RepositoryReportWorkflow : IRepositoryReportWorkflow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryReportWorkflow"/> class.
    /// </summary>
    /// <param name="repoService">Repository loading service.</param>
    /// <param name="reportDataFactory">Repository report data factory.</param>
    /// <param name="htmlReportRenderer">HTML report renderer.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryReportWorkflow(
        IRepoService repoService,
        IRepositoryReportDataFactory reportDataFactory,
        IHtmlReportRenderer htmlReportRenderer,
        IPdfReportRenderer pdfReportRenderer,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(reportDataFactory);
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(options);

        _repoService = repoService;
        _reportDataFactory = reportDataFactory;
        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<RepositoryReportGenerationResult> GenerateAsync(
        FilterPattern filterPattern,
        BitbucketId currentUserId,
        DateTimeOffset reportOpenedAt,
        DateTimeOffset generatedAt,
        IProgress<RepoLoadProgress>? repositoryProgress,
        IProgress<PullRequestRepositoryLoadProgress>? pullRequestDetailsProgress,
        IProgress<PullRequestRepositoryLoadProgress>? mergedPullRequestsProgress,
        CancellationToken cancellationToken)
    {
        var repositories = await _repoService
            .GetRepositoriesAsync(filterPattern, repositoryProgress, cancellationToken)
            .ConfigureAwait(false);
        var sortedRepositories = SortRepositoriesByName(repositories);

        var pullRequestDetails = await LoadPullRequestDetailsAsync(
            sortedRepositories,
            currentUserId,
            pullRequestDetailsProgress,
            cancellationToken).ConfigureAwait(false);

        var mergedPullRequests = await LoadMergedPullRequestsAsync(
            sortedRepositories,
            reportOpenedAt,
            currentUserId,
            mergedPullRequestsProgress,
            cancellationToken).ConfigureAwait(false);

        var reportData = _reportDataFactory.Create(
            sortedRepositories,
            mergedPullRequests,
            pullRequestDetails,
            filterPattern,
            generatedAt);

        return new RepositoryReportGenerationResult(
            sortedRepositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);
    }

    /// <inheritdoc />
    public void RenderReports(RepositoryReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        _htmlReportRenderer.RenderReport(reportData);
        _pdfReportRenderer.RenderReport(reportData);
    }

    private static List<Repository> SortRepositoriesByName(IReadOnlyList<Repository> repositories) =>
    [
        .. repositories.OrderBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
    ];

    private async Task<IReadOnlyList<PullRequestDetail>> LoadPullRequestDetailsAsync(
        List<Repository> repositories,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!_options.PullRequestDetails.IsEnabled || repositories.Count == 0)
        {
            return [];
        }

        return await _repoService
            .GetOpenPullRequestDetailsAsync(repositories, currentUserId, progress, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<MergedPullRequest>> LoadMergedPullRequestsAsync(
        List<Repository> repositories,
        DateTimeOffset reportOpenedAt,
        BitbucketId currentUserId,
        IProgress<PullRequestRepositoryLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!_options.MergedPullRequests.IsEnabled || repositories.Count == 0)
        {
            return [];
        }

        var mergedSince = reportOpenedAt.AddDays(-_options.MergedPullRequests.Days);
        return await _repoService
            .GetMergedPullRequestsAsync(repositories, mergedSince, currentUserId, progress, cancellationToken)
            .ConfigureAwait(false);
    }

    private readonly IRepoService _repoService;
    private readonly IRepositoryReportDataFactory _reportDataFactory;
    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly BitbucketOptions _options;
}
