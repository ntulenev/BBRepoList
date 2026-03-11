using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request summary DTO returned by the Bitbucket API.
/// </summary>
public sealed record PullRequestSummaryDto(
    [property: JsonPropertyName("raw")] string? Raw = null
);
