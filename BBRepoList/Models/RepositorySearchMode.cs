namespace BBRepoList.Models;

/// <summary>
/// Supported repository name search modes.
/// </summary>
public enum RepositorySearchMode
{
    /// <summary>
    /// Matches repositories when the name contains the search phrase.
    /// </summary>
    Contains,

    /// <summary>
    /// Matches repositories when the name starts with the search phrase.
    /// </summary>
    StartWith
}
