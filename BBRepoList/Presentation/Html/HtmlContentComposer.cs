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

    /// <inheritdoc />
    public string Compose(RepositoryPdfReportData reportData)
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

        return ApplyTemplate(
            HtmlTemplateLoader.LoadReportTemplate(),
            new Dictionary<string, string>(11, StringComparer.Ordinal)
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
                ["__ROWS__"] = rowsHtml
            });
    }

    private static string BuildRowsHtml(string rowTemplate, RepositoryPdfReportData reportData)
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
                    reportData.PullRequestDetails[i],
                    i + 1,
                    reportData.GeneratedAt,
                    reportData.TtfrThresholdHours,
                    reportData.MinimalDescriptionTextLength));
        }

        return html.ToString();
    }

    private static string BuildRowHtml(
        string rowTemplate,
        string workspace,
        PullRequestDetail detail,
        int index,
        DateTimeOffset generatedAt,
        int ttfrThresholdHours,
        int minimalDescriptionTextLength)
    {
        ArgumentNullException.ThrowIfNull(rowTemplate);
        ArgumentNullException.ThrowIfNull(detail);

        var repositoryUrl = HtmlPresentationHelpers.BuildRepositoryBrowseUrl(workspace, detail.RepositorySlug);
        var pullRequestUrl = HtmlPresentationHelpers.BuildPullRequestUrl(workspace, detail.RepositorySlug, detail.PullRequestId);
        var descriptionLength = detail.DescriptionText?.Length ?? 0;
        var isDescriptionShort = detail.HasShortOrMissingDescription(minimalDescriptionTextLength);
        var openFor = detail.GetOpenDuration(generatedAt);
        var ttfr = detail.TimeToFirstResponse;
        var overdueTtfr = ttfr is null && openFor > TimeSpan.FromHours(ttfrThresholdHours);
        var lastActivityAge = detail.GetLastActivityAge(generatedAt);
        var myActivityText = HtmlPresentationHelpers.BuildMyActivityText(detail);
        var requestChangesText = PresentationHelpers.FormatRequestChangesText(detail.RequestChangesCount);
        var approvalsText = PresentationHelpers.FormatApprovalsText(detail.ApprovalsCount);

        return ApplyTemplate(
            rowTemplate,
            new Dictionary<string, string>(28, StringComparer.Ordinal)
            {
                ["__INDEX__"] = index.ToString(CultureInfo.InvariantCulture),
                ["__REPOSITORY_NAME__"] = HtmlPresentationHelpers.Encode(detail.RepositoryName),
                ["__PULL_REQUEST_ID__"] = detail.PullRequestId.ToString(CultureInfo.InvariantCulture),
                ["__TITLE__"] = HtmlPresentationHelpers.Encode(detail.Title),
                ["__AUTHOR_DISPLAY_NAME_SORT__"] = HtmlPresentationHelpers.Encode(detail.AuthorDisplayName ?? "-"),
                ["__AUTHOR_DISPLAY_NAME_DISPLAY__"] = BuildCompactAuthorDisplayName(detail.AuthorDisplayName),
                ["__PULL_REQUEST_LINK__"] = BuildPullRequestLink(pullRequestUrl, detail.PullRequestId, detail.Title),
                ["__REPOSITORY_LINK__"] = BuildLink(repositoryUrl, detail.RepositoryName),
                ["__DESCRIPTION_LENGTH__"] = descriptionLength.ToString(CultureInfo.InvariantCulture),
                ["__DESCRIPTION_SHORT_CLASS__"] = isDescriptionShort ? " class=\"description-short\"" : string.Empty,
                ["__OPEN_FOR_SORT__"] = ((long)openFor.TotalMinutes).ToString(CultureInfo.InvariantCulture),
                ["__OPEN_FOR__"] = HtmlPresentationHelpers.Encode(HtmlPresentationHelpers.FormatDuration(openFor)),
                ["__TTFR_SORT__"] = (ttfr is null ? -1L : (long)ttfr.Value.TotalMinutes).ToString(CultureInfo.InvariantCulture),
                ["__TTFR_FILTER__"] = HtmlPresentationHelpers.Encode(
                    ttfr is null ? (overdueTtfr ? "alert" : "-") : HtmlPresentationHelpers.FormatDuration(ttfr.Value)),
                ["__TTFR__"] = BuildTtfrCell(ttfr, overdueTtfr),
                ["__LAST_ACTIVITY_SORT__"] = (lastActivityAge is null ? -1L : (long)lastActivityAge.Value.TotalMinutes)
                    .ToString(CultureInfo.InvariantCulture),
                ["__LAST_ACTIVITY__"] = HtmlPresentationHelpers.Encode(
                    lastActivityAge is null ? "-" : HtmlPresentationHelpers.FormatDuration(lastActivityAge.Value)),
                ["__COMMENTS_COUNT__"] = detail.CommentsCount.ToString(CultureInfo.InvariantCulture),
                ["__COMMENTS_TEXT__"] = detail.CommentsCount.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_COUNT__"] = detail.RequestChangesCount.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_TEXT__"] = HtmlPresentationHelpers.Encode(requestChangesText),
                ["__REQUEST_CHANGES_BADGE__"] = BuildBadge(requestChangesText, "rc"),
                ["__APPROVALS_COUNT__"] = detail.ApprovalsCount.ToString(CultureInfo.InvariantCulture),
                ["__APPROVALS_TEXT__"] = HtmlPresentationHelpers.Encode(approvalsText),
                ["__APPROVALS_BADGE__"] = BuildBadge(approvalsText, "ap"),
                ["__MY_ACTIVITY_TEXT__"] = HtmlPresentationHelpers.Encode(myActivityText),
                ["__MY_ACTIVITY_BADGE__"] = BuildActivityBadge(detail)
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

    private static string BuildTtfrCell(TimeSpan? ttfr, bool overdueTtfr)
    {
        if (ttfr is null)
        {
            return overdueTtfr
                ? "<span class=\"ttfr-alert\">ALERT</span>"
                : "-";
        }

        return HtmlPresentationHelpers.Encode(HtmlPresentationHelpers.FormatDuration(ttfr.Value));
    }

    private static string BuildBadge(string text, string cssClass)
    {
        var encodedText = HtmlPresentationHelpers.Encode(text);
        return text == "-"
            ? encodedText
            : $"<span class=\"badge {cssClass}\">{encodedText}</span>";
    }

    private static string BuildCompactAuthorDisplayName(string? authorDisplayName)
    {
        var lines = PresentationHelpers.SplitCompactDisplayName(authorDisplayName);
        return string.Join("<br />", lines.Select(HtmlPresentationHelpers.Encode));
    }

    private static string BuildActivityBadge(PullRequestDetail detail)
    {
        if (!detail.HasCurrentUserActivity)
        {
            return "-";
        }

        var parts = new List<string>(3);

        if (detail.HasCurrentUserDiscussion)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Comment\">&#128172;</span>");
        }

        if (detail.HasCurrentUserRequestChanges)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Request changes\">&#10060;</span>");
        }

        if (detail.HasCurrentUserApproval)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Approval\">&#9989;</span>");
        }

        return $"<span class=\"badge activity\">{string.Join(" ", parts)}</span>";
    }
}
