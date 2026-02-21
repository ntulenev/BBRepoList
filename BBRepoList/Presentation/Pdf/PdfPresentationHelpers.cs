using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BBRepoList.Presentation.Pdf;

/// <summary>
/// Helper styles and formatting for PDF rendering.
/// </summary>
internal static class PdfPresentationHelpers
{
    private const string HEADER_BACKGROUND_COLOR_HEX = "#1f2937";
    private const string HEADER_TEXT_COLOR_HEX = "#f9fafb";

    public static IContainer StyleHeaderCell(IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return container
            .Border(1)
            .Background(HEADER_BACKGROUND_COLOR_HEX)
            .PaddingHorizontal(6)
            .PaddingVertical(4)
            .DefaultTextStyle(static style => style.FontSize(9).FontColor(HEADER_TEXT_COLOR_HEX).SemiBold());
    }

    public static IContainer StyleBodyCell(IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return container
            .Border(1)
            .PaddingHorizontal(6)
            .PaddingVertical(4)
            .DefaultTextStyle(static style => style.FontSize(8));
    }

    public static int CalculateFullMonthsBetween(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from)
        {
            return 0;
        }

        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
        if (to.Day < from.Day)
        {
            months--;
        }

        return Math.Max(months, 0);
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

    public static string? BuildPullRequestsUrl(string workspace, string? repositorySlug)
    {
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return null;
        }

        var encodedWorkspace = Uri.EscapeDataString(workspace.Trim());
        var encodedSlug = Uri.EscapeDataString(repositorySlug.Trim());

        return $"https://bitbucket.org/{encodedWorkspace}/{encodedSlug}/pull-requests/";
    }
}
