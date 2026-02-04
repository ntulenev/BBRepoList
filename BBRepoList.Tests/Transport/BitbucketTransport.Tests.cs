using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Moq;
using Moq.Protected;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.Tests.Transport;

public sealed class BitbucketTransportTests
{
    [Fact(DisplayName = "Constructor throws when http client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHttpClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        HttpClient http = null!;
        var retryPolicy = new BitbucketRetryPolicy(Options.Create(CreateOptions()));

        // Act
        Action act = () => _ = new BitbucketTransport(http, retryPolicy);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when retry policy is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRetryPolicyIsNullThrowsArgumentNullException()
    {
        // Arrange
        using var http = new HttpClient();
        IBitbucketRetryPolicy retryPolicy = null!;

        // Act
        Action act = () => _ = new BitbucketTransport(http, retryPolicy);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetAsync returns deserialized DTO when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsValidReturnsDto()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "user");

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
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new BitbucketTransport(http, new BitbucketRetryPolicy(Options.Create(CreateOptions())));

        // Act
        var result = await transport.GetAsync<BitbucketUserDto>(new Uri("user", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Jane Doe");
    }

    [Fact(DisplayName = "GetAsync returns null when response body is null")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseBodyIsNullReturnsNull()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "user");

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
        var transport = new BitbucketTransport(http, new BitbucketRetryPolicy(Options.Create(CreateOptions())));

        // Act
        var result = await transport.GetAsync<BitbucketUserDto>(new Uri("user", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetAsync throws when response is not successful")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsFailureThrowsHttpRequestException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "user");
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
        var transport = new BitbucketTransport(http, new BitbucketRetryPolicy(Options.Create(CreateOptions())));

        // Act
        Func<Task> act = () => transport.GetAsync<BitbucketUserDto>(new Uri("user", UriKind.Relative), cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetAsync retries transient failures and succeeds")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenTransientFailureRetriesAndSucceeds()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "user");

        using var firstResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable",
            Content = new StringContent("temporary", Encoding.UTF8, "text/plain")
        };

        var dto = new BitbucketUserDto("{uuid}", "Jane Doe");
        var json = JsonSerializer.Serialize(dto);

        using var secondResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var responses = new Queue<HttpResponseMessage>([firstResponse, secondResponse]);

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
            .ReturnsAsync(() => responses.Dequeue());

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new BitbucketTransport(http, new BitbucketRetryPolicy(Options.Create(CreateOptions(retryCount: 1))));

        // Act
        var result = await transport.GetAsync<BitbucketUserDto>(new Uri("user", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        result.Should().NotBeNull();
    }

    private static BitbucketOptions CreateOptions(int retryCount = 0)
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
