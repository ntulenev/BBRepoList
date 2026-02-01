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
    /// <param name="filterPattern">Repository name filter pattern.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching repositories.</returns>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        FilterPattern filterPattern,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken);
}
