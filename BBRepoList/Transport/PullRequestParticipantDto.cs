using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request participant DTO returned by the Bitbucket API.
/// </summary>
public sealed record PullRequestParticipantDto(
    [property: JsonPropertyName("user")] PullRequestAuthorDto? User = null,
    [property: JsonPropertyName("approved")] bool? Approved = null,
    [property: JsonPropertyName("state")] string? State = null
);
