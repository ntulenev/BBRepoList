using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Loads Bitbucket pull request activity entries.
/// </summary>
public interface IBitbucketPullRequestActivityLoader
{
    /// <summary>
    /// Loads activity entries for a pull request.
    /// </summary>
    /// <param name="repositorySlug">Repository slug.</param>
    /// <param name="pullRequestId">Pull request identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Distinct pull request activity entries.</returns>
    Task<IReadOnlyList<PullRequestActivityEntry>> GetActivitiesAsync(
        string repositorySlug,
        int pullRequestId,
        CancellationToken cancellationToken);
}
