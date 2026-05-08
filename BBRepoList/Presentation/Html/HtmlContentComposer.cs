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
                    reportData.PullRequestDetails[i],
                    i + 1,
                    reportData.GeneratedAt,
                    reportData.TtfrThresholdHours,
                    reportData.MinimalDescriptionTextLength));
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
                    reportData.MergedPullRequests[i],
                    i + 1,
                    reportData.GeneratedAt,
                    reportData.MinimalDescriptionTextLength));
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
        PullRequestDetail detail,
        int index,
        DateTimeOffset generatedAt,
        int ttfrThresholdHours,
        int minimalDescriptionTextLength)
    {
        ArgumentNullException.ThrowIfNull(rowTemplate);
        ArgumentNullException.ThrowIfNull(detail);

        var openFor = detail.GetOpenDuration(generatedAt);
        var ttfr = detail.TimeToFirstResponse;
        var overdueTtfr = ttfr is null && openFor > TimeSpan.FromHours(ttfrThresholdHours);
        var lastActivityAge = detail.GetLastActivityAge(generatedAt);

        return BuildRowHtml(
            rowTemplate,
            workspace,
            detail,
            index,
            openFor,
            lastActivityAge,
            overdueTtfr,
            minimalDescriptionTextLength);
    }

    private static string BuildRowHtml(
        string rowTemplate,
        string workspace,
        MergedPullRequest pullRequest,
        int index,
        DateTimeOffset generatedAt,
        int minimalDescriptionTextLength)
    {
        ArgumentNullException.ThrowIfNull(rowTemplate);
        ArgumentNullException.ThrowIfNull(pullRequest);

        var openFor = pullRequest.GetOpenDuration();
        var mergedAge = TimeSpan.FromTicks(Math.Max((generatedAt - pullRequest.MergedOn).Ticks, 0));

        return BuildRowHtml(
            rowTemplate,
            workspace,
            pullRequest,
            index,
            openFor,
            mergedAge,
            overdueTtfr: false,
            minimalDescriptionTextLength);
    }

    private static string BuildRowHtml(
        string rowTemplate,
        string workspace,
        IPullRequestReportItem pullRequest,
        int index,
        TimeSpan openFor,
        TimeSpan? activityAge,
        bool overdueTtfr,
        int minimalDescriptionTextLength)
    {
        var repositoryUrl = HtmlPresentationHelpers.BuildRepositoryBrowseUrl(workspace, pullRequest.RepositorySlug);
        var pullRequestUrl = HtmlPresentationHelpers.BuildPullRequestUrl(
            workspace,
            pullRequest.RepositorySlug,
            pullRequest.PullRequestId);
        var descriptionLength = pullRequest.DescriptionText?.Length ?? 0;
        var isDescriptionShort = pullRequest.HasShortOrMissingDescription(minimalDescriptionTextLength);
        var ttfr = pullRequest.TimeToFirstResponse;
        var myActivityText = HtmlPresentationHelpers.BuildMyActivityText(pullRequest);
        var requestChangesText = PresentationHelpers.FormatRequestChangesText(pullRequest.RequestChangesCount);
        var approvalsText = PresentationHelpers.FormatApprovalsText(pullRequest.ApprovalsCount);

        return ApplyTemplate(
            rowTemplate,
            new Dictionary<string, string>(28, StringComparer.Ordinal)
            {
                ["__INDEX__"] = index.ToString(CultureInfo.InvariantCulture),
                ["__REPOSITORY_NAME__"] = HtmlPresentationHelpers.Encode(pullRequest.RepositoryName),
                ["__PULL_REQUEST_ID__"] = pullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture),
                ["__TITLE__"] = HtmlPresentationHelpers.Encode(pullRequest.Title),
                ["__AUTHOR_DISPLAY_NAME_SORT__"] = HtmlPresentationHelpers.Encode(pullRequest.AuthorDisplayName ?? "-"),
                ["__AUTHOR_DISPLAY_NAME_DISPLAY__"] = BuildCompactAuthorDisplayName(pullRequest.AuthorDisplayName),
                ["__PULL_REQUEST_LINK__"] = BuildPullRequestLink(pullRequestUrl, pullRequest.PullRequestId, pullRequest.Title),
                ["__REPOSITORY_LINK__"] = BuildLink(repositoryUrl, pullRequest.RepositoryName),
                ["__DESCRIPTION_LENGTH__"] = descriptionLength.ToString(CultureInfo.InvariantCulture),
                ["__DESCRIPTION_SHORT_CLASS__"] = isDescriptionShort ? " class=\"description-short\"" : string.Empty,
                ["__OPEN_FOR_SORT__"] = ((long)openFor.TotalMinutes).ToString(CultureInfo.InvariantCulture),
                ["__OPEN_FOR__"] = HtmlPresentationHelpers.Encode(HtmlPresentationHelpers.FormatDuration(openFor)),
                ["__TTFR_SORT__"] = (ttfr is null ? -1L : (long)ttfr.Value.TotalMinutes).ToString(CultureInfo.InvariantCulture),
                ["__TTFR_FILTER__"] = HtmlPresentationHelpers.Encode(
                    ttfr is null ? (overdueTtfr ? "alert" : "-") : HtmlPresentationHelpers.FormatDuration(ttfr.Value)),
                ["__TTFR__"] = BuildTtfrCell(ttfr, overdueTtfr),
                ["__LAST_ACTIVITY_SORT__"] = (activityAge is null ? -1L : (long)activityAge.Value.TotalMinutes)
                    .ToString(CultureInfo.InvariantCulture),
                ["__LAST_ACTIVITY__"] = HtmlPresentationHelpers.Encode(
                    activityAge is null ? "-" : HtmlPresentationHelpers.FormatDuration(activityAge.Value)),
                ["__COMMENTS_COUNT__"] = pullRequest.CommentsCount.ToString(CultureInfo.InvariantCulture),
                ["__COMMENTS_TEXT__"] = pullRequest.CommentsCount.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_COUNT__"] = pullRequest.RequestChangesCount.ToString(CultureInfo.InvariantCulture),
                ["__REQUEST_CHANGES_TEXT__"] = HtmlPresentationHelpers.Encode(requestChangesText),
                ["__REQUEST_CHANGES_BADGE__"] = BuildBadge(requestChangesText, "rc"),
                ["__APPROVALS_COUNT__"] = pullRequest.ApprovalsCount.ToString(CultureInfo.InvariantCulture),
                ["__APPROVALS_TEXT__"] = HtmlPresentationHelpers.Encode(approvalsText),
                ["__APPROVALS_BADGE__"] = BuildBadge(approvalsText, "ap"),
                ["__MY_ACTIVITY_TEXT__"] = HtmlPresentationHelpers.Encode(myActivityText),
                ["__MY_ACTIVITY_BADGE__"] = BuildActivityBadge(pullRequest)
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

    private static string BuildActivityBadge(IPullRequestReportItem pullRequest)
    {
        if (!pullRequest.HasCurrentUserActivity)
        {
            return "-";
        }

        var parts = new List<string>(3);

        if (pullRequest.HasCurrentUserDiscussion)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Comment\">&#128172;</span>");
        }

        if (pullRequest.HasCurrentUserRequestChanges)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Request changes\">&#10060;</span>");
        }

        if (pullRequest.HasCurrentUserApproval)
        {
            parts.Add("<span class=\"activity-icon\" title=\"Approval\">&#9989;</span>");
        }

        return $"<span class=\"badge activity\">{string.Join(" ", parts)}</span>";
    }
}
