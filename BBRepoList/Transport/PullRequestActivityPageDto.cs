using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Page container returned by the pull request activity API.
/// </summary>
public sealed record PullRequestActivityPageDto(
    [property: JsonPropertyName("values")] ICollection<PullRequestActivityDto>? Values,
    [property: JsonPropertyName("next")] Uri? Next
);
