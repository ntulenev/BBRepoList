using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync enriches open pull requests count when slug is present")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenSlugIsPresentLoadsOpenPullRequestsCount()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestSummaryUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=1&fields=size";

        var repository = new Repository("Repo-1", null, null, null, "repo-1");
        var pullRequestSummaryDto = new PullRequestPageSummaryDto(9);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestSummaryUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(pullRequestSummaryDto);

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        var enriched = await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        enriched.Name.Should().Be("Repo-1");
        enriched.OpenPullRequestsCount.Should().Be(9);
    }

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync keeps repository when pull requests count lookup fails")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenPullRequestsLookupFailsKeepsRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestSummaryUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=1&fields=size";
        var repository = new Repository("Repo-1", null, null, null, "repo-1");

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestSummaryUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));

        // Act
        var enriched = await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        enriched.Name.Should().Be("Repo-1");
        enriched.OpenPullRequestsCount.Should().BeNull();
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

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns mapped open pull request details")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenResponsesAreValidReturnsDetails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestsUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=25";
        var firstActivityUrl = "repositories/workspace/repo-1/pullrequests/101/activity?pagelen=25";
        var secondActivityUrl = "repositories/workspace/repo-1/pullrequests/102/activity?pagelen=25";

        var pullRequestsDto = new PullRequestPageDto(
        [
            new PullRequestDto(
                Id: 101,
                Title: "Feature A",
                CreatedOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
                Author: new PullRequestAuthorDto("{author-1}", "Author 1")),
            new PullRequestDto(
                Id: 102,
                Title: "Feature B",
                CreatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
                Author: new PullRequestAuthorDto("{author-2}", "Author 2"))
        ],
            null);

        var firstActivityDto = new PullRequestActivityPageDto(
        [
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["approval"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{reviewer-1}" },
                          "date": "2026-02-24T10:00:00+00:00"
                        }
                        """)
                }
            },
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["comment"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{current-user}" },
                          "created_on": "2026-02-24T11:00:00+00:00"
                        }
                        """)
                }
            }
        ],
            null);

        var secondActivityDto = new PullRequestActivityPageDto(
        [
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["comment"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{author-2}" },
                          "created_on": "2026-02-24T10:30:00+00:00"
                        }
                        """)
                }
            }
        ],
            null);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestsUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(pullRequestsDto);
        transport
            .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == firstActivityUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstActivityDto);
        transport
            .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == secondActivityUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondActivityDto);

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));
        var repository = new Repository(
            "Repo-1",
            new DateTimeOffset(2023, 1, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            openPullRequestsCount: 2,
            "repo-1");

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(
            repository,
            new BitbucketId("{current-user}"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(3);
        details.Should().HaveCount(2);
        details.Select(d => d.PullRequestId).Should().ContainInOrder(101, 102);
        details[0].FirstNonAuthorActivityOn.Should().Be(new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero));
        details[0].HasCurrentUserDiscussion.Should().BeTrue();
        details[0].TimeToFirstResponse.Should().Be(TimeSpan.FromHours(2));
        details[1].FirstNonAuthorActivityOn.Should().BeNull();
        details[1].HasCurrentUserDiscussion.Should().BeFalse();
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns empty list when lookup fails")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenLookupFailsReturnsEmptyList()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestsUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=25";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestsUrl),
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketApiClient(transport.Object, Options.Create(CreateOptions()));
        var repository = new Repository("Repo-1", null, null, 1, "repo-1");

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(
            repository,
            new BitbucketId("{current-user}"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        details.Should().BeEmpty();
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
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
