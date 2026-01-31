using System.Text.Json.Serialization;

namespace BBRepoList.Models;

/// <summary>
/// Repository data returned by the Bitbucket API.
/// </summary>
/// <param name="Name">Repository display name.</param>
public sealed record Repository(
    [property: JsonPropertyName("name")] string Name
);