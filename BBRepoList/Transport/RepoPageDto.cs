using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Page container returned by the Bitbucket repositories API.
/// </summary>
public sealed record RepoPageDto(
    [property: JsonPropertyName("values")] ICollection<RepositoryDto>? Values,
    [property: JsonPropertyName("next")] Uri? Next
);
