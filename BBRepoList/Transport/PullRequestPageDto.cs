using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Page container returned by the pull requests API.
/// </summary>
public sealed record PullRequestPageDto(
    [property: JsonPropertyName("values")] ICollection<PullRequestDto>? Values,
    [property: JsonPropertyName("next")] Uri? Next
);
