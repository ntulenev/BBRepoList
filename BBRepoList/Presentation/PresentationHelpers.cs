using System.Globalization;

namespace BBRepoList.Presentation;

/// <summary>
/// Presentation formatting helpers.
/// </summary>
public static class PresentationHelpers
{
    /// <summary>
    /// Formats current request changes status for presentation.
    /// </summary>
    /// <param name="requestChangesCount">Active request changes count.</param>
    /// <returns>Summary text or <c>-</c> when there are no active request changes.</returns>
    public static string FormatRequestChangesText(int requestChangesCount)
    {
        if (requestChangesCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestChangesCount),
                "Request changes count cannot be negative.");
        }

        return requestChangesCount == 0
            ? "-"
            : string.Format(CultureInfo.InvariantCulture, "RC ({0})", requestChangesCount);
    }

    /// <summary>
    /// Formats current approval status for presentation.
    /// </summary>
    /// <param name="approvalsCount">Active approvals count.</param>
    /// <returns>Summary text or <c>-</c> when there are no active approvals.</returns>
    public static string FormatApprovalsText(int approvalsCount)
    {
        if (approvalsCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approvalsCount),
                "Approvals count cannot be negative.");
        }

        return approvalsCount == 0
            ? "-"
            : string.Format(CultureInfo.InvariantCulture, "AP ({0})", approvalsCount);
    }
}
