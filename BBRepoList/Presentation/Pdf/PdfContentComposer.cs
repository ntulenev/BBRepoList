using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BBRepoList.Presentation.Pdf;

/// <summary>
/// Default PDF content composer for repository report.
/// </summary>
public sealed class PdfContentComposer : IPdfContentComposer
{
    /// <inheritdoc />
    public void ComposeContent(ColumnDescriptor column, RepositoryReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        column.Spacing(10);

        ComposeRepositoriesSection(column, reportData.Repositories, reportData.Workspace);
        ComposePullRequestSnapshotsSection(column, reportData.Repositories, reportData.Workspace);
        ComposeMergedPullRequestsSection(
            column,
            reportData.MergedPullRequests,
            reportData.Workspace,
            reportData.MergedPullRequestsDays,
            reportData.MinimalDescriptionTextLength,
            reportData.GeneratedAt);
        ComposePullRequestDetailsSection(
            column,
            reportData.PullRequestDetails,
            reportData.Workspace,
            reportData.TtfrThresholdHours,
            reportData.MinimalDescriptionTextLength,
            reportData.GeneratedAt);
        ComposeAbandonedRepositoriesSection(
            column,
            reportData.Repositories,
            reportData.Workspace,
            reportData.LoadAbandonedRepositoriesStatistics,
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
                var openPrs = repository.OpenPullRequestsCount.ToString(CultureInfo.InvariantCulture);
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

    private static void ComposePullRequestSnapshotsSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace)
    {
        var repositoriesWithPullRequestSnapshots = repositories
            .Where(static repository => repository.OpenPullRequestsCount > 0)
            .OrderBy(static repository => repository.CreatedOn ?? DateTimeOffset.MaxValue)
            .ThenBy(static repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (repositoriesWithPullRequestSnapshots.Count == 0)
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

            for (var i = 0; i < repositoriesWithPullRequestSnapshots.Count; i++)
            {
                var repository = repositoriesWithPullRequestSnapshots[i];
                var createdOn = repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var openPrs = repository.OpenPullRequestsCount.ToString(CultureInfo.InvariantCulture);
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

    private static void ComposeMergedPullRequestsSection(
        ColumnDescriptor column,
        IReadOnlyList<MergedPullRequest> mergedPullRequests,
        string workspace,
        int mergedPullRequestsDays,
        int minimalDescriptionTextLength,
        DateTimeOffset generatedAt)
    {
        if (mergedPullRequests.Count == 0)
        {
            return;
        }

        _ = column.Item()
            .Text($"Recently merged pull requests (last {mergedPullRequestsDays} days)")
            .Bold()
            .FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("PR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("🧑‍💻");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Description len");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Opened on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open for");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("TTFR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Merged");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Merged on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("💬");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("RC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("AP");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Me");
            });

            for (var i = 0; i < mergedPullRequests.Count; i++)
            {
                var row = PullRequestReportRow.FromMergedPullRequest(
                    mergedPullRequests[i],
                    generatedAt,
                    minimalDescriptionTextLength);
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, row.RepositorySlug);
                var pullRequestUrl = PdfPresentationHelpers.BuildPullRequestUrl(
                    workspace,
                    row.RepositorySlug,
                    row.PullRequestId);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.RepositoryName)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(row.RepositoryName);

                ComposePullRequestCell(
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell),
                    pullRequestUrl,
                    row.PullRequestNumberText,
                    row.Title);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(string.Join('\n', row.AuthorDisplayNameLines));
                _ = row.IsDescriptionShort
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Red.Darken2))
                        .Text(row.DescriptionLengthText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.DescriptionLengthText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.OpenedOnText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.OpenDurationText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.TimeToFirstResponseText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.ActivityAgeText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.MergedOnText ?? "-");
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(row.CommentsCountText);
                _ = row.RequestChangesCount > 0
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Orange.Darken2))
                        .Text(row.RequestChangesText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.RequestChangesText);
                _ = row.ApprovalsCount > 0
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Green.Darken2))
                        .Text(row.ApprovalsText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.ApprovalsText);
                ComposeMyActivityCell(table.Cell().Element(PdfPresentationHelpers.StyleBodyCell), row);
            }
        });
    }

    private static void ComposePullRequestDetailsSection(
        ColumnDescriptor column,
        IReadOnlyList<PullRequestDetail> pullRequestDetails,
        string workspace,
        int ttfrThresholdHours,
        int minimalDescriptionTextLength,
        DateTimeOffset generatedAt)
    {
        if (pullRequestDetails.Count == 0)
        {
            return;
        }

        _ = column.Item()
            .Text("Open PR details")
            .Bold()
            .FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("PR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("🧑‍💻");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Description len");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Opened on");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open for");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("TTFR");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Updated");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("💬");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("RC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("AP");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Me");
            });

            for (var i = 0; i < pullRequestDetails.Count; i++)
            {
                var row = PullRequestReportRow.FromOpenPullRequest(
                    pullRequestDetails[i],
                    generatedAt,
                    ttfrThresholdHours,
                    minimalDescriptionTextLength);
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, row.RepositorySlug);
                var pullRequestUrl = PdfPresentationHelpers.BuildPullRequestUrl(
                    workspace,
                    row.RepositorySlug,
                    row.PullRequestId);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = string.IsNullOrWhiteSpace(repositoryUrl)
                    ? table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.RepositoryName)
                    : table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(row.RepositoryName);

                ComposePullRequestCell(
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell),
                    pullRequestUrl,
                    row.PullRequestNumberText,
                    row.Title);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(string.Join('\n', row.AuthorDisplayNameLines));
                _ = row.IsDescriptionShort
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Red.Darken2))
                        .Text(row.DescriptionLengthText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.DescriptionLengthText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.OpenedOnText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.OpenDurationText);
                _ = row.IsTtfrAlert
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Red.Darken2))
                        .Text(row.TimeToFirstResponseText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.TimeToFirstResponseText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.ActivityAgeText);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(row.CommentsCountText);
                _ = row.RequestChangesCount > 0
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Orange.Darken2))
                        .Text(row.RequestChangesText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.RequestChangesText);
                _ = row.ApprovalsCount > 0
                    ? table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Green.Darken2))
                        .Text(row.ApprovalsText)
                    : table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.ApprovalsText);
                ComposeMyActivityCell(table.Cell().Element(PdfPresentationHelpers.StyleBodyCell), row);
            }
        });
    }

    private static void ComposeAbandonedRepositoriesSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace,
        bool loadAbandonedRepositoriesStatistics,
        int abandonedMonthsThreshold)
    {
        if (!loadAbandonedRepositoriesStatistics)
        {
            return;
        }

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

    private static void ComposePullRequestCell(
        IContainer container,
        string? pullRequestUrl,
        string pullRequestNumberText,
        string pullRequestTitle)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(pullRequestNumberText);
        ArgumentNullException.ThrowIfNull(pullRequestTitle);

        var textContainer = string.IsNullOrWhiteSpace(pullRequestUrl)
            ? container
            : container
                .Hyperlink(pullRequestUrl)
                .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline());

        textContainer.Text(text =>
        {
            _ = text.Line(pullRequestNumberText);
            _ = text.Line(pullRequestTitle);
        });
    }

    private static void ComposeMyActivityCell(IContainer container, PullRequestReportRow row)
    {
        if (!row.HasCurrentUserActivity)
        {
            _ = container.Text("-");
            return;
        }

        container.Text(text =>
        {
            var needsSeparator = false;

            if (row.HasCurrentUserDiscussion)
            {
                _ = text.Span("\U0001F4AC").FontColor(Colors.Blue.Darken2);
                needsSeparator = true;
            }

            if (row.HasCurrentUserRequestChanges)
            {
                if (needsSeparator)
                {
                    _ = text.Span(" ");
                }

                _ = text.Span("\u274C").FontColor(Colors.Red.Darken2);
                needsSeparator = true;
            }

            if (row.HasCurrentUserApproval)
            {
                if (needsSeparator)
                {
                    _ = text.Span(" ");
                }

                _ = text.Span("\u2705").FontColor(Colors.Green.Darken2);
            }
        });
    }
}
