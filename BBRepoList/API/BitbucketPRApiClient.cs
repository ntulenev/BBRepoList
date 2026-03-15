using System.Globalization;

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
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(
        IBitbucketTransport transport,
        IBitbucketJsonParser jsonParser,
        IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(jsonParser);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _jsonParser = jsonParser;
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
            var openPullRequests = await GetOpenPullRequestsAsync(repositorySlug, cancellationToken).ConfigureAwait(false);

            foreach (var pullRequest in openPullRequests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activities = await GetPullRequestActivitiesAsync(
                    repositorySlug,
                    pullRequest.Id,
                    cancellationToken).ConfigureAwait(false);
                var (requestChangesCount, hasCurrentUserRequestChanges, approvalsCount, hasCurrentUserApproval) =
                    await GetPullRequestReviewStateAsync(
                    repositorySlug,
                    pullRequest.Id,
                    currentUserId,
                    cancellationToken).ConfigureAwait(false);

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

                details.Add(new PullRequestDetail(
                    repository,
                    pullRequest.Id,
                    pullRequest.Title,
                    pullRequest.CreatedOn,
                    pullRequest.AuthorId,
                    pullRequest.AuthorDisplayName,
                    firstNonAuthorActivityOn,
                    lastActivityOn,
                    hasCurrentUserDiscussion,
                    pullRequest.DescriptionText,
                    commentsCount,
                    requestChangesCount,
                    hasCurrentUserRequestChanges,
                    approvalsCount,
                    hasCurrentUserApproval));
            }
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return details;
    }

    private async Task<IReadOnlyList<OpenPullRequest>> GetOpenPullRequestsAsync(
        string repositorySlug,
        CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(repositorySlug);
        var url = new Uri(
            $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests?state=OPEN&pagelen={_options.PageLen}",
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

                pullRequests.Add(new OpenPullRequest(
                    pullRequestDto.Id.Value,
                    string.IsNullOrWhiteSpace(pullRequestDto.Title)
                        ? $"PR-{pullRequestDto.Id.Value.ToString(CultureInfo.InvariantCulture)}"
                        : pullRequestDto.Title.Trim(),
                    pullRequestDto.CreatedOn.Value,
                    descriptionText,
                    authorId,
                    pullRequestDto.Author?.DisplayName));
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
        var url = new Uri(
            $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests/{pullRequestId}/activity?pagelen={_options.PageLen}",
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

    private async Task<(int RequestChangesCount, bool HasCurrentUserRequestChanges, int ApprovalsCount, bool HasCurrentUserApproval)>
        GetPullRequestReviewStateAsync(
        string repositorySlug,
        int pullRequestId,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(repositorySlug);
        var url = new Uri(
            $"repositories/{_options.Workspace}/{escapedSlug}/pullrequests/{pullRequestId}",
            UriKind.Relative);

        try
        {
            var pullRequest = await _transport.GetAsync<PullRequestDto>(url, cancellationToken).ConfigureAwait(false);
            if (pullRequest?.Participants is null || pullRequest.Participants.Count == 0)
            {
                return default;
            }

            var requestChangesCount = 0;
            var hasCurrentUserRequestChanges = false;
            var approvalsCount = 0;
            var hasCurrentUserApproval = false;

            foreach (var participant in pullRequest.Participants)
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
        catch (HttpRequestException)
        {
            return default;
        }
    }

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketJsonParser _jsonParser;
    private readonly BitbucketOptions _options;
}
