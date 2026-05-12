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
    /// <param name="consoleReportRenderer">Console report renderer.</param>
    /// <param name="reportWorkflow">Repository report workflow.</param>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    /// <param name="timeProvider">Time provider for report timestamps.</param>
    public ConsoleApp(IBitbucketAuthApiClient bitbucketAuthApiClient,
                      IConsoleReportRenderer consoleReportRenderer,
                      IRepositoryReportWorkflow reportWorkflow,
                      IBitbucketTelemetryService telemetryService,
                      IOptions<BitbucketOptions> options,
                      TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(bitbucketAuthApiClient);
        ArgumentNullException.ThrowIfNull(consoleReportRenderer);
        ArgumentNullException.ThrowIfNull(reportWorkflow);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bitbucketAuthApiClient = bitbucketAuthApiClient;
        _consoleReportRenderer = consoleReportRenderer;
        _reportWorkflow = reportWorkflow;
        _telemetryService = telemetryService;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs the console UI flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var executionTime = Stopwatch.StartNew();
        var reportOpenedAt = _timeProvider.GetUtcNow();
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

        var generationResult = await GenerateReportAsync(
            filterPattern,
            authenticatedUser.Uuid,
            reportOpenedAt,
            cancellationToken).ConfigureAwait(false);

        ShowResultsHeader(generationResult.Repositories.Count);
        _consoleReportRenderer.RenderRepositoriesTable(generationResult.Repositories);
        _consoleReportRenderer.RenderPullRequestSnapshotsTableIfAny(generationResult.Repositories);
        _consoleReportRenderer.RenderMergedPullRequestsTableIfAny(generationResult.MergedPullRequests);
        _consoleReportRenderer.RenderPullRequestDetailsReportIfAny(generationResult.PullRequestDetails);
        _consoleReportRenderer.RenderAbandonedRepositoriesTableIfAny(generationResult.Repositories);
        _reportWorkflow.RenderReports(generationResult.ReportData);
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

    private async Task<RepositoryReportGenerationResult> GenerateReportAsync(
        FilterPattern filterPattern,
        BitbucketId currentUserId,
        DateTimeOffset reportOpenedAt,
        CancellationToken cancellationToken)
    {
        RepoLoadProgress? lastProgress = null;
        PullRequestRepositoryLoadProgress? lastPullRequestDetailsProgress = null;
        PullRequestRepositoryLoadProgress? lastMergedPullRequestsProgress = null;
        RepositoryReportGenerationResult? result = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Loading report data...", async ctx =>
            {
                var progress = new Progress<RepoLoadProgress>(p =>
                {
                    lastProgress = p;
                    _ = p.IsLoadingPullRequestStatistics
                        ? ctx.Status(
                            $"Loading PR statistics... {p.PullRequestStatisticsLoaded}/{p.PullRequestStatisticsTotal} (matched: {p.Matched})")
                        : ctx.Status($"Loading repositories... seen: {p.Seen}, matched: {p.Matched}");
                });
                var pullRequestDetailsProgress = new Progress<PullRequestRepositoryLoadProgress>(p =>
                {
                    lastPullRequestDetailsProgress = p;
                    _ = ctx.Status($"Loading Open PR details... {p.LoadedRepositories}/{p.TotalRepositories} repositories");
                });
                var mergedPullRequestsProgress = new Progress<PullRequestRepositoryLoadProgress>(p =>
                {
                    lastMergedPullRequestsProgress = p;
                    _ = ctx.Status($"Loading recently merged PRs... {p.LoadedRepositories}/{p.TotalRepositories} repositories");
                });

                result = await _reportWorkflow
                    .GenerateAsync(
                        filterPattern,
                        currentUserId,
                        reportOpenedAt,
                        _timeProvider.GetLocalNow(),
                        progress,
                        pullRequestDetailsProgress,
                        mergedPullRequestsProgress,
                        cancellationToken)
                    .ConfigureAwait(false);

                _ = ctx.Status(BuildLoadedStatus(
                    lastProgress,
                    lastPullRequestDetailsProgress,
                    lastMergedPullRequestsProgress));
            }).ConfigureAwait(false);

        return result!;
    }

    private void ShowResultsHeader(int resultCount)
    {
        AnsiConsole.MarkupLine($"\n[bold]Workspace:[/] [green]{Markup.Escape(_options.Workspace)}[/]");
        AnsiConsole.MarkupLine($"[bold]Results:[/] [green]{resultCount}[/] (sorted by name)\n");
    }

    private static string BuildLoadedStatus(
        RepoLoadProgress? repositoryProgress,
        PullRequestRepositoryLoadProgress? pullRequestDetailsProgress,
        PullRequestRepositoryLoadProgress? mergedPullRequestsProgress)
    {
        var status = repositoryProgress is not null
            ? $"Loaded. seen: {repositoryProgress.Seen}, matched: {repositoryProgress.Matched}"
            : "Loaded.";

        if (pullRequestDetailsProgress is not null)
        {
            status += $" Open PR details: {pullRequestDetailsProgress.TotalRepositories} repositories.";
        }

        if (mergedPullRequestsProgress is not null)
        {
            status += $" Recently merged PRs: {mergedPullRequestsProgress.TotalRepositories} repositories.";
        }

        return status;
    }

    private readonly IBitbucketAuthApiClient _bitbucketAuthApiClient;
    private readonly IConsoleReportRenderer _consoleReportRenderer;
    private readonly IRepositoryReportWorkflow _reportWorkflow;
    private readonly IBitbucketTelemetryService _telemetryService;
    private readonly BitbucketOptions _options;
    private readonly TimeProvider _timeProvider;

}
