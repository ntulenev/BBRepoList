using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Repository DTO returned by the Bitbucket API.
/// </summary>
public sealed record RepositoryDto(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("created_on")] DateTimeOffset? CreatedOn = null
);
