using System.Globalization;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.Presentation.Html;

/// <summary>
/// Composes standalone HTML for open pull request analysis.
/// </summary>
public sealed class HtmlContentComposer : IHtmlContentComposer
{
    private const string EMPTY_STATE_ROW_HTML =
        """            <tr><td class="empty" colspan="12">No open pull request details were collected for this run.</td></tr>""";
    private const string EMPTY_MERGED_STATE_ROW_HTML =
        """            <tr><td class="empty" colspan="12">No recently merged pull request details were collected for this run.</td></tr>""";

    /// <inheritdoc />
    public string Compose(RepositoryReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var rows = reportData.PullRequestDetails;
        var now = reportData.GeneratedAt;
        var overdueTtfrCount = rows.Count(detail =>
            detail.TimeToFirstResponse is null
            && detail.GetOpenDuration(now) > TimeSpan.FromHours(reportData.TtfrThresholdHours));
        var commentsTotal = rows.Sum(static detail => detail.CommentsCount);
        var requestChangesTotal = rows.Sum(static detail => detail.RequestChangesCount);
        var approvalsTotal = rows.Sum(static detail => detail.ApprovalsCount);

        var rowsHtml = BuildRowsHtml(HtmlTemplateLoader.LoadPullRequestRowTemplate(), reportData);
        var mergedSectionHtml = BuildMergedSectionHtml(reportData);

        return ApplyTemplate(
            HtmlTemplateLoader.LoadReportTemplate(),
            new Dictionary<string, string>(14, StringComparer.Ordinal)
            {
                ["__WORKSPACE_TITLE__"] = HtmlPresentationHelpers.Encode(reportData.Workspace),
                ["__GENERATED_AT__"] = HtmlPresentationHelpers.Encode(
                    reportData.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                ["__FILTER__"] = HtmlPresentationHelpers.Encode(
                    string.IsNullOrWhiteSpace(reportData.FilterPhrase) ? "(none)" : reportData.FilterPhrase),
                ["__TTFR_THRESHOLD_HOURS__"] = reportData.TtfrThresholdHours.ToString(CultureInfo.InvariantCulture),
                ["__MIN_DESCRIPTION_LENGTH__"] = reportData.MinimalDescriptionTextLength.ToString(CultureInfo.InvariantCulture),
                ["__OPEN_PR_COUNT__"] = rows.Count.ToString(CultureInfo.InvariantCulture),
                ["__TTFR_ALERTS__"] = overdueTtfrCount.ToString(CultureInfo.InvariantCulture),
                ["__COMMENTS_TOTAL__"] = commentsTotal.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_TOTAL__"] = requestChangesTotal.ToString(CultureInfo.InvariantCulture),
                ["__APPROVALS_TOTAL__"] = approvalsTotal.ToString(CultureInfo.InvariantCulture),
                ["__MERGED_PR_COUNT__"] = reportData.MergedPullRequests.Count.ToString(CultureInfo.InvariantCulture),
                ["__ROWS__"] = rowsHtml,
                ["__MERGED_SECTION__"] = mergedSectionHtml
            });
    }

    private static string BuildRowsHtml(string rowTemplate, RepositoryReportData reportData)
    {
        if (reportData.PullRequestDetails.Count == 0)
        {
            return EMPTY_STATE_ROW_HTML;
        }

        var html = new StringBuilder(reportData.PullRequestDetails.Count * 512);

        for (var i = 0; i < reportData.PullRequestDetails.Count; i++)
        {
            _ = html.Append(
                BuildRowHtml(
                    rowTemplate,
                    reportData.Workspace,
                    PullRequestReportRow.FromOpenPullRequest(
                        reportData.PullRequestDetails[i],
                        reportData.GeneratedAt,
                        reportData.TtfrThresholdHours,
                        reportData.MinimalDescriptionTextLength),
                    i + 1));
        }

        return html.ToString();
    }

    private static string BuildMergedRowsHtml(string rowTemplate, RepositoryReportData reportData)
    {
        if (reportData.MergedPullRequests.Count == 0)
        {
            return EMPTY_MERGED_STATE_ROW_HTML;
        }

        var html = new StringBuilder(reportData.MergedPullRequests.Count * 512);

        for (var i = 0; i < reportData.MergedPullRequests.Count; i++)
        {
            _ = html.Append(
                BuildRowHtml(
                    rowTemplate,
                    reportData.Workspace,
                    PullRequestReportRow.FromMergedPullRequest(
                        reportData.MergedPullRequests[i],
                        reportData.GeneratedAt,
                        reportData.MinimalDescriptionTextLength),
                    i + 1));
        }

        return html.ToString();
    }

    private static string BuildMergedSectionHtml(RepositoryReportData reportData)
    {
        if (!reportData.LoadMergedPullRequests)
        {
            return string.Empty;
        }

        var mergedRowsHtml = BuildMergedRowsHtml(HtmlTemplateLoader.LoadPullRequestRowTemplate(), reportData);
        return ApplyTemplate(
            HtmlTemplateLoader.LoadMergedPullRequestSectionTemplate(),
            new Dictionary<string, string>(2, StringComparer.Ordinal)
            {
                ["__MERGED_DAYS__"] = reportData.MergedPullRequestsDays.ToString(CultureInfo.InvariantCulture),
                ["__MERGED_ROWS__"] = mergedRowsHtml
            });
    }

    private static string BuildRowHtml(
        string rowTemplate,
        string workspace,
        PullRequestReportRow row,
        int index)
    {
        ArgumentNullException.ThrowIfNull(rowTemplate);
        ArgumentNullException.ThrowIfNull(row);

        var repositoryUrl = HtmlPresentationHelpers.BuildRepositoryBrowseUrl(workspace, row.RepositorySlug);
        var pullRequestUrl = HtmlPresentationHelpers.BuildPullRequestUrl(
            workspace,
            row.RepositorySlug,
            row.PullRequestId);

        return ApplyTemplate(
            rowTemplate,
            new Dictionary<string, string>(28, StringComparer.Ordinal)
            {
                ["__INDEX__"] = index.ToString(CultureInfo.InvariantCulture),
                ["__REPOSITORY_NAME__"] = HtmlPresentationHelpers.Encode(row.RepositoryName),
                ["__PULL_REQUEST_ID__"] = row.PullRequestId.ToString(CultureInfo.InvariantCulture),
                ["__TITLE__"] = HtmlPresentationHelpers.Encode(row.Title),
                ["__AUTHOR_DISPLAY_NAME_SORT__"] = HtmlPresentationHelpers.Encode(row.AuthorDisplayName ?? "-"),
                ["__AUTHOR_DISPLAY_NAME_DISPLAY__"] = BuildCompactAuthorDisplayName(row),
                ["__PULL_REQUEST_LINK__"] = BuildPullRequestLink(pullRequestUrl, row.PullRequestId, row.Title),
                ["__REPOSITORY_LINK__"] = BuildLink(repositoryUrl, row.RepositoryName),
                ["__DESCRIPTION_LENGTH__"] = row.DescriptionLengthText,
                ["__DESCRIPTION_SHORT_CLASS__"] = row.IsDescriptionShort ? " class=\"description-short\"" : string.Empty,
                ["__OPEN_FOR_SORT__"] = ((long)row.OpenDuration.TotalMinutes).ToString(CultureInfo.InvariantCulture),
                ["__OPEN_FOR__"] = HtmlPresentationHelpers.Encode(row.OpenDurationText),
                ["__TTFR_SORT__"] = (row.TimeToFirstResponse is null ? -1L : (long)row.TimeToFirstResponse.Value.TotalMinutes)
                    .ToString(CultureInfo.InvariantCulture),
                ["__TTFR_FILTER__"] = HtmlPresentationHelpers.Encode(
                    row.TimeToFirstResponse is null ? (row.IsTtfrAlert ? "alert" : "-") : row.TimeToFirstResponseText),
                ["__TTFR__"] = BuildTtfrCell(row),
                ["__LAST_ACTIVITY_SORT__"] = (row.ActivityAge is null ? -1L : (long)row.ActivityAge.Value.TotalMinutes)
                    .ToString(CultureInfo.InvariantCulture),
                ["__LAST_ACTIVITY__"] = HtmlPresentationHelpers.Encode(row.ActivityAgeText),
                ["__COMMENTS_COUNT__"] = row.CommentsCountText,
                ["__COMMENTS_TEXT__"] = row.CommentsCountText,
                ["__REQUEST_CHANGES_COUNT__"] = row.RequestChangesCount.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_TEXT__"] = HtmlPresentationHelpers.Encode(row.RequestChangesText),
                ["__REQUEST_CHANGES_BADGE__"] = BuildBadge(row.RequestChangesText, "rc"),
                ["__APPROVALS_COUNT__"] = row.ApprovalsCount.ToString(CultureInfo.InvariantCulture),
                ["__APPROVALS_TEXT__"] = HtmlPresentationHelpers.Encode(row.ApprovalsText),
                ["__APPROVALS_BADGE__"] = BuildBadge(row.ApprovalsText, "ap"),
                ["__MY_ACTIVITY_TEXT__"] = HtmlPresentationHelpers.Encode(row.CurrentUserActivityText),
                ["__MY_ACTIVITY_BADGE__"] = BuildActivityBadge(row)
            });
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(tokens);

        var result = template;

        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string BuildLink(string? url, string text)
    {
        var encodedText = HtmlPresentationHelpers.Encode(text);
        return string.IsNullOrWhiteSpace(url)
            ? encodedText
            : $"<a href=\"{HtmlPresentationHelpers.Encode(url)}\" target=\"_blank\" rel=\"noreferrer\">{encodedText}</a>";
    }

    private static string BuildPullRequestLink(string? url, int pullRequestId, string title)
    {
        var encodedNumber = HtmlPresentationHelpers.Encode("#" + pullRequestId.ToString(CultureInfo.InvariantCulture));
        var encodedTitle = HtmlPresentationHelpers.Encode(title);
        var content = $"<span class=\"pr-link\"><span class=\"pr-number\">{encodedNumber}</span><span>{encodedTitle}</span></span>";

        return string.IsNullOrWhiteSpace(url)
            ? content
            : $"<a href=\"{HtmlPresentationHelpers.Encode(url)}\" class=\"pr-link\" target=\"_blank\" rel=\"noreferrer\"><span class=\"pr-number\">{encodedNumber}</span><span>{encodedTitle}</span></a>";
    }

    private static string BuildTtfrCell(PullRequestReportRow row)
    {
        if (row.TimeToFirstResponse is null)
        {
            return row.IsTtfrAlert
                ? "<span class=\"ttfr-alert\">ALERT</span>"
                : "-";
        }

        return HtmlPresentationHelpers.Encode(row.TimeToFirstResponseText);
    }

    private static string BuildBadge(string text, string cssClass)
    {
        var encodedText = HtmlPresentationHelpers.Encode(text);
        return text == "-"
            ? encodedText
            : $"<span class=\"badge {cssClass}\">{encodedText}</span>";
    }

    private static string BuildCompactAuthorDisplayName(PullRequestReportRow row) =>
        string.Join("<br />", row.AuthorDisplayNameLines.Select(HtmlPresentationHelpers.Encode));

    private static string BuildActivityBadge(PullRequestReportRow row)
    {
        if (!row.HasCurrentUserActivity)
        {
            return "-";
        }

        var parts = new List<string>(3);

        if (row.HasCurrentUserDiscussion)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Comment\">&#128172;</span>");
        }

        if (row.HasCurrentUserRequestChanges)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Request changes\">&#10060;</span>");
        }

        if (row.HasCurrentUserApproval)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Approval\">&#9989;</span>");
        }

        return $"<span class=\"badge activity\">{string.Join(" ", parts)}</span>";
    }
}
