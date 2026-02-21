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
    /// <param name="bitbucketApiClient">Bitbucket API client.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    /// <param name="repoService">Repository loading service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public ConsoleApp(IBitbucketApiClient bitbucketApiClient,
                      IPdfReportRenderer pdfReportRenderer,
                      IRepoService repoService,
                      IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bitbucketApiClient);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(options);

        _bitbucketApiClient = bitbucketApiClient;
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

        if (!await TryAuthenticateAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var filterPattern = await ReadFilterPatternAsync(cancellationToken).ConfigureAwait(false);
        ShowFilterInfo(filterPattern);

        var repositories = await LoadRepositoriesAsync(filterPattern, cancellationToken).ConfigureAwait(false);
        var sortedRepositories = SortRepositoriesByName(repositories);

        ShowResultsHeader(sortedRepositories.Count);
        RenderRepositoriesTable(sortedRepositories);
        RenderOpenPullRequestsTableIfAny(sortedRepositories);
        RenderAbandonedRepositoriesTableIfAny(sortedRepositories);
        RenderPdfReport(sortedRepositories, filterPattern);
        ShowDone();
    }

    private readonly IBitbucketApiClient _bitbucketApiClient;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IRepoService _repoService;
    private readonly BitbucketOptions _options;

    private static void ShowTitle() => AnsiConsole.MarkupLine("[bold green]Bitbucket Repository List[/]");

    private static void ShowDone() => AnsiConsole.MarkupLine("\n[bold green]Done.[/]");

    private async Task<bool> TryAuthenticateAsync(CancellationToken cancellationToken)
    {
        var authOk = true;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Checking authentication...", async _ =>
            {
                try
                {
                    var user = await _bitbucketApiClient.AuthSelfCheckAsync(cancellationToken).ConfigureAwait(false);
                    ShowAuthenticatedUser(user);
                }
                catch (HttpRequestException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Auth failed:[/] {Markup.Escape(ex.Message)}");
                    authOk = false;
                }
            }).ConfigureAwait(false);

        return authOk;
    }

    private static void ShowAuthenticatedUser(BitbucketUser user)
    {
        var displayName = user.DisplayName.Value;
        var uuid = user.Uuid.ToString();

        AnsiConsole.MarkupLine($"[green]Auth OK[/] as [bold]{Markup.Escape(displayName)}[/]");
        AnsiConsole.MarkupLine($"[grey]UUID:[/] {Markup.Escape(uuid)}\n");
    }

    private static async Task<FilterPattern> ReadFilterPatternAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[grey]Search by repository name (Contains, case-insensitive). Empty = all.[/]\n");

        var searchPhrase = (await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Search phrase:").AllowEmpty(),
            cancellationToken).ConfigureAwait(false) ?? string.Empty).Trim();

        return new FilterPattern(searchPhrase);
    }

    private static void ShowFilterInfo(FilterPattern filterPattern)
    {
        AnsiConsole.MarkupLine(
            filterPattern.HasFilter
                ? $"[grey]Filter:[/] contains [yellow]\"{Markup.Escape(filterPattern.Phrase!)}\"[/]\n"
                : "[grey]Filter:[/] (none) - showing all repositories\n"
        );
    }

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
                    _ = ctx.Status($"Loading... seen: {p.Seen}, matched: {p.Matched}");
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
            var openPullRequests = repository.OpenPullRequestsCount?.ToString(CultureInfo.InvariantCulture) ?? "-";

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
            .Where(static repository => repository.OpenPullRequestsCount.GetValueOrDefault() > 0)
            .OrderByDescending(static repository => repository.OpenPullRequestsCount)
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
            .AddColumn(new TableColumn("[green]Open pull requests[/]"));

        for (var i = 0; i < repositoriesWithOpenPullRequests.Count; i++)
        {
            var repository = repositoriesWithOpenPullRequests[i];
            var openPullRequests = repository.OpenPullRequestsCount?.ToString(CultureInfo.InvariantCulture) ?? "-";

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(repository.Name),
                Markup.Escape(openPullRequests));
        }

        AnsiConsole.Write(table);
    }

    private void RenderAbandonedRepositoriesTableIfAny(List<Repository> sortedRepositories)
    {
        var now = DateTimeOffset.UtcNow;

        var abandonedRepositories = sortedRepositories
            .Select(repository =>
            {
                var lastActivityOn = repository.LastUpdatedOn ?? repository.CreatedOn;
                if (lastActivityOn is null)
                {
                    return null;
                }

                var monthsWithoutActivity = CalculateFullMonthsBetween(lastActivityOn.Value, now);
                return new AbandonedRepositoryRow(repository, lastActivityOn.Value, monthsWithoutActivity);
            })
            .Where(row => row is not null && row.MonthsWithoutActivity > _options.AbandonedMonthsThreshold)
            .Select(static row => row!)
            .OrderByDescending(static row => row.MonthsWithoutActivity)
            .ThenBy(static row => row.Repository.Name, StringComparer.OrdinalIgnoreCase)
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
            var row = abandonedRepositories[i];
            var createdOn = row.Repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
            var lastActivityOn = row.LastActivityOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var monthsWithoutActivity = row.MonthsWithoutActivity.ToString(CultureInfo.InvariantCulture);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(row.Repository.Name),
                Markup.Escape(createdOn),
                Markup.Escape(lastActivityOn),
                Markup.Escape(monthsWithoutActivity));
        }

        AnsiConsole.Write(table);
    }

    private static int CalculateFullMonthsBetween(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from)
        {
            return 0;
        }

        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;

        if (to.Day < from.Day)
        {
            months--;
        }

        return Math.Max(months, 0);
    }

    private void RenderPdfReport(List<Repository> repositories, FilterPattern filterPattern)
    {
        var reportData = new RepositoryPdfReportData(
            _options.Workspace,
            filterPattern.Phrase,
            _options.AbandonedMonthsThreshold,
            DateTimeOffset.Now,
            repositories);

        _pdfReportRenderer.RenderReport(reportData);
    }

}
