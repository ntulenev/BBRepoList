using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Builds fingerprints for Bitbucket pull request DTOs.
/// </summary>
public interface IPullRequestFingerprintBuilder
{
    /// <summary>
    /// Builds a fingerprint from pull request fields that affect report details.
    /// </summary>
    /// <param name="pullRequest">Pull request DTO.</param>
    /// <returns>Stable fingerprint for cache validation.</returns>
    string BuildFingerprint(PullRequestDto pullRequest);
}
