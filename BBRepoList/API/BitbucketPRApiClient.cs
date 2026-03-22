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
            var openPullRequests = await GetOpenPullRequestsAsync(
                repositorySlug,
                currentUserId,
                cancellationToken).ConfigureAwait(false);

            foreach (var pullRequest in openPullRequests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activities = await GetPullRequestActivitiesAsync(
                    repositorySlug,
                    pullRequest.Id,
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
                    pullRequest.RequestChangesCount,
                    pullRequest.HasCurrentUserRequestChanges,
                    pullRequest.ApprovalsCount,
                    pullRequest.HasCurrentUserApproval));
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
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        var escapedSlug = Uri.EscapeDataString(repositorySlug);
        var fields = Uri.EscapeDataString(
            "values.id," +
            "values.title," +
            "values.created_on," +
            "values.description," +
            "values.summary.raw," +
            "values.author.uuid," +
            "values.author.display_name," +
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
                    hasCurrentUserApproval));
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

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketJsonParser _jsonParser;
    private readonly BitbucketOptions _options;
}
