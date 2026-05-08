using System.Globalization;

using BBRepoList.Models;

namespace BBRepoList.Presentation;

/// <summary>
/// Presentation-ready pull request row shared by report renderers.
/// </summary>
internal sealed class PullRequestReportRow
{
    private PullRequestReportRow(
        IPullRequestReportItem pullRequest,
        int descriptionLength,
        bool isDescriptionShort,
        TimeSpan openDuration,
        TimeSpan? activityAge,
        bool isTtfrAlert,
        string? mergedOnText)
    {
        PullRequest = pullRequest;
        RepositoryName = pullRequest.RepositoryName;
        RepositorySlug = pullRequest.RepositorySlug;
        PullRequestId = pullRequest.PullRequestId;
        PullRequestNumberText = "#" + pullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture);
        Title = pullRequest.Title;
        AuthorDisplayName = pullRequest.AuthorDisplayName;
        AuthorDisplayNameLines = PresentationHelpers.SplitCompactDisplayName(pullRequest.AuthorDisplayName);
        DescriptionLength = descriptionLength;
        DescriptionLengthText = descriptionLength.ToString(CultureInfo.InvariantCulture);
        IsDescriptionShort = isDescriptionShort;
        OpenedOnText = pullRequest.OpenedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        OpenDuration = openDuration;
        OpenDurationText = PresentationHelpers.FormatDuration(openDuration);
        TimeToFirstResponse = pullRequest.TimeToFirstResponse;
        TimeToFirstResponseText = pullRequest.TimeToFirstResponse is null
            ? isTtfrAlert ? "ALERT" : "-"
            : PresentationHelpers.FormatDuration(pullRequest.TimeToFirstResponse.Value);
        IsTtfrAlert = isTtfrAlert;
        ActivityAge = activityAge;
        ActivityAgeText = activityAge is null ? "-" : PresentationHelpers.FormatDuration(activityAge.Value);
        MergedOnText = mergedOnText;
        CommentsCount = pullRequest.CommentsCount;
        CommentsCountText = pullRequest.CommentsCount.ToString(CultureInfo.InvariantCulture);
        RequestChangesCount = pullRequest.RequestChangesCount;
        RequestChangesText = PresentationHelpers.FormatRequestChangesText(pullRequest.RequestChangesCount);
        ApprovalsCount = pullRequest.ApprovalsCount;
        ApprovalsText = PresentationHelpers.FormatApprovalsText(pullRequest.ApprovalsCount);
        HasCurrentUserDiscussion = pullRequest.HasCurrentUserDiscussion;
        HasCurrentUserRequestChanges = pullRequest.HasCurrentUserRequestChanges;
        HasCurrentUserApproval = pullRequest.HasCurrentUserApproval;
        HasCurrentUserActivity = pullRequest.HasCurrentUserActivity;
        CurrentUserActivityText = BuildCurrentUserActivityText(pullRequest);
    }

    /// <summary>
    /// Source pull request report item.
    /// </summary>
    public IPullRequestReportItem PullRequest { get; }

    /// <summary>
    /// Repository display name.
    /// </summary>
    public string RepositoryName { get; }

    /// <summary>
    /// Repository slug in workspace scope.
    /// </summary>
    public string? RepositorySlug { get; }

    /// <summary>
    /// Pull request identifier in repository scope.
    /// </summary>
    public int PullRequestId { get; }

    /// <summary>
    /// Formatted pull request number.
    /// </summary>
    public string PullRequestNumberText { get; }

    /// <summary>
    /// Pull request title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Pull request author display name.
    /// </summary>
    public string? AuthorDisplayName { get; }

    /// <summary>
    /// Author display name split into compact display lines.
    /// </summary>
    public string[] AuthorDisplayNameLines { get; }

    /// <summary>
    /// Pull request description length.
    /// </summary>
    public int DescriptionLength { get; }

    /// <summary>
    /// Formatted pull request description length.
    /// </summary>
    public string DescriptionLengthText { get; }

    /// <summary>
    /// Gets a value indicating whether pull request description should be highlighted as short.
    /// </summary>
    public bool IsDescriptionShort { get; }

    /// <summary>
    /// Formatted pull request creation timestamp.
    /// </summary>
    public string OpenedOnText { get; }

    /// <summary>
    /// Pull request open duration.
    /// </summary>
    public TimeSpan OpenDuration { get; }

    /// <summary>
    /// Formatted pull request open duration.
    /// </summary>
    public string OpenDurationText { get; }

    /// <summary>
    /// Time to first non-author response.
    /// </summary>
    public TimeSpan? TimeToFirstResponse { get; }

    /// <summary>
    /// Formatted time to first non-author response.
    /// </summary>
    public string TimeToFirstResponseText { get; }

    /// <summary>
    /// Gets a value indicating whether missing TTFR should be highlighted as overdue.
    /// </summary>
    public bool IsTtfrAlert { get; }

    /// <summary>
    /// Activity age displayed in the activity column.
    /// </summary>
    public TimeSpan? ActivityAge { get; }

    /// <summary>
    /// Formatted activity age displayed in the activity column.
    /// </summary>
    public string ActivityAgeText { get; }

    /// <summary>
    /// Formatted merge timestamp for merged pull requests.
    /// </summary>
    public string? MergedOnText { get; }

    /// <summary>
    /// Pull request comments count.
    /// </summary>
    public int CommentsCount { get; }

    /// <summary>
    /// Formatted pull request comments count.
    /// </summary>
    public string CommentsCountText { get; }

    /// <summary>
    /// Active request changes count.
    /// </summary>
    public int RequestChangesCount { get; }

    /// <summary>
    /// Formatted request changes status.
    /// </summary>
    public string RequestChangesText { get; }

    /// <summary>
    /// Active approvals count.
    /// </summary>
    public int ApprovalsCount { get; }

    /// <summary>
    /// Formatted approvals status.
    /// </summary>
    public string ApprovalsText { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has commented in activity.
    /// </summary>
    public bool HasCurrentUserDiscussion { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently requests changes.
    /// </summary>
    public bool HasCurrentUserRequestChanges { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user currently approves the pull request.
    /// </summary>
    public bool HasCurrentUserApproval { get; }

    /// <summary>
    /// Gets a value indicating whether current authenticated user has any tracked pull request activity.
    /// </summary>
    public bool HasCurrentUserActivity { get; }

    /// <summary>
    /// Formatted current authenticated user activity summary.
    /// </summary>
    public string CurrentUserActivityText { get; }

    /// <summary>
    /// Creates a report row for an open pull request.
    /// </summary>
    /// <param name="detail">Open pull request details.</param>
    /// <param name="generatedAt">Report generation timestamp.</param>
    /// <param name="ttfrThresholdHours">TTFR alert threshold in hours.</param>
    /// <param name="minimalDescriptionTextLength">Minimal pull request description text length.</param>
    /// <returns>Presentation-ready report row.</returns>
    public static PullRequestReportRow FromOpenPullRequest(
        PullRequestDetail detail,
        DateTimeOffset generatedAt,
        int ttfrThresholdHours,
        int minimalDescriptionTextLength)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var openDuration = detail.GetOpenDuration(generatedAt);
        var isTtfrAlert = detail.TimeToFirstResponse is null
                          && openDuration > TimeSpan.FromHours(ttfrThresholdHours);

        return new PullRequestReportRow(
            detail,
            detail.DescriptionText?.Length ?? 0,
            detail.HasShortOrMissingDescription(minimalDescriptionTextLength),
            openDuration,
            detail.GetLastActivityAge(generatedAt),
            isTtfrAlert,
            mergedOnText: null);
    }

    /// <summary>
    /// Creates a report row for a recently merged pull request.
    /// </summary>
    /// <param name="pullRequest">Recently merged pull request.</param>
    /// <param name="generatedAt">Report generation timestamp.</param>
    /// <param name="minimalDescriptionTextLength">Minimal pull request description text length.</param>
    /// <returns>Presentation-ready report row.</returns>
    public static PullRequestReportRow FromMergedPullRequest(
        MergedPullRequest pullRequest,
        DateTimeOffset generatedAt,
        int minimalDescriptionTextLength)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        return new PullRequestReportRow(
            pullRequest,
            pullRequest.DescriptionText?.Length ?? 0,
            pullRequest.HasShortOrMissingDescription(minimalDescriptionTextLength),
            pullRequest.GetOpenDuration(),
            TimeSpan.FromTicks(Math.Max((generatedAt - pullRequest.MergedOn).Ticks, 0)),
            isTtfrAlert: false,
            pullRequest.MergedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
    }

    private static string BuildCurrentUserActivityText(IPullRequestReportItem pullRequest)
    {
        var parts = new List<string>(3);

        if (pullRequest.HasCurrentUserDiscussion)
        {
            parts.Add("comment");
        }

        if (pullRequest.HasCurrentUserRequestChanges)
        {
            parts.Add("request changes");
        }

        if (pullRequest.HasCurrentUserApproval)
        {
            parts.Add("approval");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }
}
