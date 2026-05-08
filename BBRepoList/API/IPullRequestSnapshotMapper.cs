using BBRepoList.Models;
using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Maps Bitbucket pull request DTOs to pull request snapshots.
/// </summary>
public interface IPullRequestSnapshotMapper
{
    /// <summary>
    /// Maps a Bitbucket pull request DTO to a pull request snapshot.
    /// </summary>
    /// <param name="pullRequestDto">Pull request DTO.</param>
    /// <param name="currentUserId">Current authenticated Bitbucket user id.</param>
    /// <returns>Pull request snapshot.</returns>
    PullRequestSnapshot CreateSnapshot(PullRequestDto pullRequestDto, BitbucketId currentUserId);
}
