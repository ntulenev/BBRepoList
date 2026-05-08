using System.Globalization;
using System.Net;

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
        => PresentationHelpers.FormatDuration(duration);

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

}
