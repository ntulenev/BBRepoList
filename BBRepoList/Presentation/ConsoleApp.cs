using System.Globalization;

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
    /// <param name="options">Bitbucket configuration options.</param>
    public ConsoleApp(IBitbucketAuthApiClient bitbucketAuthApiClient,
                      IHtmlReportRenderer htmlReportRenderer,
                      IPdfReportRenderer pdfReportRenderer,
                      IRepoService repoService,
                      IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bitbucketAuthApiClient);
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(options);

        _bitbucketAuthApiClient = bitbucketAuthApiClient;
        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
        _repoService = repoService;
        _options = options.Value;
    }

    /// <summary>
    /// Runs the console UI flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
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

        ShowResultsHeader(sortedRepositories.Count);
        RenderRepositoriesTable(sortedRepositories);
        RenderOpenPullRequestsTableIfAny(sortedRepositories);
        RenderPullRequestDetailsReportIfAny(pullRequestDetails);
        RenderAbandonedRepositoriesTableIfAny(sortedRepositories);
        RenderHtmlReport(sortedRepositories, pullRequestDetails, filterPattern);
        RenderPdfReport(sortedRepositories, pullRequestDetails, filterPattern);
        ShowDone();
    }

    private static void ShowTitle() => AnsiConsole.MarkupLine("[bold green]Bitbucket Repository List[/]");

    private static void ShowDone() => AnsiConsole.MarkupLine("\n[bold green]Done.[/]");

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
        PullRequestDetailsLoadProgress? lastProgress = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Loading Open PR details...", async ctx =>
            {
                var progress = new Progress<PullRequestDetailsLoadProgress>(p =>
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

    private static void RenderOpenPullRequestsTableIfAny(List<Repository> sortedRepositories)
    {
        var repositoriesWithOpenPullRequests = sortedRepositories
            .Where(static repository => repository.OpenPullRequestsCount > 0)
            .OrderBy(static repository => repository.CreatedOn ?? DateTimeOffset.MaxValue)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (repositoriesWithOpenPullRequests.Count == 0)
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

        for (var i = 0; i < repositoriesWithOpenPullRequests.Count; i++)
        {
            var repository = repositoriesWithOpenPullRequests[i];
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

        var ttfrThreshold = TimeSpan.FromHours(_options.PullRequestDetails.TtfrThresholdHours);
        var minimalDescriptionTextLength = _options.PullRequestDetails.MinimalDescriptionTextLength;
        var asOf = DateTimeOffset.UtcNow;

        for (var i = 0; i < pullRequestDetails.Count; i++)
        {
            var detail = pullRequestDetails[i];
            var openedOn = detail.OpenedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var openDuration = detail.GetOpenDuration(asOf);
            var openFor = FormatDuration(openDuration);
            var ttfr = detail.TimeToFirstResponse;
            var isTtfrPendingOverdue = ttfr is null && openDuration > ttfrThreshold;
            var ttfrCell = ttfr is not null
                ? Markup.Escape(FormatDuration(ttfr.Value))
                : isTtfrPendingOverdue
                    ? "[red]ALERT[/]"
                    : "-";
            var lastActivityAge = detail.GetLastActivityAge(asOf);
            var lastActivityCell = lastActivityAge is null
                ? "-"
                : Markup.Escape(FormatDuration(lastActivityAge.Value));
            var authorCell = Markup.Escape(string.Join('\n', PresentationHelpers.SplitCompactDisplayName(detail.AuthorDisplayName)));
            var pullRequestText = Markup.Escape(
                $"#{detail.PullRequestId.ToString(CultureInfo.InvariantCulture)}\n{detail.Title}");
            var descriptionLength = detail.DescriptionText?.Length ?? 0;
            var descriptionLengthCell = detail.HasShortOrMissingDescription(minimalDescriptionTextLength)
                ? $"[red]{descriptionLength.ToString(CultureInfo.InvariantCulture)}[/]"
                : descriptionLength.ToString(CultureInfo.InvariantCulture);
            var requestChangesText = Markup.Escape(
                PresentationHelpers.FormatRequestChangesText(detail.RequestChangesCount));
            var approvalsText = Markup.Escape(
                PresentationHelpers.FormatApprovalsText(detail.ApprovalsCount));
            var myActivityText = GetMyActivityMarkup(detail);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(detail.RepositoryName),
                pullRequestText,
                authorCell,
                descriptionLengthCell,
                Markup.Escape(openedOn),
                Markup.Escape(openFor),
                ttfrCell,
                lastActivityCell,
                detail.CommentsCount.ToString(CultureInfo.InvariantCulture),
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
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = new RepositoryPdfReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            _options.LoadAbandonedRepositoriesStatistics,
            _options.PullRequestDetails.TtfrThresholdHours,
            _options.PullRequestDetails.MinimalDescriptionTextLength,
            DateTimeOffset.Now,
            repositories,
            pullRequestDetails);

        _pdfReportRenderer.RenderReport(reportData);
    }

    private void RenderHtmlReport(
        List<Repository> repositories,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        FilterPattern filterPattern)
    {
        var reportData = new RepositoryPdfReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            _options.LoadAbandonedRepositoriesStatistics,
            _options.PullRequestDetails.TtfrThresholdHours,
            _options.PullRequestDetails.MinimalDescriptionTextLength,
            DateTimeOffset.Now,
            repositories,
            pullRequestDetails);

        _htmlReportRenderer.RenderReport(reportData);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var safeDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

        if (safeDuration.TotalDays >= 1)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}d {1}h {2}m",
                (int)safeDuration.TotalDays,
                safeDuration.Hours,
                safeDuration.Minutes);
        }

        if (safeDuration.TotalHours >= 1)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}h {1}m",
                (int)safeDuration.TotalHours,
                safeDuration.Minutes);
        }

        if (safeDuration.TotalMinutes >= 1)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}m", (int)safeDuration.TotalMinutes);
        }

        return "<1m";
    }

    private static string GetMyActivityMarkup(PullRequestDetail detail)
    {
        if (!detail.HasCurrentUserActivity)
        {
            return "-";
        }

        var parts = new List<string>(3);

        if (detail.HasCurrentUserDiscussion)
        {
            parts.Add("[yellow]💬[/]");
        }

        if (detail.HasCurrentUserRequestChanges)
        {
            parts.Add("[red]❌[/]");
        }

        if (detail.HasCurrentUserApproval)
        {
            parts.Add("[green]✅[/]");
        }

        return string.Join(" ", parts);
    }

    private readonly IBitbucketAuthApiClient _bitbucketAuthApiClient;
    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IRepoService _repoService;
    private readonly BitbucketOptions _options;

}
