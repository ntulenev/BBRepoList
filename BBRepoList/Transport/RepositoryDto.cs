using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Repository DTO returned by the Bitbucket API.
/// </summary>
public sealed record RepositoryDto(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("created_on")] DateTimeOffset? CreatedOn = null,
    [property: JsonPropertyName("updated_on")] DateTimeOffset? UpdatedOn = null,
    [property: JsonPropertyName("slug")] string? Slug = null,
    [property: JsonPropertyName("open_pull_requests_count")] int? OpenPullRequestsCount = null
);
