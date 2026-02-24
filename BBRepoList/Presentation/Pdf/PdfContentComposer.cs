using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace BBRepoList.Presentation.Pdf;

/// <summary>
/// Default PDF content composer for repository report.
/// </summary>
public sealed class PdfContentComposer : IPdfContentComposer
{
    /// <inheritdoc />
    public void ComposeContent(ColumnDescriptor column, RepositoryPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        column.Spacing(10);

        ComposeRepositoriesSection(column, reportData.Repositories, reportData.Workspace);
        ComposeOpenPullRequestsSection(column, reportData.Repositories, reportData.Workspace);
        ComposePullRequestDetailsSection(
            column,
            reportData.PullRequestDetails,
            reportData.Workspace,
            reportData.TtfrThresholdHours,
            reportData.GeneratedAt);
        ComposeAbandonedRepositoriesSection(
            column,
            reportData.Repositories,
            reportData.Workspace,
            reportData.AbandonedMonthsThreshold);
    }

    private static void ComposeRepositoriesSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace)
    {
        _ = column.Item().Text("Repositories").Bold().FontSize(12);

        if (repositories.Count == 0)
        {
            _ = column.Item().Text("No repositories found.").FontColor(Colors.Grey.Darken1);
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.RelativeColumn(3f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Last updated");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open PRs");
            });

            for (var i = 0; i < repositories.Count; i++)
            {
                var repository = repositories[i];
                var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var updatedOn = repository.LastUpdatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var openPrs = repository.OpenPullRequestsCount?.ToString(CultureInfo.InvariantCulture) ?? "-";
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, repository.Slug);
                var pullRequestsUrl = PdfPresentationHelpers.BuildPullRequestsUrl(workspace, repository.Slug);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(repository.Name)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(repository.Name);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(updatedOn);
                _ = repository.OpenPullRequestsCount is null || string.IsNullOrWhiteSpace(pullRequestsUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openPrs)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(pullRequestsUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(openPrs);
            }
        });
    }

    private static void ComposeOpenPullRequestsSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace)
    {
        var repositoriesWithOpenPullRequests = repositories
            .Where(static repository => repository.OpenPullRequestsCount.GetValueOrDefault() > 0)
            .OrderBy(static repository => repository.CreatedOn ?? DateTimeOffset.MaxValue)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (repositoriesWithOpenPullRequests.Count == 0)
        {
            return;
        }

        _ = column.Item().Text("Repositories with open pull requests").Bold().FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.RelativeColumn(3f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open PRs");
            });

            for (var i = 0; i < repositoriesWithOpenPullRequests.Count; i++)
            {
                var repository = repositoriesWithOpenPullRequests[i];
                var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var openPrs = repository.OpenPullRequestsCount?.ToString(CultureInfo.InvariantCulture) ?? "-";
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, repository.Slug);
                var pullRequestsUrl = PdfPresentationHelpers.BuildPullRequestsUrl(workspace, repository.Slug);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(repository.Name)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(repository.Name);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdOn);
                _ = string.IsNullOrWhiteSpace(pullRequestsUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openPrs)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(pullRequestsUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(openPrs);
            }
        });
    }

    private static void ComposePullRequestDetailsSection(
        ColumnDescriptor column,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        string workspace,
        int ttfrThresholdHours,
        DateTimeOffset generatedAt)
    {
        if (pullRequestDetails.Count == 0)
        {
            return;
        }

        var ttfrThreshold = TimeSpan.FromHours(ttfrThresholdHours);

        _ = column.Item()
            .Text("Open PR details")
            .Bold()
            .FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn(2.1f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.9f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("PR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Opened on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open for");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("TTFR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("My comments");
            });

            for (var i = 0; i < pullRequestDetails.Count; i++)
            {
                var detail = pullRequestDetails[i];
                var openedOn = detail.OpenedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var openDuration = detail.GetOpenDuration(generatedAt);
                var openFor = FormatDuration(openDuration);
                var ttfr = detail.TimeToFirstResponse;
                var isTtfrPendingOverdue = ttfr is null && openDuration > ttfrThreshold;
                var ttfrText = ttfr is null
                    ? isTtfrPendingOverdue ? "ALERT" : "-"
                    : FormatDuration(ttfr.Value);
                var discussion = detail.HasCurrentUserDiscussion ? "\U0001F4AC Yes" : "-";
                var pullRequestText = $"#{detail.PullRequestId.ToString(CultureInfo.InvariantCulture)} {detail.Title}";
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, detail.RepositorySlug);
                var pullRequestUrl = PdfPresentationHelpers.BuildPullRequestUrl(
                    workspace,
                    detail.RepositorySlug,
                    detail.PullRequestId);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(detail.RepositoryName)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(detail.RepositoryName);

                _ = string.IsNullOrWhiteSpace(pullRequestUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(pullRequestText)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(pullRequestUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(pullRequestText);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openedOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openFor);
                _ = isTtfrPendingOverdue
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Red.Darken2))
                        .Text(ttfrText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(ttfrText);
                _ = detail.HasCurrentUserDiscussion
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2))
                        .Text(discussion)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(discussion);
            }
        });
    }

    private static void ComposeAbandonedRepositoriesSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace,
        int abandonedMonthsThreshold)
    {
        var abandonedRepositories = repositories
            .Where(repository => repository.CanCalculateInactivityTiming
                                 && repository.MonthsWithoutActivity > abandonedMonthsThreshold)
            .OrderByDescending(static repository => repository.MonthsWithoutActivity)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (abandonedRepositories.Count == 0)
        {
            return;
        }

        _ = column.Item()
            .Text($"Abandoned repositories (more than {abandonedMonthsThreshold} months inactive)")
            .Bold()
            .FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.RelativeColumn(3f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Last activity on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Months inactive");
            });

            for (var i = 0; i < abandonedRepositories.Count; i++)
            {
                var repository = abandonedRepositories[i];
                var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var lastActivityOn = repository.LastUpdatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var inactiveMonths = repository.MonthsWithoutActivity.ToString(CultureInfo.InvariantCulture);
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, repository.Slug);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(repository.Name)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(repository.Name);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(lastActivityOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(inactiveMonths);
            }
        });
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
}
