using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client implementation for pull request operations.
/// </summary>
public sealed class BitbucketPRApiClient : IBitbucketPRApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPRApiClient"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    /// <param name="jsonParser">Bitbucket JSON parser.</param>
    /// <param name="cache">Pull request details cache.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(
        IBitbucketTransport transport,
        IBitbucketJsonParser jsonParser,
        IPullRequestDetailsCache cache,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(jsonParser);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _jsonParser = jsonParser;
        _cache = cache;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task PopulateOpenPullRequestCountAsync(Repository repository, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!repository.CanPopulateOpenPullRequestsCount)
        {
            return;
        }

        var repositorySlug = repository.Slug!;

        try
        {
            var escapedSlug = Uri.EscapeDataString(repositorySlug);
            var url = new Uri(
                $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests?state=OPEN&pagelen=1&fields=size",
                UriKind.Relative);

            var summary = await _transport.GetAsync<PullRequestPageSummaryDto>(url, cancellationToken).ConfigureAwait(false);
            repository.UpdateOpenPullRequestsCount(summary?.Size ?? 0);
        }
        catch (HttpRequestException)
        {
            return;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestDetail>> GetOpenPullRequestDetailsAsync(
        Repository repository,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!repository.CanLoadOpenPullRequestDetails)
        {
            return [];
        }

        var repositorySlug = repository.Slug!;
        var details = new List<PullRequestDetail>();

        try
        {
            var cacheEntries = await _cache
                .ReadEntriesAsync(_options.Workspace, repositorySlug, currentUserId, cancellationToken)
                .ConfigureAwait(false);
            var cacheEntriesByPullRequestId = cacheEntries
                .GroupBy(static entry => entry.PullRequestId)
                .ToDictionary(static group => group.Key, static group => group.Last());

            var openPullRequests = await GetOpenPullRequestsAsync(
                repositorySlug,
                currentUserId,
                cancellationToken).ConfigureAwait(false);
            repository.UpdateOpenPullRequestsCount(openPullRequests.Count);

            if (openPullRequests.Count == 0)
            {
                await _cache
                    .DeleteAsync(_options.Workspace, repositorySlug, currentUserId, cancellationToken)
                    .ConfigureAwait(false);
                return [];
            }

            var updatedCacheEntries = new List<PullRequestDetailsCacheEntry>(openPullRequests.Count);

            foreach (var pullRequest in openPullRequests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (TryCreateDetailFromCache(
                    repository,
                    pullRequest,
                    cacheEntriesByPullRequestId,
                    out var cachedDetail,
                    out var cacheEntry))
                {
                    details.Add(cachedDetail);
                    updatedCacheEntries.Add(cacheEntry);
                    continue;
                }

                var activities = await GetPullRequestActivitiesAsync(
                    repositorySlug,
                    pullRequest.Id,
                    cancellationToken).ConfigureAwait(false);
                var activitySummary = CreateActivitySummary(activities, pullRequest, currentUserId);

                details.Add(CreatePullRequestDetail(repository, pullRequest, activitySummary));
                updatedCacheEntries.Add(new PullRequestDetailsCacheEntry(
                    pullRequest.Id,
                    pullRequest.CacheFingerprint ?? string.Empty,
                    activitySummary.FirstNonAuthorActivityOn,
                    activitySummary.LastActivityOn,
                    activitySummary.HasCurrentUserDiscussion,
                    activitySummary.CommentsCount));
            }

            await _cache
                .SaveEntriesAsync(_options.Workspace, repositorySlug, currentUserId, updatedCacheEntries, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return details;
    }

    private async Task<IReadOnlyList<OpenPullRequest>> GetOpenPullRequestsAsync(
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(repositorySlug);
        var fields = Uri.EscapeDataString(
            "values.id," +
            "values.title," +
            "values.created_on," +
            "values.updated_on," +
            "values.state," +
            "values.description," +
            "values.summary.raw," +
            "values.author.uuid," +
            "values.author.display_name," +
            "values.source.commit.hash," +
            "values.comment_count," +
            "values.task_count," +
            "values.participants.user.uuid," +
            "values.participants.state," +
            "values.participants.approved," +
            "next");
        var url = new Uri(
            $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests?state=OPEN&pagelen={_options.PageLen}&fields={fields}",
            UriKind.Relative);

        var pullRequests = new List<OpenPullRequest>();

        while (url is not null)
        {
            var page = await _transport.GetAsync<PullRequestPageDto>(url, cancellationToken).ConfigureAwait(false);
            if (page is null)
            {
                break;
            }

            foreach (var pullRequestDto in page.Values ?? [])
            {
                if (pullRequestDto.Id is null || pullRequestDto.Id <= 0 || pullRequestDto.CreatedOn is null)
                {
                    continue;
                }

                var authorId = BitbucketId.TryCreate(pullRequestDto.Author?.Uuid, out var parsedAuthorId)
                    ? parsedAuthorId
                    : (BitbucketId?)null;
                var descriptionText = string.IsNullOrWhiteSpace(pullRequestDto.Description)
                    ? pullRequestDto.Summary?.Raw
                    : pullRequestDto.Description;
                var (requestChangesCount, hasCurrentUserRequestChanges, approvalsCount, hasCurrentUserApproval) =
                    GetPullRequestReviewState(pullRequestDto.Participants, currentUserId);

                pullRequests.Add(new OpenPullRequest(
                    pullRequestDto.Id.Value,
                    string.IsNullOrWhiteSpace(pullRequestDto.Title)
                        ? $"PR-{pullRequestDto.Id.Value.ToString(CultureInfo.InvariantCulture)}"
                        : pullRequestDto.Title.Trim(),
                    pullRequestDto.CreatedOn.Value,
                    descriptionText,
                    authorId,
                    pullRequestDto.Author?.DisplayName,
                    requestChangesCount,
                    hasCurrentUserRequestChanges,
                    approvalsCount,
                    hasCurrentUserApproval,
                    BuildPullRequestFingerprint(pullRequestDto)));
            }

            url = page.Next;
        }

        return pullRequests;
    }

    private async Task<IReadOnlyList<PullRequestActivityEntry>> GetPullRequestActivitiesAsync(
        string repositorySlug,
        int pullRequestId,
        CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(repositorySlug);
        var fields = Uri.EscapeDataString(
            "values.actor.uuid," +
            "values.user.uuid," +
            "values.date," +
            "values.created_on," +
            "values.updated_on," +
            "values.comment," +
            "values.approval," +
            "values.request_changes," +
            "values.changes_requested," +
            "values.update," +
            "next");
        var url = new Uri(
            $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests/{pullRequestId}/activity?pagelen={_options.PageLen}&fields={fields}",
            UriKind.Relative);

        var activities = new List<PullRequestActivityEntry>();

        while (url is not null)
        {
            var page = await _transport.GetAsync<PullRequestActivityPageDto>(url, cancellationToken).ConfigureAwait(false);
            if (page is null)
            {
                break;
            }

            foreach (var activity in page.Values ?? [])
            {
                if (activity.Properties is null)
                {
                    continue;
                }

                foreach (var property in activity.Properties)
                {
                    _jsonParser.AddActivityEntriesFromJson(
                        property.Value,
                        isCommentContext: property.Key.Equals("comment", StringComparison.OrdinalIgnoreCase),
                        (actorId, happenedOn, isComment) =>
                            activities.Add(new PullRequestActivityEntry(actorId, happenedOn, isComment)));
                }
            }

            url = page.Next;
        }

        return [.. activities.DistinctBy(static activity => (activity.ActorId, activity.HappenedOn, activity.IsComment))];
    }

    private (int RequestChangesCount, bool HasCurrentUserRequestChanges, int ApprovalsCount, bool HasCurrentUserApproval)
        GetPullRequestReviewState(
        ICollection<PullRequestParticipantDto>? participants,
        BitbucketId currentUserId)
    {
        if (participants is null || participants.Count == 0)
        {
            return default;
        }

        var requestChangesCount = 0;
        var hasCurrentUserRequestChanges = false;
        var approvalsCount = 0;
        var hasCurrentUserApproval = false;

        foreach (var participant in participants)
        {
            if (!BitbucketId.TryCreate(participant.User?.Uuid, out var participantId))
            {
                continue;
            }

            if (_jsonParser.IsRequestChangesState(participant.State))
            {
                requestChangesCount++;
                hasCurrentUserRequestChanges |= participantId == currentUserId;
            }

            if (_jsonParser.IsApprovalState(participant))
            {
                approvalsCount++;
                hasCurrentUserApproval |= participantId == currentUserId;
            }
        }

        return (requestChangesCount, hasCurrentUserRequestChanges, approvalsCount, hasCurrentUserApproval);
    }

    private static PullRequestActivitySummary CreateActivitySummary(
        IReadOnlyList<PullRequestActivityEntry> activities,
        OpenPullRequest pullRequest,
        BitbucketId currentUserId)
    {
        var firstNonAuthorActivityOn = activities
            .Where(activity => pullRequest.AuthorId is null
                               || activity.ActorId != pullRequest.AuthorId.Value)
            .OrderBy(static activity => activity.HappenedOn)
            .Select(static activity => (DateTimeOffset?)activity.HappenedOn)
            .FirstOrDefault();
        var lastActivityOn = activities
            .OrderByDescending(static activity => activity.HappenedOn)
            .Select(static activity => (DateTimeOffset?)activity.HappenedOn)
            .FirstOrDefault();
        var hasCurrentUserDiscussion = activities.Any(activity =>
            activity.IsComment
            && activity.ActorId == currentUserId);
        var commentsCount = activities.Count(static activity => activity.IsComment);

        return new PullRequestActivitySummary(
            firstNonAuthorActivityOn,
            lastActivityOn,
            hasCurrentUserDiscussion,
            commentsCount);
    }

    private static PullRequestDetail CreatePullRequestDetail(
        Repository repository,
        OpenPullRequest pullRequest,
        PullRequestActivitySummary activitySummary) =>
        new(
            repository,
            pullRequest.Id,
            pullRequest.Title,
            pullRequest.CreatedOn,
            pullRequest.AuthorId,
            pullRequest.AuthorDisplayName,
            activitySummary.FirstNonAuthorActivityOn,
            activitySummary.LastActivityOn,
            activitySummary.HasCurrentUserDiscussion,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);

    private static bool TryCreateDetailFromCache(
        Repository repository,
        OpenPullRequest pullRequest,
        Dictionary<int, PullRequestDetailsCacheEntry> cacheEntriesByPullRequestId,
        out PullRequestDetail detail,
        out PullRequestDetailsCacheEntry cacheEntry)
    {
        detail = null!;
        cacheEntry = null!;

        if (string.IsNullOrWhiteSpace(pullRequest.CacheFingerprint)
            || !cacheEntriesByPullRequestId.TryGetValue(pullRequest.Id, out var existingEntry)
            || !string.Equals(existingEntry.Fingerprint, pullRequest.CacheFingerprint, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var activitySummary = new PullRequestActivitySummary(
                existingEntry.FirstNonAuthorActivityOn,
                existingEntry.LastActivityOn,
                existingEntry.HasCurrentUserDiscussion,
                existingEntry.CommentsCount);
            detail = CreatePullRequestDetail(repository, pullRequest, activitySummary);
            cacheEntry = existingEntry;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string BuildPullRequestFingerprint(PullRequestDto pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        var participantReviewState = string.Join(
            ';',
            (pullRequest.Participants ?? [])
                .Select(static participant => string.Join(
                    '|',
                    participant.User?.Uuid?.Trim() ?? string.Empty,
                    participant.State?.Trim() ?? string.Empty,
                    participant.Approved == true ? "1" : "0"))
                .OrderBy(static value => value, StringComparer.Ordinal));

        var rawFingerprint = string.Join(
            '\n',
            (pullRequest.Id ?? 0).ToString(CultureInfo.InvariantCulture),
            pullRequest.UpdatedOn?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            pullRequest.State?.Trim() ?? string.Empty,
            pullRequest.Source?.Commit?.Hash?.Trim() ?? string.Empty,
            pullRequest.CommentCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            pullRequest.TaskCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            participantReviewState);

        var fingerprintBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprint));
        return Convert.ToHexString(fingerprintBytes);
    }

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketJsonParser _jsonParser;
    private readonly IPullRequestDetailsCache _cache;
    private readonly BitbucketOptions _options;

    private sealed record PullRequestActivitySummary(
        DateTimeOffset? FirstNonAuthorActivityOn,
        DateTimeOffset? LastActivityOn,
        bool HasCurrentUserDiscussion,
        int CommentsCount);
}
