using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request DTO returned by the Bitbucket API.
/// </summary>
public sealed record PullRequestDto(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("created_on")] DateTimeOffset? CreatedOn = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("summary")] PullRequestSummaryDto? Summary = null,
    [property: JsonPropertyName("author")] PullRequestAuthorDto? Author = null
);
