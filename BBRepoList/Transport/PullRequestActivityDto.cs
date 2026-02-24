using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBRepoList.Transport;

/// <summary>
/// Pull request activity item DTO.
/// </summary>
public sealed class PullRequestActivityDto
{
    /// <summary>
    /// Gets or sets unmodeled activity payload fields.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Properties { get; init; }
}
