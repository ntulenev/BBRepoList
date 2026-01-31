using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Provides access to Bitbucket repositories with optional filtering.
/// </summary>
public interface IRepoService
{
    /// <summary>
    /// Retrieves repositories with an optional name filter.
    /// </summary>
    /// <param name="searchPhrase">Substring to match against repository names.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching repositories.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        string? searchPhrase,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken);
}