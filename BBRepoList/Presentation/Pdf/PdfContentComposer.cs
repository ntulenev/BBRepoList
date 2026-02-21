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
        ComposeAbandonedRepositoriesSection(
            column,
            reportData.Repositories,
            reportData.Workspace,
            reportData.AbandonedMonthsThreshold,
            reportData.GeneratedAt);
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
                if (string.IsNullOrWhiteSpace(repositoryUrl))
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(repository.Name);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(repository.Name);
                }

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(updatedOn);
                if (repository.OpenPullRequestsCount is null || string.IsNullOrWhiteSpace(pullRequestsUrl))
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openPrs);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(pullRequestsUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(openPrs);
                }
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
            .OrderByDescending(static repository => repository.OpenPullRequestsCount)
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
                columns.RelativeColumn(1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Repository");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Open PRs");
            });

            for (var i = 0; i < repositoriesWithOpenPullRequests.Count; i++)
            {
                var repository = repositoriesWithOpenPullRequests[i];
                var openPrs = repository.OpenPullRequestsCount?.ToString(CultureInfo.InvariantCulture) ?? "-";
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, repository.Slug);
                var pullRequestsUrl = PdfPresentationHelpers.BuildPullRequestsUrl(workspace, repository.Slug);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                if (string.IsNullOrWhiteSpace(repositoryUrl))
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(repository.Name);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(repository.Name);
                }

                if (string.IsNullOrWhiteSpace(pullRequestsUrl))
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openPrs);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(pullRequestsUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(openPrs);
                }
            }
        });
    }

    private static void ComposeAbandonedRepositoriesSection(
        ColumnDescriptor column,
        IReadOnlyList<Repository> repositories,
        string workspace,
        int abandonedMonthsThreshold,
        DateTimeOffset generatedAt)
    {
        var abandonedRepositories = repositories
            .Select(repository =>
            {
                var lastActivityOn = repository.LastUpdatedOn ?? repository.CreatedOn;
                if (lastActivityOn is null)
                {
                    return null;
                }

                var monthsWithoutActivity = PdfPresentationHelpers.CalculateFullMonthsBetween(lastActivityOn.Value, generatedAt);
                return new AbandonedRepositoryRow(repository, lastActivityOn.Value, monthsWithoutActivity);
            })
            .Where(row => row is not null && row.MonthsWithoutActivity > abandonedMonthsThreshold)
            .Select(static row => row!)
            .OrderByDescending(static row => row.MonthsWithoutActivity)
            .ThenBy(static row => row.Repository.Name, StringComparer.OrdinalIgnoreCase)
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
                var row = abandonedRepositories[i];
                var createdOn = row.Repository.CreatedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
                var lastActivityOn = row.LastActivityOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var inactiveMonths = row.MonthsWithoutActivity.ToString(CultureInfo.InvariantCulture);
                var repositoryUrl = PdfPresentationHelpers.BuildRepositoryBrowseUrl(workspace, row.Repository.Slug);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                if (string.IsNullOrWhiteSpace(repositoryUrl))
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(row.Repository.Name);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(repositoryUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(row.Repository.Name);
                }

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(lastActivityOn);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(inactiveMonths);
            }
        });
    }
}
