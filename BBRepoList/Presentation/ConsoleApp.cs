using System.Globalization;
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
    /// <param name="repoService">Repository loading service.</param>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public ConsoleApp(IBitbucketAuthApiClient bitbucketAuthApiClient,
                      IHtmlReportRenderer htmlReportRenderer,
                      IPdfReportRenderer pdfReportRenderer,
                      IRepoService repoService,
                      IBitbucketTelemetryService telemetryService,
                      IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bitbucketAuthApiClient);
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(options);

        _bitbucketAuthApiClient = bitbucketAuthApiClient;
        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
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
        RenderRepositoriesTable(sortedRepositories);
        RenderPullRequestSnapshotsTableIfAny(sortedRepositories);
        RenderMergedPullRequestsTableIfAny(mergedPullRequests);
        RenderPullRequestDetailsReportIfAny(pullRequestDetails);
        RenderAbandonedRepositoriesTableIfAny(sortedRepositories);
        RenderHtmlReport(sortedRepositories, mergedPullRequests, pullRequestDetails, filterPattern);
        RenderPdfReport(sortedRepositories, mergedPullRequests, pullRequestDetails, filterPattern);
        RenderTelemetrySummary();
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

    private static void RenderRepositoriesTable(List<Repository> sortedRepositories)
    {
        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository name[/]"))
            .AddColumn(new TableColumn("[green]Created on[/]"))
            .AddColumn(new TableColumn("[green]Last updated[/]"))
            .AddColumn(new TableColumn("[green]Open pull requests[/]"));

        for (var i = 0; i < sortedRepositories.Count; i++)
        {
            var repository = sortedRepositories[i];
            var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var lastUpdated = repository.LastUpdatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var openPullRequests = repository.OpenPullRequestsCount.ToString(CultureInfo.InvariantCulture);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(repository.Name),
                Markup.Escape(createdOn),
                Markup.Escape(lastUpdated),
                Markup.Escape(openPullRequests));
        }

        AnsiConsole.Write(table);
    }

    private static void RenderPullRequestSnapshotsTableIfAny(List<Repository> sortedRepositories)
    {
        var repositoriesWithPullRequestSnapshots = sortedRepositories
            .Where(static repository => repository.OpenPullRequestsCount > 0)
            .OrderBy(static repository => repository.CreatedOn ?? DateTimeOffset.MaxValue)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (repositoriesWithPullRequestSnapshots.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("\n[bold]Repositories with open pull requests[/]\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository name[/]"))
            .AddColumn(new TableColumn("[green]Created on[/]"))
            .AddColumn(new TableColumn("[green]Open pull requests[/]"));

        for (var i = 0; i < repositoriesWithPullRequestSnapshots.Count; i++)
        {
            var repository = repositoriesWithPullRequestSnapshots[i];
            var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var openPullRequests = repository.OpenPullRequestsCount.ToString(CultureInfo.InvariantCulture);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(repository.Name),
                Markup.Escape(createdOn),
                Markup.Escape(openPullRequests));
        }

        AnsiConsole.Write(table);
    }

    private void RenderMergedPullRequestsTableIfAny(IReadOnlyList<MergedPullRequest> mergedPullRequests)
    {
        if (!_options.MergedPullRequests.IsEnabled || mergedPullRequests.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine(
            $"\n[bold]Recently merged pull requests[/] [grey](last {_options.MergedPullRequests.Days} days)[/]\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository[/]"))
            .AddColumn(new TableColumn("[green]PR[/]").Width(28))
            .AddColumn(new TableColumn("[green]🧑‍💻[/]"))
            .AddColumn(new TableColumn("[green]Description len[/]"))
            .AddColumn(new TableColumn("[green]Opened on[/]"))
            .AddColumn(new TableColumn("[green]Open for[/]"))
            .AddColumn(new TableColumn("[green]TTFR[/]"))
            .AddColumn(new TableColumn("[green]Merged[/]"))
            .AddColumn(new TableColumn("[green]Merged on[/]"))
            .AddColumn(new TableColumn("[green]💬[/]"))
            .AddColumn(new TableColumn("[green]RC[/]"))
            .AddColumn(new TableColumn("[green]AP[/]"))
            .AddColumn(new TableColumn("[green]Me[/]"));

        var minimalDescriptionTextLength = _options.PullRequestDetails.MinimalDescriptionTextLength;
        var asOf = DateTimeOffset.UtcNow;

        for (var i = 0; i < mergedPullRequests.Count; i++)
        {
            var row = PullRequestReportRow.FromMergedPullRequest(
                mergedPullRequests[i],
                asOf,
                minimalDescriptionTextLength);
            var pullRequestText = Markup.Escape(
                $"{row.PullRequestNumberText}\n{row.Title}");
            var authorCell = Markup.Escape(string.Join('\n', row.AuthorDisplayNameLines));
            var descriptionLengthCell = row.IsDescriptionShort
                ? $"[red]{row.DescriptionLengthText}[/]"
                : row.DescriptionLengthText;
            var ttfrCell = Markup.Escape(row.TimeToFirstResponseText);
            var requestChangesText = Markup.Escape(row.RequestChangesText);
            var approvalsText = Markup.Escape(row.ApprovalsText);
            var myActivityText = GetMyActivityMarkup(row);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(row.RepositoryName),
                pullRequestText,
                authorCell,
                descriptionLengthCell,
                Markup.Escape(row.OpenedOnText),
                Markup.Escape(row.OpenDurationText),
                ttfrCell,
                Markup.Escape(row.ActivityAgeText),
                Markup.Escape(row.MergedOnText ?? "-"),
                row.CommentsCountText,
                requestChangesText,
                approvalsText,
                myActivityText);
        }

        AnsiConsole.Write(table);
    }

    private void RenderPullRequestDetailsReportIfAny(IReadOnlyList<PullRequestDetail> pullRequestDetails)
    {
        if (!_options.PullRequestDetails.IsEnabled || pullRequestDetails.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("\n[bold]Open PR details[/]\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository[/]"))
            .AddColumn(new TableColumn("[green]PR[/]").Width(28))
            .AddColumn(new TableColumn("[green]🧑‍💻[/]"))
            .AddColumn(new TableColumn("[green]Description len[/]"))
            .AddColumn(new TableColumn("[green]Opened on[/]"))
            .AddColumn(new TableColumn("[green]Open for[/]"))
            .AddColumn(new TableColumn("[green]TTFR[/]"))
            .AddColumn(new TableColumn("[green]Updated[/]"))
            .AddColumn(new TableColumn("[green]💬[/]"))
            .AddColumn(new TableColumn("[green]RC[/]"))
            .AddColumn(new TableColumn("[green]AP[/]"))
            .AddColumn(new TableColumn("[green]Me[/]"));

        var ttfrThresholdHours = _options.PullRequestDetails.TtfrThresholdHours;
        var minimalDescriptionTextLength = _options.PullRequestDetails.MinimalDescriptionTextLength;
        var asOf = DateTimeOffset.UtcNow;

        for (var i = 0; i < pullRequestDetails.Count; i++)
        {
            var row = PullRequestReportRow.FromOpenPullRequest(
                pullRequestDetails[i],
                asOf,
                ttfrThresholdHours,
                minimalDescriptionTextLength);
            var ttfrCell = row.TimeToFirstResponse is not null
                ? Markup.Escape(row.TimeToFirstResponseText)
                : row.IsTtfrAlert
                    ? "[red]ALERT[/]"
                    : "-";
            var lastActivityCell = Markup.Escape(row.ActivityAgeText);
            var authorCell = Markup.Escape(string.Join('\n', row.AuthorDisplayNameLines));
            var pullRequestText = Markup.Escape(
                $"{row.PullRequestNumberText}\n{row.Title}");
            var descriptionLengthCell = row.IsDescriptionShort
                ? $"[red]{row.DescriptionLengthText}[/]"
                : row.DescriptionLengthText;
            var requestChangesText = Markup.Escape(row.RequestChangesText);
            var approvalsText = Markup.Escape(row.ApprovalsText);
            var myActivityText = GetMyActivityMarkup(row);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(row.RepositoryName),
                pullRequestText,
                authorCell,
                descriptionLengthCell,
                Markup.Escape(row.OpenedOnText),
                Markup.Escape(row.OpenDurationText),
                ttfrCell,
                lastActivityCell,
                row.CommentsCountText,
                requestChangesText,
                approvalsText,
                myActivityText);
        }

        AnsiConsole.Write(table);
    }

    private void RenderAbandonedRepositoriesTableIfAny(List<Repository> sortedRepositories)
    {
        if (!_options.LoadAbandonedRepositoriesStatistics)
        {
            return;
        }

        var abandonedRepositories = sortedRepositories
            .Where(repository => repository.CanCalculateInactivityTiming
                                 && repository.MonthsWithoutActivity > _options.AbandonedMonthsThreshold)
            .OrderByDescending(static repository => repository.MonthsWithoutActivity)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (abandonedRepositories.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine(
            $"\n[bold]Abandoned repositories[/] [grey](more than {_options.AbandonedMonthsThreshold} months without activity)[/]\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository name[/]"))
            .AddColumn(new TableColumn("[green]Created on[/]"))
            .AddColumn(new TableColumn("[green]Last activity on[/]"))
            .AddColumn(new TableColumn("[green]Months inactive[/]"));

        for (var i = 0; i < abandonedRepositories.Count; i++)
        {
            var repository = abandonedRepositories[i];
            var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var lastActivityOn = repository.LastUpdatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var monthsWithoutActivity = repository.MonthsWithoutActivity.ToString(CultureInfo.InvariantCulture);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(repository.Name),
                Markup.Escape(createdOn),
                Markup.Escape(lastActivityOn),
                Markup.Escape(monthsWithoutActivity));
        }

        AnsiConsole.Write(table);
    }

    private void RenderPdfReport(
        List<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = new RepositoryReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            _options.LoadAbandonedRepositoriesStatistics,
            _options.PullRequestDetails.TtfrThresholdHours,
            _options.PullRequestDetails.MinimalDescriptionTextLength,
            _options.MergedPullRequests.IsEnabled,
            _options.MergedPullRequests.Days,
            DateTimeOffset.Now,
            repositories,
            mergedPullRequests,
            pullRequestDetails);

        _pdfReportRenderer.RenderReport(reportData);
    }

    private void RenderHtmlReport(
        List<Repository> repositories,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = new RepositoryReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            _options.LoadAbandonedRepositoriesStatistics,
            _options.PullRequestDetails.TtfrThresholdHours,
            _options.PullRequestDetails.MinimalDescriptionTextLength,
            _options.MergedPullRequests.IsEnabled,
            _options.MergedPullRequests.Days,
            DateTimeOffset.Now,
            repositories,
            mergedPullRequests,
            pullRequestDetails);

        _htmlReportRenderer.RenderReport(reportData);
    }

    private void RenderTelemetrySummary()
    {
        var snapshot = _telemetryService.GetSnapshot();
        if (!snapshot.IsEnabled || snapshot.TotalRequests == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine(
            $"\n[bold]Bitbucket API request statistics[/] [grey](total: {snapshot.TotalRequests.ToString(CultureInfo.InvariantCulture)})[/]\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Bitbucket API[/]"))
            .AddColumn(new TableColumn("[green]Requests[/]").RightAligned());

        for (var i = 0; i < snapshot.RequestStatistics.Count; i++)
        {
            var statistic = snapshot.RequestStatistics[i];

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(statistic.ApiName),
                Markup.Escape(statistic.RequestCount.ToString(CultureInfo.InvariantCulture)));
        }

        AnsiConsole.Write(table);
    }

    private static string GetMyActivityMarkup(PullRequestReportRow row)
    {
        if (!row.HasCurrentUserActivity)
        {
            return "-";
        }

        var parts = new List<string>(3);

        if (row.HasCurrentUserDiscussion)
        {
            parts.Add("[yellow]\U0001F4AC[/]");
        }

        if (row.HasCurrentUserRequestChanges)
        {
            parts.Add("[red]\u274C[/]");
        }

        if (row.HasCurrentUserApproval)
        {
            parts.Add("[green]\u2705[/]");
        }

        return string.Join(" ", parts);
    }

    private readonly IBitbucketAuthApiClient _bitbucketAuthApiClient;
    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IRepoService _repoService;
    private readonly IBitbucketTelemetryService _telemetryService;
    private readonly BitbucketOptions _options;

}
