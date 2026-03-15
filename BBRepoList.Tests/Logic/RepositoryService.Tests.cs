using System.Runtime.CompilerServices;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Models;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.Logic;

public sealed class RepositoryServiceTests
{
    [Fact(DisplayName = "Constructor throws when api is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketRepoApiClient api = null!;
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryService(api, prApi, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when PR api is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPrApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict).Object;
        IBitbucketPRApiClient prApi = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryService(api, prApi, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict).Object;
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new RepositoryService(api, prApi, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetRepositoriesAsync returns all repositories when no filter is provided")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenNoFilterReturnsAllRepositories()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var enrichCalls = 0;
        var pages = new List<Repository[]>
        {
             new []
             {
                 new Repository("Repo-1"),
                 new Repository("Repo-2"),
                 new Repository("Repo-3")
             }
        };

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));

        var expectedRepositories = pages.SelectMany(static page => page).ToHashSet();
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(m => m.PopulateOpenPullRequestCountAsync(
                It.Is<Repository>(repository => expectedRepositories.Contains(repository)),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback<Repository, CancellationToken>((repository, _) =>
            {
                enrichCalls++;
                repository.UpdateOpenPullRequestsCount(1);
            })
            .Returns(Task.CompletedTask);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object, prApi.Object, Options.Create(CreateOptions(loadOpenPullRequestsStatistics: true)));

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern(null), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(3);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2", "Repo-3");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 1);

        apiCalls.Should().Be(1);
        enrichCalls.Should().Be(3);
        progressReports.Should().HaveCount(7);
        progressReports[0].Seen.Should().Be(1);
        progressReports[0].Matched.Should().Be(1);
        progressReports[0].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports[1].Seen.Should().Be(2);
        progressReports[1].Matched.Should().Be(2);
        progressReports[1].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports[2].Seen.Should().Be(3);
        progressReports[2].Matched.Should().Be(3);
        progressReports[2].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports.Should().Contain(report =>
            report.IsLoadingPullRequestStatistics
            && report.PullRequestStatisticsLoaded == 0
            && report.PullRequestStatisticsTotal == 3);
        progressReports.Should().Contain(report =>
            report.IsLoadingPullRequestStatistics
            && report.PullRequestStatisticsLoaded == 3
            && report.PullRequestStatisticsTotal == 3);
    }

    [Fact(DisplayName = "GetRepositoriesAsync filters repositories when search phrase is provided")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedReturnsMatches()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var enrichCalls = 0;
        var pages = new List<Repository[]>
        {
            new[]
            {
                new Repository("Repo-1"),
                new Repository("App-One")
            },
            new[]
            {
                new Repository("app-two"),
                new Repository("Other")
            }
        };

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));

        var expectedRepositories = pages
            .SelectMany(static page => page)
            .Where(static repository => repository.Name.Contains("app", StringComparison.OrdinalIgnoreCase))
            .ToHashSet();
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(m => m.PopulateOpenPullRequestCountAsync(
                It.Is<Repository>(repository => expectedRepositories.Contains(repository)),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback<Repository, CancellationToken>((repository, _) =>
            {
                enrichCalls++;
                repository.UpdateOpenPullRequestsCount(2);
            })
            .Returns(Task.CompletedTask);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object, prApi.Object, Options.Create(CreateOptions(loadOpenPullRequestsStatistics: true)));

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern("app"), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(2);
        repositories.Select(r => r.Name).Should().ContainInOrder("App-One", "app-two");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 2);

        apiCalls.Should().Be(1);
        enrichCalls.Should().Be(2);
        progressReports.Should().HaveCount(7);
        progressReports[0].Seen.Should().Be(1);
        progressReports[0].Matched.Should().Be(0);
        progressReports[0].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports[1].Seen.Should().Be(2);
        progressReports[1].Matched.Should().Be(1);
        progressReports[1].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports[2].Seen.Should().Be(3);
        progressReports[2].Matched.Should().Be(2);
        progressReports[2].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports[3].Seen.Should().Be(4);
        progressReports[3].Matched.Should().Be(2);
        progressReports[3].IsLoadingPullRequestStatistics.Should().BeFalse();
        progressReports.Should().Contain(report =>
            report.IsLoadingPullRequestStatistics
            && report.PullRequestStatisticsLoaded == 0
            && report.PullRequestStatisticsTotal == 2);
        progressReports.Should().Contain(report =>
            report.IsLoadingPullRequestStatistics
            && report.PullRequestStatisticsLoaded == 2
            && report.PullRequestStatisticsTotal == 2);

    }

    [Fact(DisplayName = "GetRepositoriesAsync filters repositories and return none when search phrase is not fit")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedButNotFitReturnsNoMatches()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var enrichCalls = 0;
        var pages = new List<Repository[]>
        {
            new[]{new Repository("Repo-1"), new Repository("App-One") },
            new[]{new Repository("app-two"), new Repository("Other") }
        };

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));

        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object, prApi.Object, Options.Create(CreateOptions(loadOpenPullRequestsStatistics: true)));

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern("XXX"), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(0);

        apiCalls.Should().Be(1);
        enrichCalls.Should().Be(0);
        progressReports.Should().HaveCount(4);
        progressReports[0].Seen.Should().Be(1);
        progressReports[0].Matched.Should().Be(0);
        progressReports[1].Seen.Should().Be(2);
        progressReports[1].Matched.Should().Be(0);
        progressReports[2].Seen.Should().Be(3);
        progressReports[2].Matched.Should().Be(0);
        progressReports[3].Seen.Should().Be(4);
        progressReports[3].Matched.Should().Be(0);

    }

    [Fact(DisplayName = "GetRepositoriesAsync does not load open pull requests when disabled in options")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenLoadOpenPullRequestsStatisticsIsDisabledDoesNotEnrichRepositories()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var pages = new List<Repository[]>
        {
            new[]
            {
                new Repository("Repo-1"),
                new Repository("Repo-2")
            }
        };

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object, prApi.Object, Options.Create(CreateOptions(loadOpenPullRequestsStatistics: false)));

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern(null), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(2);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 0);

        apiCalls.Should().Be(1);
        progressReports.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetRepositoriesAsync loads open pull requests with configured concurrency threshold")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenThresholdIsConfiguredRespectsConfiguredConcurrency()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var pages = new List<Repository[]>
        {
            new[]
            {
                new Repository("Repo-1"),
                new Repository("Repo-2"),
                new Repository("Repo-3"),
                new Repository("Repo-4")
            }
        };
        var inFlight = 0;
        var maxInFlight = 0;

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));

        var expectedRepositories = pages.SelectMany(static page => page).ToHashSet();
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(m => m.PopulateOpenPullRequestCountAsync(
                It.Is<Repository>(repository => expectedRepositories.Contains(repository)),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Returns<Repository, CancellationToken>(async (repository, token) =>
            {
                var currentInFlight = Interlocked.Increment(ref inFlight);
                UpdateMaxInFlight(ref maxInFlight, currentInFlight);

                try
                {
                    await Task.Delay(40, token);
                    repository.UpdateOpenPullRequestsCount(3);
                }
                finally
                {
                    _ = Interlocked.Decrement(ref inFlight);
                }
            });

        var service = new RepositoryService(
            api.Object,
            prApi.Object,
            Options.Create(CreateOptions(
                loadOpenPullRequestsStatistics: true,
                openPullRequestsLoadThreshold: 2)));

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern(null), progress: null, cts.Token);

        // Assert
        repositories.Should().HaveCount(4);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2", "Repo-3", "Repo-4");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 3);
        maxInFlight.Should().BeLessThanOrEqualTo(2);
        maxInFlight.Should().BeGreaterThan(1);
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync loads details for eligible repositories and reports progress")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenEnabledLoadsAndSortsDetails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var currentUserId = new BitbucketId("{current-user}");
        var prDetailCalls = 0;

        var repo1 = new Repository("Repo-1", null, null, "repo-1");
        repo1.UpdateOpenPullRequestsCount(1);
        var repo2 = new Repository("Repo-2", null, null, "repo-2");
        repo2.UpdateOpenPullRequestsCount(2);
        var repoWithoutOpenPrs = new Repository("Repo-3", null, null, "repo-3");
        repoWithoutOpenPrs.UpdateOpenPullRequestsCount(0);

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        prApi.Setup(m => m.GetOpenPullRequestDetailsAsync(repo1, currentUserId, It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => prDetailCalls++)
            .ReturnsAsync(
            [
                new PullRequestDetail(
                    repo1,
                    101,
                    "PR older",
                    new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
                    new BitbucketId("{author-1}"),
                    "Author 1",
                    null,
                    null,
                    false)
            ]);
        prApi.Setup(m => m.GetOpenPullRequestDetailsAsync(repo2, currentUserId, It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => prDetailCalls++)
            .ReturnsAsync(
            [
                new PullRequestDetail(
                    repo2,
                    102,
                    "PR newer",
                    new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero),
                    new BitbucketId("{author-2}"),
                    "Author 2",
                    null,
                    null,
                    true)
            ]);

        var progressReports = new List<PullRequestDetailsLoadProgress>();
        var progress = new Progress<PullRequestDetailsLoadProgress>(progressReports.Add);

        var service = new RepositoryService(
            api.Object,
            prApi.Object,
            Options.Create(CreateOptions(prDetailsEnabled: true, prDetailsLoadThreshold: 2)));

        // Act
        var details = await service.GetOpenPullRequestDetailsAsync(
            [repo1, repo2, repoWithoutOpenPrs],
            currentUserId,
            progress,
            cts.Token);

        // Assert
        prDetailCalls.Should().Be(2);
        details.Should().HaveCount(2);
        details.Select(d => d.RepositoryName).Should().ContainInOrder("Repo-2", "Repo-1");
        progressReports.Should().Contain(report => report.LoadedRepositories == 0 && report.TotalRepositories == 2);
        progressReports.Should().Contain(report => report.LoadedRepositories == 2 && report.TotalRepositories == 2);
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns empty list when disabled in options")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenDisabledReturnsEmpty()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var currentUserId = new BitbucketId("{current-user}");
        var repository = new Repository("Repo-1", null, null, "repo-1");
        repository.UpdateOpenPullRequestsCount(1);

        var api = new Mock<IBitbucketRepoApiClient>(MockBehavior.Strict);
        var prApi = new Mock<IBitbucketPRApiClient>(MockBehavior.Strict);
        var service = new RepositoryService(
            api.Object,
            prApi.Object,
            Options.Create(CreateOptions(prDetailsEnabled: false)));

        // Act
        var details = await service.GetOpenPullRequestDetailsAsync([repository], currentUserId, progress: null, cts.Token);

        // Assert
        details.Should().BeEmpty();
    }

    private static async IAsyncEnumerable<Repository> StreamRepositories(
        IReadOnlyList<Repository[]> pages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var page in pages)
        {
            foreach (var repository in page)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return repository;
                await Task.Yield();
            }
        }
    }

    private static BitbucketOptions CreateOptions(
        bool loadOpenPullRequestsStatistics = true,
        int openPullRequestsLoadThreshold = 8,
        bool prDetailsEnabled = false,
        int prDetailsLoadThreshold = 8)
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            PullRequestDetails = new PullRequestDetailsOptions
            {
                IsEnabled = prDetailsEnabled,
                LoadThreshold = prDetailsLoadThreshold
            },
            AbandonedMonthsThreshold = 12,
            LoadOpenPullRequestsStatistics = loadOpenPullRequestsStatistics,
            OpenPullRequestsLoadThreshold = openPullRequestsLoadThreshold
        };
    }

    private static void UpdateMaxInFlight(ref int maxInFlight, int currentInFlight)
    {
        while (true)
        {
            var snapshot = maxInFlight;
            if (currentInFlight <= snapshot)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref maxInFlight, currentInFlight, snapshot) == snapshot)
            {
                return;
            }
        }
    }
}


