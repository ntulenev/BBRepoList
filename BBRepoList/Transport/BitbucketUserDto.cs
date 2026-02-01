using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Bitbucket user profile DTO returned by the "user" endpoint.
/// </summary>
public sealed record BitbucketUserDto(
    [property: JsonPropertyName("uuid")] string Id,
    [property: JsonPropertyName("display_name")] string? DisplayName
);
