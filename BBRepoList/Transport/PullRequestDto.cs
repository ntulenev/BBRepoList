using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request DTO returned by the Bitbucket API.
/// </summary>
public sealed record PullRequestDto(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("created_on")] DateTimeOffset? CreatedOn = null,
    [property: JsonPropertyName("updated_on")] DateTimeOffset? UpdatedOn = null,
    [property: JsonPropertyName("state")] string? State = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("summary")] PullRequestSummaryDto? Summary = null,
    [property: JsonPropertyName("author")] PullRequestAuthorDto? Author = null,
    [property: JsonPropertyName("source")] PullRequestSourceDto? Source = null,
    [property: JsonPropertyName("comment_count")] int? CommentCount = null,
    [property: JsonPropertyName("task_count")] int? TaskCount = null,
    [property: JsonPropertyName("participants")] ICollection<PullRequestParticipantDto>? Participants = null
);
