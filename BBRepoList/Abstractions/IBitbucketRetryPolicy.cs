using System.Net;

namespace BBRepoList.Abstractions;

/// <summary>
/// Defines retry behavior for Bitbucket transport requests.
/// </summary>
public interface IBitbucketRetryPolicy
{
    /// <summary>
    /// Determines whether a retry should occur and returns the delay.
    /// </summary>
    /// <param name="retryAttempt">1-based retry attempt count.</param>
    /// <param name="statusCode">HTTP status code, if available.</param>
    /// <param name="exception">Exception, if available.</param>
    /// <param name="delay">Delay before the retry.</param>
    /// <returns>True if the request should be retried.</returns>
    bool TryGetDelay(int retryAttempt, HttpStatusCode? statusCode, Exception? exception, out TimeSpan delay);
}
