using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Repository DTO returned by the Bitbucket API.
/// </summary>
public sealed record RepositoryDto(
    [property: JsonPropertyName("name")] string? Name
);
