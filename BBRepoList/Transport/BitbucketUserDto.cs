using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Bitbucket user profile DTO returned by the "user" endpoint.
/// </summary>
public sealed record BitbucketUserDto(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("nickname")] string? Nickname,
    [property: JsonPropertyName("uuid")] string? Uuid,
    [property: JsonPropertyName("account_id")] string? AccountId
);
