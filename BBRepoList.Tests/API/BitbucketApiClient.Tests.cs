using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using FluentAssertions;
using Moq;
using Moq.Protected;

using BBRepoList.API;
using BBRepoList.Transport;

namespace BBRepoList.Tests.API;

public sealed class BitbucketApiClientTests
{
    [Fact(DisplayName = "Constructor throws when http client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHttpClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        HttpClient http = null!;

        // Act
        Action act = () => _ = new BitbucketApiClient(http);

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

    [Fact(DisplayName = "GetRepositoriesPageAsync throws when url is null")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesPageAsyncWhenUrlIsNullThrowsArgumentNullException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var http = new HttpClient();
        var client = new BitbucketApiClient(http);
        Uri url = null!;

        // Act
        Func<Task> act = () => client.GetRepositoriesPageAsync(url, cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetRepositoriesPageAsync returns mapped page when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesPageAsyncWhenResponseIsValidReturnsPage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var expectedRequestUri = new Uri(baseUri, requestUrl);

        var dto = new RepoPageDto(
            [new RepositoryDto("Repo-1")],
            new Uri("next", UriKind.Relative));
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
        var client = new BitbucketApiClient(http);

        // Act
        var page = await client.GetRepositoriesPageAsync(requestUrl, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        page.Values.Should().HaveCount(1);
        page.Values[0].Name.Should().Be("Repo-1");
        page.Next.Should().Be(new Uri("next", UriKind.Relative));
    }

    [Fact(DisplayName = "GetRepositoriesPageAsync returns empty page when response body is null")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesPageAsyncWhenResponseBodyIsNullReturnsEmptyPage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var expectedRequestUri = new Uri(baseUri, requestUrl);

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
        var client = new BitbucketApiClient(http);

        // Act
        var page = await client.GetRepositoriesPageAsync(requestUrl, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        page.Values.Should().NotBeNull();
        page.Values.Should().BeEmpty();
        page.Next.Should().BeNull();
    }

    [Fact(DisplayName = "GetRepositoriesPageAsync throws when response is not successful")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesPageAsyncWhenResponseIsFailureThrowsHttpRequestException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var expectedRequestUri = new Uri(baseUri, requestUrl);
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
                    req.Method == HttpMethod.Get && req.RequestUri == expectedRequestUri),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var client = new BitbucketApiClient(http);

        // Act
        Func<Task> act = () => client.GetRepositoriesPageAsync(requestUrl, cts.Token);

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
        var client = new BitbucketApiClient(http);

        // Act
        var user = await client.AuthSelfCheckAsync(cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        user.DisplayName.Value.Should().Be("Jane Doe");
        user.Uuid.Should().Be(new BBRepoList.Models.BitbucketId("{uuid}"));
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
        var client = new BitbucketApiClient(http);

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
        var client = new BitbucketApiClient(http);

        // Act
        Func<Task> act = () => client.AuthSelfCheckAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }
}
