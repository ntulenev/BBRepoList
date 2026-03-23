using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request source DTO.
/// </summary>
public sealed record PullRequestSourceDto(
    [property: JsonPropertyName("commit")] PullRequestCommitDto? Commit = null
);
