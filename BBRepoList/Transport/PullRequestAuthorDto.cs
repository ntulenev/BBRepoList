using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request author DTO.
/// </summary>
public sealed record PullRequestAuthorDto(
    [property: JsonPropertyName("uuid")] string? Uuid = null,
    [property: JsonPropertyName("display_name")] string? DisplayName = null
);
