using System.Net;

using FluentAssertions;

using BBRepoList.Configuration;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.Tests.Transport;

public sealed class BitbucketRetryPolicyTests
{
    [Fact(DisplayName = "TryGetDelay returns false when retry attempt is out of range")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenRetryAttemptIsOutOfRangeReturnsFalse()
    {
        // Arrange
        var policy = new BitbucketRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var resultZero = policy.TryGetDelay(0, HttpStatusCode.ServiceUnavailable, null, out _);
        var resultTooHigh = policy.TryGetDelay(2, HttpStatusCode.ServiceUnavailable, null, out _);

        // Assert
        resultZero.Should().BeFalse();
        resultTooHigh.Should().BeFalse();
    }

    [Fact(DisplayName = "TryGetDelay retries on transient status codes")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenStatusIsRetryableReturnsTrue()
    {
        // Arrange
        var policy = new BitbucketRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, HttpStatusCode.TooManyRequests, null, out var delay);

        // Assert
        result.Should().BeTrue();
        delay.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact(DisplayName = "TryGetDelay retries on HttpRequestException")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenExceptionIsHttpRequestExceptionReturnsTrue()
    {
        // Arrange
        var policy = new BitbucketRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, null, new HttpRequestException("boom"), out var delay);

        // Assert
        result.Should().BeTrue();
        delay.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact(DisplayName = "TryGetDelay returns false for non-retryable status codes")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenStatusIsNotRetryableReturnsFalse()
    {
        // Arrange
        var policy = new BitbucketRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, HttpStatusCode.BadRequest, null, out _);

        // Assert
        result.Should().BeFalse();
    }

    private static BitbucketOptions CreateOptions(int retryCount)
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://example.test/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = retryCount
        };
    }
}
