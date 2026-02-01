using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using FluentAssertions;
using Moq;
using Moq.Protected;

using BBRepoList.API;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.Tests.API;

public sealed class BitbucketApiClientTests
{
    [Fact(DisplayName = "Constructor throws when http client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHttpClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        HttpClient http = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketApiClient(http, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        using var http = new HttpClient();
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketApiClient(http, options);

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
        var baseUri = new Uri("https://example.test/");
        var firstUrl = new Uri(baseUri, "repositories/workspace?pagelen=25");
        var nextUrl = new Uri(baseUri, "next");

        var firstDto = new RepoPageDto(
            [new RepositoryDto("Repo-1"), new RepositoryDto("Repo-2")],
            new Uri("next", UriKind.Relative));
        var firstJson = JsonSerializer.Serialize(firstDto);

        var secondDto = new RepoPageDto(
            [new RepositoryDto("Repo-3")],
            null);
        var secondJson = JsonSerializer.Serialize(secondDto);

        using var firstResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(firstJson, Encoding.UTF8, "application/json")
        };
        using var secondResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(secondJson, Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == firstUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstResponse);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == nextUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondResponse);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));
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
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "repositories/workspace?pagelen=25");

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));

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
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "repositories/workspace?pagelen=25");
        var body = "error body";

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));

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
        var baseUri = new Uri("https://example.test/");
        var expectedRequestUri = new Uri(baseUri, "user");

        var dto = new BitbucketUserDto("{uuid}", "Jane Doe");
        var json = JsonSerializer.Serialize(dto);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == expectedRequestUri),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));

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
        var baseUri = new Uri("https://example.test/");
        var expectedRequestUri = new Uri(baseUri, "user");

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == expectedRequestUri),
               ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));

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
        var baseUri = new Uri("https://example.test/");
        var expectedRequestUri = new Uri(baseUri, "user");
        var body = "error body";

        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            ReasonPhrase = "Unauthorized",
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == expectedRequestUri),
               ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http, Options.Create(CreateOptions()));

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
            PageLen = 25
        };
    }
}
