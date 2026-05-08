using System.Diagnostics;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using Spectre.Console;

namespace BBRepoList.Presentation;

/// <summary>
/// Console UI entry for listing Bitbucket repositories.
/// </summary>
public sealed class ConsoleApp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleApp"/> class.
    /// </summary>
    /// <param name="bitbucketAuthApiClient">Bitbucket auth API client.</param>
    /// <param name="htmlReportRenderer">HTML report renderer.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    /// <param name="reportDataFactory">Repository report data factory.</param>
    /// <param name="consoleReportRenderer">Console report renderer.</param>
    /// <param name="repoService">Repository loading service.</param>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public ConsoleApp(IBitbucketAuthApiClient bitbucketAuthApiClient,
                      IHtmlReportRenderer htmlReportRenderer,
                      IPdfReportRenderer pdfReportRenderer,
                      IRepositoryReportDataFactory reportDataFactory,
                      IConsoleReportRenderer consoleReportRenderer,
                      IRepoService repoService,
                      IBitbucketTelemetryService telemetryService,
                      IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bitbucketAuthApiClient);
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(reportDataFactory);
        ArgumentNullException.ThrowIfNull(consoleReportRenderer);
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(options);

        _bitbucketAuthApiClient = bitbucketAuthApiClient;
        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
        _reportDataFactory = reportDataFactory;
        _consoleReportRenderer = consoleReportRenderer;
        _repoService = repoService;
        _telemetryService = telemetryService;
        _options = options.Value;
    }

    /// <summary>
    /// Runs the console UI flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var executionTime = Stopwatch.StartNew();
        var reportOpenedAt = DateTimeOffset.UtcNow;
        ShowTitle();

        var authenticatedUser = await TryAuthenticateAsync(cancellationToken).ConfigureAwait(false);
        if (authenticatedUser is null)
        {
            return;
        }

        var filterPattern = await ReadFilterPatternAsync(
            _options.RepositorySearchMode,
            _options.RepositorySearchPhrase,
            cancellationToken).ConfigureAwait(false);
        ShowFilterInfo(filterPattern);

        var repositories = await LoadRepositoriesAsync(filterPattern, cancellationToken).ConfigureAwait(false);
        var sortedRepositories = SortRepositoriesByName(repositories);
        var pullRequestDetails = await LoadPullRequestDetailsAsync(
            sortedRepositories,
            authenticatedUser.Uuid,
            cancellationToken).ConfigureAwait(false);
        var mergedPullRequests = await LoadMergedPullRequestsAsync(
            sortedRepositories,
            reportOpenedAt,
            authenticatedUser.Uuid,
            cancellationToken).ConfigureAwait(false);

        ShowResultsHeader(sortedRepositories.Count);
        _consoleReportRenderer.RenderRepositoriesTable(sortedRepositories);
        _consoleReportRenderer.RenderPullRequestSnapshotsTableIfAny(sortedRepositories);
        _consoleReportRenderer.RenderMergedPullRequestsTableIfAny(mergedPullRequests);
        _consoleReportRenderer.RenderPullRequestDetailsReportIfAny(pullRequestDetails);
        _consoleReportRenderer.RenderAbandonedRepositoriesTableIfAny(sortedRepositories);
        RenderHtmlReport(sortedRepositories, mergedPullRequests, pullRequestDetails, filterPattern);
        RenderPdfReport(sortedRepositories, mergedPullRequests, pullRequestDetails, filterPattern);
        _consoleReportRenderer.RenderTelemetrySummary(_telemetryService.GetSnapshot());
        executionTime.Stop();
        ShowDone(executionTime.Elapsed);
    }

    private static void ShowTitle() => AnsiConsole.MarkupLine("[bold green]Bitbucket Repository List[/]");

    private static void ShowDone(TimeSpan elapsed)
    {
        AnsiConsole.MarkupLine("\n[bold green]Done.[/]");
        AnsiConsole.MarkupLine($"[grey]Generation time:[/] [green]{elapsed:hh\\:mm\\:ss}[/]");
    }

    private async Task<BitbucketUser?> TryAuthenticateAsync(CancellationToken cancellationToken)
    {
        var authenticatedUser = default(BitbucketUser);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Checking authentication...", async _ =>
            {
                try
                {
                    authenticatedUser = await _bitbucketAuthApiClient.AuthSelfCheckAsync(cancellationToken).ConfigureAwait(false);
                    ShowAuthenticatedUser(authenticatedUser);
                }
                catch (HttpRequestException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Auth failed:[/] {Markup.Escape(ex.Message)}");
                }
            }).ConfigureAwait(false);

        return authenticatedUser;
    }

    private static void ShowAuthenticatedUser(BitbucketUser user)
    {
        var displayName = user.DisplayName.Value;
        var uuid = user.Uuid.ToString();

        AnsiConsole.MarkupLine($"[green]Auth OK[/] as [bold]{Markup.Escape(displayName)}[/]");
        AnsiConsole.MarkupLine($"[grey]UUID:[/] {Markup.Escape(uuid)}\n");
    }

    private static async Task<FilterPattern> ReadFilterPatternAsync(
        RepositorySearchMode defaultSearchMode,
        string? configuredSearchPhrase,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine(
            $"[grey]Search by repository name. Mode from settings: {Markup.Escape(defaultSearchMode.ToString())}. Empty phrase = all.[/]\n");

        if (!string.IsNullOrWhiteSpace(configuredSearchPhrase))
        {
            return new FilterPattern(configuredSearchPhrase.Trim(), defaultSearchMode);
        }

        var searchPhrase = (await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Search phrase:").AllowEmpty(),
            cancellationToken).ConfigureAwait(false) ?? string.Empty).Trim();

        return new FilterPattern(searchPhrase, defaultSearchMode);
    }

    private static void ShowFilterInfo(FilterPattern filterPattern)
    {
        AnsiConsole.MarkupLine(
            filterPattern.HasFilter
                ? $"[grey]Filter:[/] {GetSearchModeText(filterPattern.SearchMode)} [yellow]\"{Markup.Escape(filterPattern.Phrase!)}\"[/]\n"
                : "[grey]Filter:[/] (none) - showing all repositories\n"
        );
    }

    private static string GetSearchModeText(RepositorySearchMode searchMode) =>
        searchMode == RepositorySearchMode.StartWith ? "starts with" : "contains";

    private async Task<IReadOnlyList<Repository>> LoadRepositoriesAsync(FilterPattern filterPattern, CancellationToken cancellationToken)
    {
        IReadOnlyList<Repository> repositories = [];
        RepoLoadProgress? lastProgress = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Loading repositories...", async ctx =>
            {
                var progress = new Progress<RepoLoadProgress>(p =>
                {
                    lastProgress = p;
                    _ = p.IsLoadingPullRequestStatistics
                        ? ctx.Status(
                            $"Loading PR statistics... {p.PullRequestStatisticsLoaded}/{p.PullRequestStatisticsTotal} (matched: {p.Matched})")
                        : ctx.Status($"Loading repositories... seen: {p.Seen}, matched: {p.Matched}");
                });

                repositories = await _repoService.GetRepositoriesAsync(filterPattern, progress, cancellationToken).ConfigureAwait(false);

                _ = lastProgress is not null
                    ? ctx.Status($"Loaded. seen: {lastProgress.Seen}, matched: {lastProgress.Matched}")
                    : ctx.Status("Loaded.");
            }).ConfigureAwait(false);

        return repositories;
    }

    private static List<Repository> SortRepositoriesByName(IReadOnlyList<Repository> repositories) =>
    [
        .. repositories.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
    ];

    private async Task<IReadOnlyList<PullRequestDetail>> LoadPullRequestDetailsAsync(
        List<Repository> repositories,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        if (!_options.PullRequestDetails.IsEnabled || repositories.Count == 0)
        {
            return [];
        }

        IReadOnlyList<PullRequestDetail> pullRequestDetails = [];
        PullRequestRepositoryLoadProgress? lastProgress = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Loading Open PR details...", async ctx =>
            {
                var progress = new Progress<PullRequestRepositoryLoadProgress>(p =>
                {
                    lastProgress = p;
                    _ = ctx.Status($"Loading Open PR details... {p.LoadedRepositories}/{p.TotalRepositories} repositories");
                });

                pullRequestDetails = await _repoService
                    .GetOpenPullRequestDetailsAsync(repositories, currentUserId, progress, cancellationToken)
                    .ConfigureAwait(false);

                _ = lastProgress is not null
                    ? ctx.Status($"Loaded Open PR details for {lastProgress.TotalRepositories} repositories.")
                    : ctx.Status("Loaded Open PR details.");
            }).ConfigureAwait(false);

        return pullRequestDetails;
    }

    private async Task<IReadOnlyList<MergedPullRequest>> LoadMergedPullRequestsAsync(
        List<Repository> repositories,
        DateTimeOffset reportOpenedAt,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        if (!_options.MergedPullRequests.IsEnabled || repositories.Count == 0)
        {
            return [];
        }

        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        PullRequestRepositoryLoadProgress? lastProgress = null;
        var mergedSince = reportOpenedAt.AddDays(-_options.MergedPullRequests.Days);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Loading recently merged PRs...", async ctx =>
            {
                var progress = new Progress<PullRequestRepositoryLoadProgress>(p =>
                {
                    lastProgress = p;
                    _ = ctx.Status($"Loading recently merged PRs... {p.LoadedRepositories}/{p.TotalRepositories} repositories");
                });

                mergedPullRequests = await _repoService
                    .GetMergedPullRequestsAsync(repositories, mergedSince, currentUserId, progress, cancellationToken)
                    .ConfigureAwait(false);

                _ = lastProgress is not null
                    ? ctx.Status($"Loaded recently merged PRs for {lastProgress.TotalRepositories} repositories.")
                    : ctx.Status("Loaded recently merged PRs.");
            }).ConfigureAwait(false);

        return mergedPullRequests;
    }

    private void ShowResultsHeader(int resultCount)
    {
        AnsiConsole.MarkupLine($"\n[bold]Workspace:[/] [green]{Markup.Escape(_options.Workspace)}[/]");
        AnsiConsole.MarkupLine($"[bold]Results:[/] [green]{resultCount}[/] (sorted by name)\n");
    }

    private void RenderPdfReport(
        List<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = _reportDataFactory.Create(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            filterPattern,
            DateTimeOffset.Now);

        _pdfReportRenderer.RenderReport(reportData);
    }

    private void RenderHtmlReport(
        List<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = _reportDataFactory.Create(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            filterPattern,
            DateTimeOffset.Now);

        _htmlReportRenderer.RenderReport(reportData);
    }

    private readonly IBitbucketAuthApiClient _bitbucketAuthApiClient;
    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IRepositoryReportDataFactory _reportDataFactory;
    private readonly IConsoleReportRenderer _consoleReportRenderer;
    private readonly IRepoService _repoService;
    private readonly IBitbucketTelemetryService _telemetryService;
    private readonly BitbucketOptions _options;

}
