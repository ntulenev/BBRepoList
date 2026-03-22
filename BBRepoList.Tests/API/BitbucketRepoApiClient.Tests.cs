using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.API;

public sealed class BitbucketRepoApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketRepoApiClient(transport, options);

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
        Action act = () => _ = new BitbucketRepoApiClient(transport.Object, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetRepositoriesAsync returns mapped repositories when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenResponseIsValidReturnsRepositories()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var firstUrl = "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext";
        var nextUrl = "next";

        var firstDto = new RepoPageDto(
            [new RepositoryDto("Repo-1"), new RepositoryDto("Repo-2")],
            new Uri("next", UriKind.Relative));
        var secondDto = new RepoPageDto([new RepositoryDto("Repo-3")], null);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == firstUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstDto);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == nextUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondDto);

        var client = new BitbucketRepoApiClient(transport.Object, Options.Create(CreateOptions()));
        // Act
        var repositories = new List<Repository>();
        await foreach (var repository in client.GetRepositoriesAsync(new FilterPattern(null), cts.Token))
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
        var requestUrl = "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync((RepoPageDto?)null);

        var client = new BitbucketRepoApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        var repositories = new List<Repository>();
        await foreach (var repository in client.GetRepositoriesAsync(new FilterPattern(null), cts.Token))
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
        var requestUrl = "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketRepoApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in client.GetRepositoriesAsync(new FilterPattern(null), cts.Token))
            {
            }
        };

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetRepositoriesAsync appends server-side repository name filter when search phrase is provided")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedAppendsQueryParameter()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var requestUrl = "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext&q=name%20~%20%22App%22";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(new RepoPageDto([], null));

        var client = new BitbucketRepoApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        await foreach (var _ in client.GetRepositoriesAsync(new FilterPattern("App"), cts.Token))
        {
        }

        // Assert
        sendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetRepositoriesAsync escapes server-side repository name filter")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterContainsSpecialCharactersEscapesQueryParameter()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var requestUrl = "repositories/workspace?pagelen=25&fields=values.name%2Cvalues.slug%2Cvalues.created_on%2Cvalues.updated_on%2Cnext&q=name%20~%20%22A%5C%22B%5C%5CRepo%22";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<RepoPageDto>(
                It.Is<Uri>(u => u.ToString() == requestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(new RepoPageDto([], null));

        var client = new BitbucketRepoApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        await foreach (var _ in client.GetRepositoriesAsync(new FilterPattern("A\"B\\Repo"), cts.Token))
        {
        }

        // Assert
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


