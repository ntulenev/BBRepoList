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
        AnsiConsole.MarkupLine("[bold green]Bitbucket Repository List[/]");

        var authOk = true;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
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

                var displayName = user.DisplayName.Value;
                var uuid = user.Uuid.ToString();

                AnsiConsole.MarkupLine($"[green]Auth OK[/] as [bold]{Markup.Escape(displayName)}[/]");
                AnsiConsole.MarkupLine($"[grey]UUID:[/] {Markup.Escape(uuid)}\n");
            }).ConfigureAwait(false);

        if (!authOk)
        {
            return;
        }

        AnsiConsole.MarkupLine("[grey]Search by repository name (Contains, case-insensitive). Empty = all.[/]\n");

        var searchPhrase = (await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Search phrase:").AllowEmpty(),
            cancellationToken).ConfigureAwait(false) ?? string.Empty).Trim();

        var filterPattern = new FilterPattern(searchPhrase);

        AnsiConsole.MarkupLine(
            filterPattern.HasFilter
                ? $"[grey]Filter:[/] contains [yellow]\"{Markup.Escape(searchPhrase)}\"[/]\n"
                : "[grey]Filter:[/] (none) — showing all repositories\n"
        );

        IReadOnlyList<Repository> all = [];
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

                all = await _repoService.GetRepositoriesAsync(filterPattern, progress, cancellationToken).ConfigureAwait(false);

                _ = lastProgress is not null
                    ? ctx.Status($"Loaded. seen: {lastProgress.Seen}, matched: {lastProgress.Matched}")
                    : ctx.Status("Loaded.");
            }).ConfigureAwait(false);

        var sorted = all
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AnsiConsole.MarkupLine($"\n[bold]Workspace:[/] [green]{Markup.Escape(_options.Workspace)}[/]");
        AnsiConsole.MarkupLine($"[bold]Results:[/] [green]{sorted.Count}[/] (sorted by name)\n");

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository name[/]"));

        for (var i = 0; i < sorted.Count; i++)
        {
            _ = table.AddRow((i + 1).ToString(CultureInfo.InvariantCulture), Markup.Escape(sorted[i].Name));
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("\n[bold green]Done.[/]");
    }

    private readonly IBitbucketApiClient _bitbucketApiClient;
    private readonly IRepoService _repoService;
    private readonly BitbucketOptions _options;
}
