using System.Text.Json.Serialization;

namespace BBRepoList.Models;

/// <summary>
/// Page container returned by the Bitbucket repositories API.
/// </summary>
/// <param name="Values">Repository items on this page.</param>
/// <param name="Next">URL for the next page, if any.</param>
public sealed record RepoPage(
    [property: JsonPropertyName("values")] ICollection<Repository> Values,
    [property: JsonPropertyName("next")] Uri? Next
);
