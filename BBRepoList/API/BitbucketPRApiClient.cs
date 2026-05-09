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
    /// <param name="activityAnalyzer">Pull request activity analyzer.</param>
    /// <param name="activityLoader">Pull request activity loader.</param>
    /// <param name="snapshotMapper">Pull request snapshot mapper.</param>
    /// <param name="cacheService">Pull request details cache service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(
        IBitbucketTransport transport,
        IPullRequestActivityAnalyzer activityAnalyzer,
        IBitbucketPullRequestActivityLoader activityLoader,
        IPullRequestSnapshotMapper snapshotMapper,
        IPullRequestDetailsCacheService cacheService,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(activityAnalyzer);
        ArgumentNullException.ThrowIfNull(activityLoader);
        ArgumentNullException.ThrowIfNull(snapshotMapper);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _activityAnalyzer = activityAnalyzer;
        _activityLoader = activityLoader;
        _snapshotMapper = snapshotMapper;
        _cacheService = cacheService;
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
            var url = CreateOpenPullRequestCountUrl(repositorySlug);
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
            var cacheEntriesByPullRequestId = await _cacheService
                .ReadEntriesByPullRequestIdAsync(_options.Workspace, repositorySlug, currentUserId, cancellationToken)
                .ConfigureAwait(false);

            var openPullRequests = await GetPullRequestSnapshotsAsync(
                repositorySlug,
                currentUserId,
                cancellationToken).ConfigureAwait(false);
            repository.UpdateOpenPullRequestsCount(openPullRequests.Count);

            if (openPullRequests.Count == 0)
            {
                await _cacheService
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

                var activities = await _activityLoader.GetActivitiesAsync(
                    repositorySlug,
                    pullRequest.Id,
                    cancellationToken).ConfigureAwait(false);
                var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);

                details.Add(CreatePullRequestDetail(repository, pullRequest, activitySummary));
                updatedCacheEntries.Add(_cacheService.CreateEntry(pullRequest, activitySummary));
            }

            await _cacheService
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
            var url = CreateMergedPullRequestsUrl(repositorySlug);

            await ForEachPullRequestDtoAsync(
                url,
                async (pullRequestDto, token) =>
                {
                    if (pullRequestDto.Id is null
                        || pullRequestDto.Id <= 0
                        || pullRequestDto.CreatedOn is null
                        || pullRequestDto.UpdatedOn is null)
                    {
                        return true;
                    }

                    if (pullRequestDto.UpdatedOn.Value < mergedSince)
                    {
                        return false;
                    }

                    var pullRequest = _snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId);
                    var activities = await _activityLoader.GetActivitiesAsync(
                        repositorySlug,
                        pullRequest.Id,
                        token).ConfigureAwait(false);
                    var activitySummary = _activityAnalyzer.CreateSummary(activities, pullRequest, currentUserId);

                    pullRequests.Add(CreateMergedPullRequest(repository, pullRequest, pullRequestDto.UpdatedOn.Value, activitySummary));
                    return true;
                },
                cancellationToken).ConfigureAwait(false);
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
        var url = CreateOpenPullRequestSnapshotsUrl(repositorySlug);
        var pullRequests = new List<PullRequestSnapshot>();

        await ForEachPullRequestDtoAsync(
            url,
            (pullRequestDto, _) =>
            {
                if (pullRequestDto.Id is null || pullRequestDto.Id <= 0 || pullRequestDto.CreatedOn is null)
                {
                    return ValueTask.FromResult(true);
                }

                pullRequests.Add(_snapshotMapper.CreateSnapshot(pullRequestDto, currentUserId));
                return ValueTask.FromResult(true);
            },
            cancellationToken).ConfigureAwait(false);

        return pullRequests;
    }

    private async Task ForEachPullRequestDtoAsync(
        Uri initialUrl,
        Func<PullRequestDto, CancellationToken, ValueTask<bool>> handlePullRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialUrl);
        ArgumentNullException.ThrowIfNull(handlePullRequest);

        var url = initialUrl;

        while (url is not null)
        {
            var page = await _transport.GetAsync<PullRequestPageDto>(url, cancellationToken).ConfigureAwait(false);
            if (page is null)
            {
                break;
            }

            foreach (var pullRequestDto in page.Values ?? [])
            {
                if (!await handlePullRequest(pullRequestDto, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            url = page.Next;
        }
    }

    private Uri CreateOpenPullRequestCountUrl(string repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=OPEN&pagelen=1&fields=size",
            UriKind.Relative);

    private Uri CreateMergedPullRequestsUrl(string repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=MERGED&pagelen={_options.PageLen}&sort=-updated_on&fields={EscapeFields(MERGED_PULL_REQUEST_FIELDS)}",
            UriKind.Relative);

    private Uri CreateOpenPullRequestSnapshotsUrl(string repositorySlug) =>
        new(
            $"repositories/{_options.Workspace}/{EscapeRepositorySlug(repositorySlug)}/pullrequests?state=OPEN&pagelen={_options.PageLen}&fields={EscapeFields(OPEN_PULL_REQUEST_SNAPSHOT_FIELDS)}",
            UriKind.Relative);

    private static string EscapeRepositorySlug(string repositorySlug) => Uri.EscapeDataString(repositorySlug);

    private static string EscapeFields(string fields) => Uri.EscapeDataString(fields);

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

    private bool TryCreateDetailFromCache(
        Repository repository,
        PullRequestSnapshot pullRequest,
        IReadOnlyDictionary<int, PullRequestDetailsCacheEntry> cacheEntriesByPullRequestId,
        out PullRequestDetail detail,
        out PullRequestDetailsCacheEntry cacheEntry)
    {
        detail = null!;

        if (!_cacheService.TryCreateActivitySummary(
            pullRequest,
            cacheEntriesByPullRequestId,
            out var activitySummary,
            out cacheEntry))
        {
            return false;
        }

        detail = CreatePullRequestDetail(repository, pullRequest, activitySummary);
        return true;
    }

    private readonly IBitbucketTransport _transport;
    private readonly IPullRequestActivityAnalyzer _activityAnalyzer;
    private readonly IBitbucketPullRequestActivityLoader _activityLoader;
    private readonly IPullRequestSnapshotMapper _snapshotMapper;
    private readonly IPullRequestDetailsCacheService _cacheService;
    private readonly BitbucketOptions _options;

    private const string MERGED_PULL_REQUEST_FIELDS =
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
        "next";

    private const string OPEN_PULL_REQUEST_SNAPSHOT_FIELDS =
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
        "next";

}
