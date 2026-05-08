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
    /// <param name="activityAnalyzer">Pull request activity analyzer.</param>
    /// <param name="snapshotMapper">Pull request snapshot mapper.</param>
    /// <param name="cache">Pull request details cache.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(
        IBitbucketTransport transport,
        IBitbucketJsonParser jsonParser,
        IPullRequestActivityAnalyzer activityAnalyzer,
        IPullRequestSnapshotMapper snapshotMapper,
        IPullRequestDetailsCache cache,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(jsonParser);
        ArgumentNullException.ThrowIfNull(activityAnalyzer);
        ArgumentNullException.ThrowIfNull(snapshotMapper);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _jsonParser = jsonParser;
        _activityAnalyzer = activityAnalyzer;
        _snapshotMapper = snapshotMapper;
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

        if (!repository.CanLoadPullRequests)
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

            var openPullRequests = await GetPullRequestSnapshotsAsync(
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
                var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);

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

    /// <inheritdoc />
    public async Task<IReadOnlyList<MergedPullRequest>> GetMergedPullRequestsAsync(
        Repository repository,
        DateTimeOffset mergedSince,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!repository.CanLoadPullRequests)
        {
            return [];
        }

        var repositorySlug = repository.Slug!;
        var pullRequests = new List<MergedPullRequest>();

        try
        {
            var escapedSlug = Uri.EscapeDataString(repositorySlug);
            var fields = Uri.EscapeDataString(
                "values.id," +
                "values.title," +
                "values.created_on," +
                "values.updated_on," +
                "values.description," +
                "values.summary.raw," +
                "values.author.uuid," +
                "values.author.display_name," +
                "values.participants.user.uuid," +
                "values.participants.state," +
                "values.participants.approved," +
                "next");
            var url = new Uri(
                $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests?state=MERGED&pagelen={_options.PageLen}&sort=-updated_on&fields={fields}",
                UriKind.Relative);

            while (url is not null)
            {
                var page = await _transport.GetAsync<PullRequestPageDto>(url, cancellationToken).ConfigureAwait(false);
                if (page is null)
                {
                    break;
                }

                foreach (var pullRequestDto in page.Values ?? [])
                {
                    if (pullRequestDto.Id is null
                        || pullRequestDto.Id <= 0
                        || pullRequestDto.CreatedOn is null
                        || pullRequestDto.UpdatedOn is null)
                    {
                        continue;
                    }

                    if (pullRequestDto.UpdatedOn.Value < mergedSince)
                    {
                        return pullRequests;
                    }

                    var pullRequest = _snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId);
                    var activities = await GetPullRequestActivitiesAsync(
                        repositorySlug,
                        pullRequest.Id,
                        cancellationToken).ConfigureAwait(false);
                    var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);

                    pullRequests.Add(CreateMergedPullRequest(repository, pullRequest, pullRequestDto.UpdatedOn.Value, activitySummary));
                }

                url = page.Next;
            }
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return pullRequests;
    }

    private async Task<IReadOnlyList<PullRequestSnapshot>> GetPullRequestSnapshotsAsync(
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

        var pullRequests = new List<PullRequestSnapshot>();

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

                pullRequests.Add(_snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId));
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

    private static PullRequestDetail CreatePullRequestDetail(
        Repository repository,
        PullRequestSnapshot pullRequest,
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

    private static MergedPullRequest CreateMergedPullRequest(
        Repository repository,
        PullRequestSnapshot pullRequest,
        DateTimeOffset mergedOn,
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
            mergedOn,
            pullRequest.DescriptionText,
            activitySummary.CommentsCount,
            pullRequest.RequestChangesCount,
            pullRequest.HasCurrentUserRequestChanges,
            pullRequest.ApprovalsCount,
            pullRequest.HasCurrentUserApproval);

    private static bool TryCreateDetailFromCache(
        Repository repository,
        PullRequestSnapshot pullRequest,
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

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketJsonParser _jsonParser;
    private readonly IPullRequestActivityAnalyzer _activityAnalyzer;
    private readonly IPullRequestSnapshotMapper _snapshotMapper;
    private readonly IPullRequestDetailsCache _cache;
    private readonly BitbucketOptions _options;

}
