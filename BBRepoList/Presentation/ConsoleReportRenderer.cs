using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using Spectre.Console;

namespace BBRepoList.Presentation;

/// <summary>
/// Default console report renderer.
/// </summary>
public sealed class ConsoleReportRenderer : IConsoleReportRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleReportRenderer"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    /// <param name="timeProvider">Time provider for age calculations.</param>
    public ConsoleReportRenderer(IOptions<BitbucketOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void RenderRepositoriesTable(IReadOnlyList<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository name[/]"))
            .AddColumn(new TableColumn("[green]Created on[/]"))
            .AddColumn(new TableColumn("[green]Last updated[/]"))
            .AddColumn(new TableColumn("[green]Open pull requests[/]"));

        for (var i = 0; i < repositories.Count; i++)
        {
            var repository = repositories[i];
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

    /// <inheritdoc />
    public void RenderPullRequestSnapshotsTableIfAny(IReadOnlyList<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var repositoriesWithPullRequestSnapshots = repositories
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

    /// <inheritdoc />
    public void RenderMergedPullRequestsTableIfAny(IReadOnlyList<MergedPullRequest> mergedPullRequests)
    {
        ArgumentNullException.ThrowIfNull(mergedPullRequests);

        if (!_options.MergedPullRequests.IsEnabled || mergedPullRequests.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine(
            $"\n[bold]Recently merged pull requests[/] [grey](last {_options.MergedPullRequests.Days} days)[/]\n");

        var table = CreatePullRequestTable("Merged", includeMergedOn: true);
        var minimalDescriptionTextLength = _options.PullRequestDetails.MinimalDescriptionTextLength;
        var asOf = _timeProvider.GetUtcNow();

        for (var i = 0; i < mergedPullRequests.Count; i++)
        {
            var row = PullRequestReportRow.FromMergedPullRequest(
                mergedPullRequests[i],
                asOf,
                minimalDescriptionTextLength);

            AddPullRequestRow(table, row, i, Markup.Escape(row.TimeToFirstResponseText), row.MergedOnText);
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void RenderPullRequestDetailsReportIfAny(IReadOnlyList<PullRequestDetail> pullRequestDetails)
    {
        ArgumentNullException.ThrowIfNull(pullRequestDetails);

        if (!_options.PullRequestDetails.IsEnabled || pullRequestDetails.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("\n[bold]Open PR details[/]\n");

        var table = CreatePullRequestTable("Updated", includeMergedOn: false);
        var ttfrThresholdHours = _options.PullRequestDetails.TtfrThresholdHours;
        var minimalDescriptionTextLength = _options.PullRequestDetails.MinimalDescriptionTextLength;
        var asOf = _timeProvider.GetUtcNow();

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

            AddPullRequestRow(table, row, i, ttfrCell, mergedOnText: null);
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void RenderAbandonedRepositoriesTableIfAny(IReadOnlyList<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        if (!_options.LoadAbandonedRepositoriesStatistics)
        {
            return;
        }

        var asOf = _timeProvider.GetUtcNow();
        var abandonedRepositories = repositories
            .Where(repository => repository.CanCalculateInactivityTiming
                                 && repository.CalculateMonthsWithoutActivity(asOf) > _options.AbandonedMonthsThreshold)
            .OrderByDescending(repository => repository.CalculateMonthsWithoutActivity(asOf))
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
            var monthsWithoutActivity = repository.CalculateMonthsWithoutActivity(asOf).ToString(CultureInfo.InvariantCulture);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(repository.Name),
                Markup.Escape(createdOn),
                Markup.Escape(lastActivityOn),
                Markup.Escape(monthsWithoutActivity));
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void RenderTelemetrySummary(BitbucketTelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

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

    private static Table CreatePullRequestTable(string activityColumnName, bool includeMergedOn)
    {
        var table = new Table()
            .Border(TableBorder.Double)
            .Expand()
            .AddColumn(new TableColumn("[green]#[/]").Centered())
            .AddColumn(new TableColumn("[green]Repository[/]"))
            .AddColumn(new TableColumn("[green]PR[/]").Width(28))
            .AddColumn(new TableColumn("[green]\U0001F9D1\u200D\U0001F4BB[/]"))
            .AddColumn(new TableColumn("[green]Description len[/]"))
            .AddColumn(new TableColumn("[green]Opened on[/]"))
            .AddColumn(new TableColumn("[green]Open for[/]"))
            .AddColumn(new TableColumn("[green]TTFR[/]"))
            .AddColumn(new TableColumn($"[green]{Markup.Escape(activityColumnName)}[/]"));

        if (includeMergedOn)
        {
            _ = table.AddColumn(new TableColumn("[green]Merged on[/]"));
        }

        return table
            .AddColumn(new TableColumn("[green]\U0001F4AC[/]"))
            .AddColumn(new TableColumn("[green]RC[/]"))
            .AddColumn(new TableColumn("[green]AP[/]"))
            .AddColumn(new TableColumn("[green]Me[/]"));
    }

    private static void AddPullRequestRow(
        Table table,
        PullRequestReportRow row,
        int index,
        string ttfrCell,
        string? mergedOnText)
    {
        var pullRequestText = Markup.Escape($"{row.PullRequestNumberText}\n{row.Title}");
        var authorCell = Markup.Escape(string.Join('\n', row.AuthorDisplayNameLines));
        var descriptionLengthCell = row.IsDescriptionShort
            ? $"[red]{row.DescriptionLengthText}[/]"
            : row.DescriptionLengthText;
        var requestChangesText = Markup.Escape(row.RequestChangesText);
        var approvalsText = Markup.Escape(row.ApprovalsText);
        var myActivityText = GetMyActivityMarkup(row);
        var cells = new List<string>
        {
            (index + 1).ToString(CultureInfo.InvariantCulture),
            Markup.Escape(row.RepositoryName),
            pullRequestText,
            authorCell,
            descriptionLengthCell,
            Markup.Escape(row.OpenedOnText),
            Markup.Escape(row.OpenDurationText),
            ttfrCell,
            Markup.Escape(row.ActivityAgeText)
        };

        if (mergedOnText is not null)
        {
            cells.Add(Markup.Escape(mergedOnText));
        }

        cells.Add(row.CommentsCountText);
        cells.Add(requestChangesText);
        cells.Add(approvalsText);
        cells.Add(myActivityText);

        _ = table.AddRow([.. cells]);
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

    private readonly BitbucketOptions _options;
    private readonly TimeProvider _timeProvider;
}
