using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.API.Helpers;
using BBRepoList.Caching;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.API;

public sealed class BitbucketPRApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;
        var parser = new BitbucketJsonParser();
        var analyzer = new PullRequestActivityAnalyzer();
        var activityLoader = new Mock<IBitbucketPullRequestActivityLoader>(MockBehavior.Strict).Object;
        var snapshotMapper = CreateSnapshotMapper(parser);
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport, analyzer, activityLoader, snapshotMapper, CreateCacheService(cache), options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when activity loader is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenActivityLoaderIsNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        var analyzer = new PullRequestActivityAnalyzer();
        var snapshotMapper = CreateSnapshotMapper(new BitbucketJsonParser());
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object;
        IBitbucketPullRequestActivityLoader activityLoader = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport.Object, analyzer, activityLoader, snapshotMapper, CreateCacheService(cache), options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when cache service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCacheServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        var parser = new BitbucketJsonParser();
        var analyzer = new PullRequestActivityAnalyzer();
        var activityLoader = new Mock<IBitbucketPullRequestActivityLoader>(MockBehavior.Strict).Object;
        var snapshotMapper = CreateSnapshotMapper(parser);
        IPullRequestDetailsCacheService cacheService = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport.Object, analyzer, activityLoader, snapshotMapper, cacheService, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        var parser = new BitbucketJsonParser();
        var analyzer = new PullRequestActivityAnalyzer();
        var activityLoader = new Mock<IBitbucketPullRequestActivityLoader>(MockBehavior.Strict).Object;
        var snapshotMapper = CreateSnapshotMapper(parser);
        var cache = new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport.Object, analyzer, activityLoader, snapshotMapper, CreateCacheService(cache), options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync enriches open pull requests count when slug is present")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenSlugIsPresentLoadsOpenPullRequestsCount()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var repository = new Repository("Repo-1", null, null, "repo-1");

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestSummaryUrl("repo-1")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(new PullRequestPageSummaryDto(9));

        var client = CreateClient(transport.Object, new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object);

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        repository.OpenPullRequestsCount.Should().Be(9);
    }

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync keeps repository when pull requests count lookup fails")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenPullRequestsLookupFailsKeepsRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var repository = new Repository("Repo-1", null, null, "repo-1");

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestSummaryUrl("repo-1")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = CreateClient(transport.Object, new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object);

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        repository.OpenPullRequestsCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync uses cache hit without loading activity")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenCacheHitSkipsActivityRequest()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var pullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            commentCount: 1,
            taskCount: 2);
        var firstActivityPage = CreateActivityPage(
            """
            {
              "approval": {
                "user": { "uuid": "{reviewer-1}" },
                "date": "2026-02-24T10:00:00+00:00"
              }
            }
            """,
            """
            {
              "comment": {
                "user": { "uuid": "{current-user}" },
                "created_on": "2026-02-24T11:00:00+00:00"
              }
            }
            """);

        var firstTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = firstActivityPage
            },
            out var firstSendCounter,
            cts.Token);
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var firstClient = CreateClient(firstTransport.Object, cache);

        await firstClient.GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var secondTransport = CreateTransportForPullRequestListOnly(
            "repo-1",
            [pullRequest],
            out var secondSendCounter,
            cts.Token);
        var secondClient = CreateClient(secondTransport.Object, cache);

        // Act
        var details = await secondClient.GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        firstSendCounter.Count.Should().Be(2);
        secondSendCounter.Count.Should().Be(1);
        details.Should().HaveCount(1);
        details[0].PullRequestId.Should().Be(101);
        details[0].CommentsCount.Should().Be(1);
        details[0].HasCurrentUserDiscussion.Should().BeTrue();
        details[0].LastActivityOn.Should().Be(new DateTimeOffset(2026, 2, 24, 11, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync loads activity and saves cache on cache miss")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenCacheMissLoadsActivityAndStoresCache()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var pullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            commentCount: 1,
            taskCount: 0);
        var activityPage = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{current-user}" },
                "created_on": "2026-02-24T09:30:00+00:00"
              }
            }
            """);

        var transport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = activityPage
            },
            out var sendCounter,
            cts.Token);
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var client = CreateClient(transport.Object, cache);

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        sendCounter.Count.Should().Be(2);
        details.Should().HaveCount(1);
        details[0].CommentsCount.Should().Be(1);
        Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Should().ContainSingle();
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync invalidates cache when pull request fingerprint changes")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenPullRequestChangesReloadsActivity()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var originalPullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            commentCount: 1,
            taskCount: 0);
        var changedPullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 12, 0, 0, TimeSpan.Zero),
            commentCount: 2,
            taskCount: 1);
        var originalActivityPage = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{reviewer-1}" },
                "created_on": "2026-02-24T09:30:00+00:00"
              }
            }
            """);
        var changedActivityPage = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{reviewer-1}" },
                "created_on": "2026-02-24T09:30:00+00:00"
              }
            }
            """,
            """
            {
              "comment": {
                "user": { "uuid": "{current-user}" },
                "created_on": "2026-02-24T12:30:00+00:00"
              }
            }
            """);

        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var firstTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [originalPullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = originalActivityPage
            },
            out var firstSendCounter,
            cts.Token);
        var firstClient = CreateClient(firstTransport.Object, cache);
        await firstClient.GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var secondTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [changedPullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = changedActivityPage
            },
            out var secondSendCounter,
            cts.Token);
        var secondClient = CreateClient(secondTransport.Object, cache);

        // Act
        var details = await secondClient.GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        firstSendCounter.Count.Should().Be(2);
        secondSendCounter.Count.Should().Be(2);
        details.Should().HaveCount(1);
        details[0].CommentsCount.Should().Be(2);
        details[0].HasCurrentUserDiscussion.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync cleans cache when pull request disappears")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenPullRequestDisappearsRemovesItFromCache()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var pullRequest101 = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero));
        var pullRequest102 = CreatePullRequestDto(
            102,
            createdOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero));
        var activity101 = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{reviewer-1}" },
                "created_on": "2026-02-24T09:30:00+00:00"
              }
            }
            """);
        var activity102 = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{reviewer-2}" },
                "created_on": "2026-02-24T10:30:00+00:00"
              }
            }
            """);

        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);

        var firstTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest101, pullRequest102],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = activity101,
                [102] = activity102
            },
            out var firstSendCounter,
            cts.Token);
        await CreateClient(firstTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var secondTransport = CreateTransportForPullRequestListOnly(
            "repo-1",
            [pullRequest101],
            out var secondSendCounter,
            cts.Token);
        await CreateClient(secondTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var thirdTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest101, pullRequest102],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [102] = activity102
            },
            out var thirdSendCounter,
            cts.Token);

        // Act
        var details = await CreateClient(thirdTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        firstSendCounter.Count.Should().Be(3);
        secondSendCounter.Count.Should().Be(1);
        thirdSendCounter.Count.Should().Be(2);
        details.Should().HaveCount(2);
        details.Select(detail => detail.PullRequestId).Should().ContainInOrder(101, 102);
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync ignores corrupted cache and recalculates details")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenCacheIsCorruptedFallsBackToBitbucket()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var pullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero));
        var activityPage = CreateActivityPage(
            """
            {
              "comment": {
                "user": { "uuid": "{current-user}" },
                "created_on": "2026-02-24T10:00:00+00:00"
              }
            }
            """);

        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var firstTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = activityPage
            },
            out _,
            cts.Token);
        await CreateClient(firstTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var cacheFilePath = Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(cacheFilePath, "{ bad json", cts.Token);

        var secondTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [pullRequest],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = activityPage
            },
            out var secondSendCounter,
            cts.Token);

        // Act
        var details = await CreateClient(secondTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        secondSendCounter.Count.Should().Be(2);
        details.Should().HaveCount(1);
        details[0].HasCurrentUserDiscussion.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns empty list when lookup fails")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenLookupFailsReturnsEmptyList()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var sendCalls = 0;

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestsUrl("repo-1")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = CreateClient(transport.Object, new FilePullRequestDetailsCache(cacheDirectory.Path));

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(repository, new BitbucketId("{current-user}"), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        details.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync updates repository count and returns empty list when no open pull requests exist")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenNoPullRequestSnapshotsExistUpdatesRepositoryCount()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var cacheDirectory = new TemporaryDirectory();
        var repository = CreateRepository();
        var currentUserId = new BitbucketId("{current-user}");
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);

        var firstTransport = CreateTransportForPullRequestDetails(
            "repo-1",
            [CreatePullRequestDto(101)],
            new Dictionary<int, PullRequestActivityPageDto>
            {
                [101] = CreateActivityPage(
                    """
                    {
                      "comment": {
                        "user": { "uuid": "{reviewer-1}" },
                        "created_on": "2026-02-24T10:00:00+00:00"
                      }
                    }
                    """)
            },
            out _,
            cts.Token);
        await CreateClient(firstTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        var secondTransport = CreateTransportForPullRequestListOnly(
            "repo-1",
            [],
            out var secondSendCounter,
            cts.Token);

        // Act
        var details = await CreateClient(secondTransport.Object, cache)
            .GetOpenPullRequestDetailsAsync(repository, currentUserId, cts.Token);

        // Assert
        secondSendCounter.Count.Should().Be(1);
        repository.OpenPullRequestsCount.Should().Be(0);
        details.Should().BeEmpty();
        Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact(DisplayName = "GetMergedPullRequestsAsync loads merged pull requests since boundary")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsAsyncWhenMergedPullRequestsExistReturnsRecentRows()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var repository = CreateRepository();
        var mergedSince = new DateTimeOffset(2026, 2, 24, 0, 0, 0, TimeSpan.Zero);
        var recentPullRequest = CreatePullRequestDto(
            101,
            createdOn: new DateTimeOffset(2026, 2, 23, 10, 0, 0, TimeSpan.Zero),
            updatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            state: "MERGED",
            title: "Recent merge");
        var oldPullRequest = CreatePullRequestDto(
            102,
            updatedOn: new DateTimeOffset(2026, 2, 23, 23, 59, 0, TimeSpan.Zero),
            state: "MERGED",
            title: "Old merge");
        var sendCalls = 0;

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildMergedPullRequestsUrl("repo-1")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(new PullRequestPageDto([recentPullRequest, oldPullRequest], null));
        transport
            .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestActivityUrl("repo-1", 101)),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(CreateActivityPage(
                """
                {
                  "comment": {
                    "user": { "uuid": "{current-user}" },
                    "created_on": "2026-02-24T10:00:00+00:00"
                  }
                }
                """));

        var client = CreateClient(transport.Object, new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object);

        // Act
        var pullRequests = await client.GetMergedPullRequestsAsync(repository, mergedSince, new BitbucketId("{current-user}"), cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        pullRequests.Should().ContainSingle();
        pullRequests[0].PullRequestId.Should().Be(101);
        pullRequests[0].Title.Should().Be("Recent merge");
        pullRequests[0].MergedOn.Should().Be(new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero));
        pullRequests[0].HasCurrentUserDiscussion.Should().BeTrue();
        pullRequests[0].CommentsCount.Should().Be(1);
    }

    [Fact(DisplayName = "GetMergedPullRequestsAsync returns empty list when lookup fails")]
    [Trait("Category", "Unit")]
    public async Task GetMergedPullRequestsAsyncWhenLookupFailsReturnsEmptyList()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var repository = CreateRepository();
        var sendCalls = 0;

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildMergedPullRequestsUrl("repo-1")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = CreateClient(transport.Object, new Mock<IPullRequestDetailsCache>(MockBehavior.Strict).Object);

        // Act
        var pullRequests = await client.GetMergedPullRequestsAsync(
            repository,
            new DateTimeOffset(2026, 2, 24, 0, 0, 0, TimeSpan.Zero),
            new BitbucketId("{current-user}"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        pullRequests.Should().BeEmpty();
    }


    private static BitbucketPRApiClient CreateClient(IBitbucketTransport transport, IPullRequestDetailsCache cache) =>
        new(
            transport,
            new PullRequestActivityAnalyzer(),
            CreateActivityLoader(transport),
            CreateSnapshotMapper(new BitbucketJsonParser()),
            CreateCacheService(cache),
            Options.Create(CreateOptions()));

    private static PullRequestDetailsCacheService CreateCacheService(IPullRequestDetailsCache cache) => new(cache);

    private static BitbucketPullRequestActivityLoader CreateActivityLoader(IBitbucketTransport transport) =>
        new(transport, new BitbucketJsonParser(), Options.Create(CreateOptions()));

    private static PullRequestSnapshotMapper CreateSnapshotMapper(IBitbucketJsonParser parser) =>
        new(parser, new PullRequestFingerprintBuilder());

    private static Mock<IBitbucketTransport> CreateTransportForPullRequestDetails(
        string repositorySlug,
        IReadOnlyList<PullRequestDto> pullRequests,
        IReadOnlyDictionary<int, PullRequestActivityPageDto> activityPagesByPullRequestId,
        out RequestCounter sendCounter,
        CancellationToken cancellationToken)
    {
        var requestCounter = new RequestCounter();
        sendCounter = requestCounter;
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);

        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestsUrl(repositorySlug)),
                It.Is<CancellationToken>(token => token == cancellationToken)))
            .Callback(() => requestCounter.Count++)
            .ReturnsAsync(new PullRequestPageDto([.. pullRequests], null));

        foreach (var activityPage in activityPagesByPullRequestId)
        {
            var pullRequestId = activityPage.Key;
            transport
                .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestActivityUrl(repositorySlug, pullRequestId)),
                It.Is<CancellationToken>(token => token == cancellationToken)))
                .Callback(() => requestCounter.Count++)
                .ReturnsAsync(activityPage.Value);
        }

        return transport;
    }

    private static Mock<IBitbucketTransport> CreateTransportForPullRequestListOnly(
        string repositorySlug,
        IReadOnlyList<PullRequestDto> pullRequests,
        out RequestCounter sendCounter,
        CancellationToken cancellationToken)
    {
        var requestCounter = new RequestCounter();
        sendCounter = requestCounter;
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == BuildPullRequestsUrl(repositorySlug)),
                It.Is<CancellationToken>(token => token == cancellationToken)))
            .Callback(() => requestCounter.Count++)
            .ReturnsAsync(new PullRequestPageDto([.. pullRequests], null));

        return transport;
    }

    private static PullRequestDto CreatePullRequestDto(
        int id,
        DateTimeOffset? createdOn = null,
        DateTimeOffset? updatedOn = null,
        string? state = "OPEN",
        string? sourceCommitHash = "abcdef123456",
        int? commentCount = 0,
        int? taskCount = 0,
        string? description = "Detailed description",
        string? summary = null,
        string? title = null,
        string? authorUuid = "{author-1}",
        string? authorDisplayName = "Author 1",
        ICollection<PullRequestParticipantDto>? participants = null)
    {
        return new PullRequestDto(
            Id: id,
            Title: title ?? $"Feature {id}",
            CreatedOn: createdOn ?? new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
            UpdatedOn: updatedOn ?? new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
            State: state,
            Description: description,
            Summary: summary is null ? null : new PullRequestSummaryDto(summary),
            Author: new PullRequestAuthorDto(authorUuid, authorDisplayName),
            Source: new PullRequestSourceDto(new PullRequestCommitDto(sourceCommitHash)),
            CommentCount: commentCount,
            TaskCount: taskCount,
            Participants: participants ?? CreateParticipants());
    }

    private static ICollection<PullRequestParticipantDto> CreateParticipants() =>
    [
        new PullRequestParticipantDto(
            User: new PullRequestAuthorDto("{reviewer-1}", "Reviewer 1"),
            State: "changes_requested"),
        new PullRequestParticipantDto(
            User: new PullRequestAuthorDto("{current-user}", "Current User"),
            Approved: true)
    ];

    private static PullRequestActivityPageDto CreateActivityPage(params string[] activityJsonPayloads)
    {
        return new PullRequestActivityPageDto(
        [
            .. activityJsonPayloads.Select(static json => new PullRequestActivityDto
            {
                Properties = ParseActivityJson(json)
            })
        ],
            null);
    }

    private static Dictionary<string, JsonElement> ParseActivityJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
    }

    private static Repository CreateRepository() =>
        new(
            "Repo-1",
            new DateTimeOffset(2023, 1, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            "repo-1");

    private static string BuildPullRequestSummaryUrl(string repositorySlug) =>
        $"repositories/workspace/{repositorySlug}/pullrequests?state=OPEN&pagelen=1&fields=size";

    private static string BuildPullRequestsUrl(string repositorySlug)
    {
        var fields = Uri.EscapeDataString(
            "values.id," +
            "values.title," +
            "values.created_on," +
            "values.updated_on," +
            "values.state," +
            "values.description," +
            "values.summary.raw," +
            "values.author.uuid," +
            "values.author.display_name," +
            "values.source.commit.hash," +
            "values.comment_count," +
            "values.task_count," +
            "values.participants.user.uuid," +
            "values.participants.state," +
            "values.participants.approved," +
            "next");

        return $"repositories/workspace/{repositorySlug}/pullrequests?state=OPEN&pagelen=25&fields={fields}";
    }

    private static string BuildMergedPullRequestsUrl(string repositorySlug)
    {
        var fields = Uri.EscapeDataString(
            "values.id," +
            "values.title," +
            "values.created_on," +
            "values.updated_on," +
            "values.description," +
            "values.summary.raw," +
            "values.author.uuid," +
            "values.author.display_name," +
            "values.participants.user.uuid," +
            "values.participants.state," +
            "values.participants.approved," +
            "next");

        return $"repositories/workspace/{repositorySlug}/pullrequests?state=MERGED&pagelen=25&sort=-updated_on&fields={fields}";
    }

    private static string BuildPullRequestActivityUrl(string repositorySlug, int pullRequestId)
    {
        var fields = Uri.EscapeDataString(
            "values.actor.uuid," +
            "values.user.uuid," +
            "values.date," +
            "values.created_on," +
            "values.updated_on," +
            "values.comment," +
            "values.approval," +
            "values.request_changes," +
            "values.changes_requested," +
            "values.update," +
            "next");

        return $"repositories/workspace/{repositorySlug}/pullrequests/{pullRequestId}/activity?pagelen=25&fields={fields}";
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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BBRepoList.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class RequestCounter
    {
        public int Count { get; set; }
    }
}
