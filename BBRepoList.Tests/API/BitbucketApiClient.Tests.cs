using System.Net.Http.Headers;
using System.Text;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.API;

public sealed class BitbucketApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketApiClient(transport, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketApiClient(transport.Object, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader throws when auth email is null")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenAuthEmailIsNullThrowsArgumentNullException()
    {
        // Arrange
        string authEmail = null!;
        var authApiToken = "token";

        // Act
        Action act = () => _ = BitbucketApiClient.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader throws when auth token is null")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenAuthTokenIsNullThrowsArgumentNullException()
    {
        // Arrange
        var authEmail = "user@example.test";
        string authApiToken = null!;

        // Act
        Action act = () => _ = BitbucketApiClient.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildAuthHeader returns basic auth header")]
    [Trait("Category", "Unit")]
    public void BuildAuthHeaderWhenArgumentsAreValidReturnsHeader()
    {
        // Arrange
        var authEmail = "user@example.test";
        var authApiToken = "token";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authEmail}:{authApiToken}"));

        // Act
        var header = BitbucketApiClient.BuildAuthHeader(authEmail, authApiToken);

        // Assert
        header.Should().BeOfType<AuthenticationHeaderValue>();
        header.Scheme.Should().Be("Basic");
        header.Parameter.Should().Be(expected);
    }

    [Fact(DisplayName = "GetRepositoriesAsync returns mapped repositories when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenResponseIsValidReturnsRepositories()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var firstUrl = "repositories/workspace?pagelen=25";
        var nextUrl = "next";

        var firstDto = new RepoPageDto(
            [new RepositoryDto("Repo-1"), new RepositoryDto("Repo-2")],
            new Uri("next", UriKind.Relative));
        var secondDto = new RepoPageDto([new RepositoryDto("Repo-3")], null);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == firstUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstDto);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == nextUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondDto);

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));
        // Act
        var repositories = new List<Repository>();
        await foreach (var repository in client.GetRepositoriesAsync(cts.Token))
        {
            repositories.Add(repository);
        }

        // Assert
        sendCalls.Should().Be(2);
        repositories.Should().HaveCount(3);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2", "Repo-3");
    }

    [Fact(DisplayName = "GetRepositoriesAsync returns empty sequence when response body is null")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenResponseBodyIsNullReturnsEmptySequence()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var requestUrl = "repositories/workspace?pagelen=25";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync((RepoPageDto?)null);

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        var repositories = new List<Repository>();
        await foreach (var repository in client.GetRepositoriesAsync(cts.Token))
        {
            repositories.Add(repository);
        }

        // Assert
        sendCalls.Should().Be(1);
        repositories.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetRepositoriesAsync throws when response is not successful")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenResponseIsFailureThrowsHttpRequestException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var requestUrl = "repositories/workspace?pagelen=25";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in client.GetRepositoriesAsync(cts.Token))
            {
            }
        };

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
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

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

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

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

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

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        Func<Task> act = () => client.AuthSelfCheckAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }

    private static BitbucketOptions CreateOptions()
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://example.test/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };
    }
}
