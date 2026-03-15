using System.Globalization;

namespace BBRepoList.Presentation;

/// <summary>
/// Presentation formatting helpers.
/// </summary>
public static class PresentationHelpers
{
    /// <summary>
    /// Splits a person display name into up to two balanced lines for compact rendering.
    /// </summary>
    /// <param name="displayName">Display name to split.</param>
    /// <returns>One or two normalized lines, or <c>-</c> when the input is empty.</returns>
    public static string[] SplitCompactDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return ["-"];
        }

        var parts = displayName.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length <= 1)
        {
            return [parts[0]];
        }

        var splitIndex = FindBestDisplayNameSplitIndex(parts);

        return
        [
            string.Join(" ", parts, 0, splitIndex),
            string.Join(" ", parts, splitIndex, parts.Length - splitIndex)
        ];
    }

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

    private static int FindBestDisplayNameSplitIndex(string[] parts)
    {
        var totalLength = CalculateCombinedLength(parts, 0, parts.Length);
        var bestIndex = 1;
        var bestDifference = int.MaxValue;

        for (var i = 1; i < parts.Length; i++)
        {
            var firstLineLength = CalculateCombinedLength(parts, 0, i);
            var secondLineLength = totalLength - firstLineLength - 1;
            var difference = Math.Abs(firstLineLength - secondLineLength);

            if (difference < bestDifference)
            {
                bestDifference = difference;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static int CalculateCombinedLength(string[] parts, int startIndex, int count)
    {
        var total = 0;

        for (var i = startIndex; i < startIndex + count; i++)
        {
            total += parts[i].Length;
        }

        return total + Math.Max(count - 1, 0);
    }
}
