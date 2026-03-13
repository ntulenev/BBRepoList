using System.Text.Json;

using BBRepoList.Models;
using BBRepoList.Transport;

namespace BBRepoList.Abstractions;

/// <summary>
/// Parses common Bitbucket-specific values from JSON payloads.
/// </summary>
public interface IBitbucketJsonParser
{
    /// <summary>
    /// Attempts to parse a Bitbucket UUID from an object that contains a <c>uuid</c> property.
    /// </summary>
    /// <param name="element">JSON object candidate.</param>
    /// <param name="uuid">Parsed Bitbucket identifier when successful.</param>
    /// <returns><see langword="true" /> when a valid identifier was parsed.</returns>
    bool TryReadUuidFromObject(JsonElement element, out BitbucketId uuid);

    /// <summary>
    /// Attempts to parse a date-time value from a JSON string node.
    /// </summary>
    /// <param name="element">JSON string candidate.</param>
    /// <param name="value">Parsed date-time when successful.</param>
    /// <returns><see langword="true" /> when parsing succeeds.</returns>
    bool TryReadDateTime(JsonElement element, out DateTimeOffset value);

    /// <summary>
    /// Walks a JSON node and adds parsed pull request activity entries into the target collection.
    /// </summary>
    /// <param name="element">Root JSON element to parse.</param>
    /// <param name="isCommentContext">Whether current scope should be treated as comment activity.</param>
    /// <param name="onEntry">Callback invoked for each parsed activity entry.</param>
    void AddActivityEntriesFromJson(
        JsonElement element,
        bool isCommentContext,
        Action<BitbucketId, DateTimeOffset, bool> onEntry);

    /// <summary>
    /// Determines whether a participant state represents a request for changes.
    /// </summary>
    /// <param name="state">Participant review state value.</param>
    /// <returns><see langword="true" /> when the state indicates changes were requested.</returns>
    bool IsRequestChangesState(string? state);

    /// <summary>
    /// Determines whether a participant should be counted as an approval.
    /// </summary>
    /// <param name="participant">Pull request participant to inspect.</param>
    /// <returns><see langword="true" /> when the participant approved the pull request.</returns>
    bool IsApprovalState(PullRequestParticipantDto participant);
}
