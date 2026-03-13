using System.Globalization;
using System.Net;

using BBRepoList.Models;

namespace BBRepoList.Presentation.Html;

/// <summary>
/// Helper methods for HTML report rendering.
/// </summary>
internal static class HtmlPresentationHelpers
{
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public static string FormatDate(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static string FormatDate(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "-";

    public static string FormatDuration(TimeSpan duration)
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

    public static string? BuildRepositoryBrowseUrl(string workspace, string? repositorySlug)
    {
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return null;
        }

        var encodedWorkspace = Uri.EscapeDataString(workspace.Trim());
        var encodedSlug = Uri.EscapeDataString(repositorySlug.Trim());

        return $"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}";
    }

    public static string? BuildPullRequestUrl(string workspace, string? repositorySlug, int pullRequestId)
    {
        if (pullRequestId <= 0 || string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return null;
        }

        var encodedWorkspace = Uri.EscapeDataString(workspace.Trim());
        var encodedSlug = Uri.EscapeDataString(repositorySlug.Trim());

        return $"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}/pull-requests/{pullRequestId}";
    }

    public static string BuildMyActivityText(PullRequestDetail detail)
    {
        var parts = new List<string>(3);

        if (detail.HasCurrentUserDiscussion)
        {
            parts.Add("comment");
        }

        if (detail.HasCurrentUserRequestChanges)
        {
            parts.Add("request changes");
        }

        if (detail.HasCurrentUserApproval)
        {
            parts.Add("approval");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }
}
