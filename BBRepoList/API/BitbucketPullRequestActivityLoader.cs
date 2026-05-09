using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Loads and parses Bitbucket pull request activity pages.
/// </summary>
public sealed class BitbucketPullRequestActivityLoader : IBitbucketPullRequestActivityLoader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketPullRequestActivityLoader"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    /// <param name="jsonParser">Bitbucket JSON parser.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketPullRequestActivityLoader(
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
    public async Task<IReadOnlyList<PullRequestActivityEntry>> GetActivitiesAsync(
        string repositorySlug,
        int pullRequestId,
        CancellationToken cancellationToken)
    {
        var url = CreatePullRequestActivitiesUrl(repositorySlug, pullRequestId);
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

    private Uri CreatePullRequestActivitiesUrl(string repositorySlug, int pullRequestId) =>
        new(
            $"repositories/{_options.Workspace}/{Uri.EscapeDataString(repositorySlug)}/pullrequests/{pullRequestId}/activity?pagelen={_options.PageLen}&fields={Uri.EscapeDataString(PULL_REQUEST_ACTIVITY_FIELDS)}",
            UriKind.Relative);

    private readonly IBitbucketTransport _transport;
    private readonly IBitbucketJsonParser _jsonParser;
    private readonly BitbucketOptions _options;

    private const string PULL_REQUEST_ACTIVITY_FIELDS =
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
        "next";
}
