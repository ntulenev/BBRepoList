using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request page summary DTO.
/// </summary>
public sealed record PullRequestPageSummaryDto(
    [property: JsonPropertyName("size")] int? Size = null
);
