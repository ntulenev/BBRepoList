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
    private readonly IBitbucketApiClient _bitbucketApiClient;
    private readonly IRepoService _repoService;
    private readonly BitbucketOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleApp"/> class.
    /// </summary>
    /// <param name="bitbucketApiClient">Bitbucket API client.</param>
    /// <param name="repoService">Repository loading service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public ConsoleApp(IBitbucketApiClient bitbucketApiClient, IRepoService repoService, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bitbucketApiClient);
        ArgumentNullException.ThrowIfNull(repoService);
        ArgumentNullException.ThrowIfNull(options);

        _bitbucketApiClient = bitbucketApiClient;
        _repoService = repoService;
        _options = options.Value;
    }

    /// <summary>
    /// Runs the console UI flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold cyan]Bitbucket Repository Finder[/]");

        var authOk = true;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync("Checking authentication...", async _ =>
            {
                BitbucketUser user;
                try
                {
                    user = await _bitbucketApiClient.AuthSelfCheckAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Auth failed:[/] {Markup.Escape(ex.Message)}");
                    authOk = false;
                    return;
                }

                var displayName = user.DisplayName ?? user.Nickname;
                var accountId = user.AccountId;
                var uuid = user.Uuid;

                if (displayName is null && accountId is null && uuid is null)
                {
                    AnsiConsole.MarkupLine("[red]Auth failed:[/] user profile is unavailable.");
                    authOk = false;
                    return;
                }

                AnsiConsole.MarkupLine($"[green]Auth OK[/] as [bold]{Markup.Escape(displayName ?? "Unknown")}[/]");
                AnsiConsole.MarkupLine($"[grey]AccountId:[/] {Markup.Escape(accountId ?? "n/a")}  [grey]UUID:[/] {Markup.Escape(uuid ?? "n/a")}\n");
            });

        if (!authOk)
        {
            return;
        }

        AnsiConsole.MarkupLine("[grey]Search by repository name (Contains, case-insensitive). Empty = all.[/]\n");

        var searchPhrase = (await AnsiConsole.AskAsync<string>("Search phrase:", cancellationToken).ConfigureAwait(false) ?? "").Trim();
        var hasFilter = !string.IsNullOrWhiteSpace(searchPhrase);

        AnsiConsole.MarkupLine(
            hasFilter
                ? $"[grey]Filter:[/] contains [yellow]\"{Markup.Escape(searchPhrase)}\"[/]\n"
                : "[grey]Filter:[/] (none) — showing all repositories\n"
        );

        IReadOnlyList<Repository> all = [];
        RepoLoadProgress? lastProgress = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync("Loading repositories...", async ctx =>
            {
                var progress = new Progress<RepoLoadProgress>(p =>
                {
                    lastProgress = p;
                    _ = ctx.Status($"Loading... pages: {p.Pages}, seen: {p.Seen}, matched: {p.Matched}");
                });

                all = await _repoService.GetRepositoriesAsync(searchPhrase, progress, cancellationToken);

                _ = lastProgress is not null
                    ? ctx.Status($"Loaded. pages: {lastProgress.Pages}, seen: {lastProgress.Seen}, matched: {lastProgress.Matched}")
                    : ctx.Status("Loaded.");
            });

        var sorted = all.Take(3)
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AnsiConsole.MarkupLine($"\n[bold]Workspace:[/] [cyan]{Markup.Escape(_options.Workspace)}[/]");
        AnsiConsole.MarkupLine($"[bold]Results:[/] [green]{sorted.Count}[/] (sorted by name)\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand()
            .AddColumn(new TableColumn("#").Centered())
            .AddColumn(new TableColumn("Repository name"));

        for (var i = 0; i < sorted.Count; i++)
        {
            _ = table.AddRow((i + 1).ToString(CultureInfo.InvariantCulture), Markup.Escape(sorted[i].Name));
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("\n[bold green]Done.[/]");
    }
}
