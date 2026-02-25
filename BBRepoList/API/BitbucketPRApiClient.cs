using System.Globalization;
using System.Text.Json;

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
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPRApiClient(IBitbucketTransport transport, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
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
            repository.UpdateOpenPullRequestsCount(summary?.Size);
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

        if (string.IsNullOrWhiteSpace(repository.Slug) || repository.OpenPullRequestsCount == 0)
        {
            return [];
        }

        var normalizedCurrentUserId = NormalizeUuid(currentUserId.Value);
        var details = new List<PullRequestDetail>();

        try
        {
            var openPullRequests = await GetOpenPullRequestsAsync(repository.Slug, cancellationToken).ConfigureAwait(false);

            foreach (var pullRequest in openPullRequests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activities = await GetPullRequestActivitiesAsync(
                    repository.Slug,
                    pullRequest.Id,
                    cancellationToken).ConfigureAwait(false);

                var normalizedAuthorUuid = NormalizeUuid(pullRequest.AuthorUuid);
                var firstNonAuthorActivityOn = activities
                    .Where(activity => string.IsNullOrWhiteSpace(normalizedAuthorUuid)
                                       || !string.Equals(
                                           NormalizeUuid(activity.ActorUuid),
                                           normalizedAuthorUuid,
                                           StringComparison.OrdinalIgnoreCase))
                    .OrderBy(static activity => activity.HappenedOn)
                    .Select(static activity => (DateTimeOffset?)activity.HappenedOn)
                    .FirstOrDefault();

                var hasCurrentUserDiscussion = activities.Any(activity =>
                    activity.IsComment
                    && string.Equals(
                        NormalizeUuid(activity.ActorUuid),
                        normalizedCurrentUserId,
                        StringComparison.OrdinalIgnoreCase));

                details.Add(new PullRequestDetail(
                    repository.Name,
                    repository.Slug,
                    repository.CreatedOn,
                    pullRequest.Id,
                    pullRequest.Title,
                    pullRequest.CreatedOn,
                    pullRequest.AuthorUuid,
                    firstNonAuthorActivityOn,
                    hasCurrentUserDiscussion));
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

                pullRequests.Add(new OpenPullRequest(
                    pullRequestDto.Id.Value,
                    string.IsNullOrWhiteSpace(pullRequestDto.Title)
                        ? $"PR-{pullRequestDto.Id.Value.ToString(CultureInfo.InvariantCulture)}"
                        : pullRequestDto.Title.Trim(),
                    pullRequestDto.CreatedOn.Value,
                    pullRequestDto.Author?.Uuid));
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
                    AddActivityEntriesFromJson(property.Value, isCommentContext: property.Key.Equals("comment", StringComparison.OrdinalIgnoreCase), activities);
                }
            }

            url = page.Next;
        }

        return [.. activities.DistinctBy(static activity => (activity.ActorUuid, activity.HappenedOn, activity.IsComment))];
    }

    private static void AddActivityEntriesFromJson(
        JsonElement element,
        bool isCommentContext,
        ICollection<PullRequestActivityEntry> entries)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var currentScopeIsComment = isCommentContext;
            var actorUuid = default(string);
            var happenedOn = default(DateTimeOffset?);

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("comment", StringComparison.OrdinalIgnoreCase))
                {
                    currentScopeIsComment = true;
                }

                if (actorUuid is null
                    && (property.Name.Equals("user", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("actor", StringComparison.OrdinalIgnoreCase))
                    && TryReadUuidFromObject(property.Value, out var parsedUuid))
                {
                    actorUuid = parsedUuid;
                }

                if (happenedOn is null
                    && IsDateProperty(property.Name)
                    && TryReadDateTime(property.Value, out var parsedDate))
                {
                    happenedOn = parsedDate;
                }
            }

            if (!string.IsNullOrWhiteSpace(actorUuid) && happenedOn is not null)
            {
                entries.Add(new PullRequestActivityEntry(actorUuid, happenedOn.Value, currentScopeIsComment));
            }

            foreach (var property in element.EnumerateObject())
            {
                AddActivityEntriesFromJson(
                    property.Value,
                    currentScopeIsComment || property.Name.Equals("comment", StringComparison.OrdinalIgnoreCase),
                    entries);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AddActivityEntriesFromJson(item, isCommentContext, entries);
            }
        }
    }

    private static bool IsDateProperty(string propertyName) =>
        propertyName.Equals("date", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("created_on", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("updated_on", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadUuidFromObject(JsonElement element, out string uuid)
    {
        uuid = string.Empty;

        if (element.ValueKind is not JsonValueKind.Object
            || !element.TryGetProperty("uuid", out var uuidElement)
            || uuidElement.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var rawUuid = uuidElement.GetString();
        if (string.IsNullOrWhiteSpace(rawUuid))
        {
            return false;
        }

        uuid = rawUuid.Trim();
        return true;
    }

    private static bool TryReadDateTime(JsonElement element, out DateTimeOffset value)
    {
        value = default;

        if (element.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var raw = element.GetString();
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }

    private static string NormalizeUuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('{', '}');
    }

    private readonly IBitbucketTransport _transport;
    private readonly BitbucketOptions _options;
}
