using System.Net;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;

using Microsoft.Extensions.Options;

namespace BBRepoList.Transport;

/// <summary>
/// Default retry policy for Bitbucket transport requests.
/// </summary>
public sealed class BitbucketRetryPolicy : IBitbucketRetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketRetryPolicy(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool TryGetDelay(int retryAttempt, HttpStatusCode? statusCode, Exception? exception, out TimeSpan delay)
    {
        if (retryAttempt <= 0 || retryAttempt > _options.RetryCount)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        if (exception is HttpRequestException)
        {
            delay = TimeSpan.FromMilliseconds(BASE_DELAY_MS * retryAttempt);
            return true;
        }

        if (statusCode is not null && IsRetryable(statusCode.Value))
        {
            delay = TimeSpan.FromMilliseconds(BASE_DELAY_MS * retryAttempt);
            return true;
        }

        delay = TimeSpan.Zero;
        return false;
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode == HttpStatusCode.TooManyRequests || code >= 500;
    }

    private const int BASE_DELAY_MS = 200;
    private readonly BitbucketOptions _options;
}
