using System.Net.Http.Headers;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Moq;

namespace BBRepoList.Tests.API;

public sealed class BitbucketAuthApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenServiceProviderIsNullThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider services = null!;

        // Act
        Action act = () => _ = new BitbucketAuthApiClient(services);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader throws when auth email is null")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenAuthEmailIsNullThrowsArgumentNullException()
    {
        // Arrange
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var client = (IBitbucketAuthApiClient)new BitbucketAuthApiClient(services.Object);
        string authEmail = null!;
        var authApiToken = "token";

        // Act
        Action act = () => _ = client.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader throws when auth token is null")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenAuthTokenIsNullThrowsArgumentNullException()
    {
        // Arrange
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var client = (IBitbucketAuthApiClient)new BitbucketAuthApiClient(services.Object);
        var authEmail = "user@example.test";
        string authApiToken = null!;

        // Act
        Action act = () => _ = client.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader returns basic auth header")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenArgumentsAreValidReturnsHeader()
    {
        // Arrange
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var client = (IBitbucketAuthApiClient)new BitbucketAuthApiClient(services.Object);
        var authEmail = "user@example.test";
        var authApiToken = "token";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authEmail}:{authApiToken}"));

        // Act
        var header = client.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        header.Should().BeOfType<AuthenticationHeaderValue>();
        header.Scheme.Should().Be("Basic");
        header.Parameter.Should().Be(expected);
    }

    [Fact(DisplayName = "AuthSelfCheckAsync returns mapped user when response is valid")]
    [Trait("Category", "Unit")]
    public async Task AuthSelfCheckAsyncWhenResponseIsValidReturnsUser()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var expectedRequestUri = "user";

        var dto = new BitbucketUserDto("{uuid}", "Jane Doe");

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<BitbucketUserDto>(
                It.Is<Uri>(u => u.ToString() == expectedRequestUri),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(dto);

        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        services
            .Setup(p => p.GetService(typeof(IBitbucketTransport)))
            .Returns(transport.Object);

        var client = new BitbucketAuthApiClient(services.Object);

        // Act
        var user = await client.AuthSelfCheckAsync(cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        user.DisplayName.Value.Should().Be("Jane Doe");
        user.Uuid.Should().Be(new BitbucketId("{uuid}"));
    }

    [Fact(DisplayName = "AuthSelfCheckAsync throws when response body is null")]
    [Trait("Category", "Unit")]
    public async Task AuthSelfCheckAsyncWhenResponseBodyIsNullThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var expectedRequestUri = "user";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<BitbucketUserDto>(
                It.Is<Uri>(u => u.ToString() == expectedRequestUri),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync((BitbucketUserDto?)null);

        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        services
            .Setup(p => p.GetService(typeof(IBitbucketTransport)))
            .Returns(transport.Object);

        var client = new BitbucketAuthApiClient(services.Object);

        // Act
        Func<Task> act = () => client.AuthSelfCheckAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        sendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "AuthSelfCheckAsync throws when response is not successful")]
    [Trait("Category", "Unit")]
    public async Task AuthSelfCheckAsyncWhenResponseIsFailureThrowsHttpRequestException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var expectedRequestUri = "user";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<BitbucketUserDto>(
                It.Is<Uri>(u => u.ToString() == expectedRequestUri),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        services
            .Setup(p => p.GetService(typeof(IBitbucketTransport)))
            .Returns(transport.Object);

        var client = new BitbucketAuthApiClient(services.Object);

        // Act
        Func<Task> act = () => client.AuthSelfCheckAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }
}
