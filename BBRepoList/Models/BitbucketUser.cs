using System.Text.Json.Serialization;

namespace BBRepoList.Models;

/// <summary>
/// Bitbucket user profile returned by the "user" endpoint.
/// </summary>
/// <param name="DisplayName">User display name.</param>
/// <param name="Nickname">User nickname.</param>
/// <param name="Uuid">User UUID.</param>
/// <param name="AccountId">User account identifier.</param>
public sealed record BitbucketUser(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("nickname")] string? Nickname,
    [property: JsonPropertyName("uuid")] string? Uuid,
    [property: JsonPropertyName("account_id")] string? AccountId
);