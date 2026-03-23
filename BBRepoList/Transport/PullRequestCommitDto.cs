using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request source commit DTO.
/// </summary>
public sealed record PullRequestCommitDto(
    [property: JsonPropertyName("hash")] string? Hash = null
);
