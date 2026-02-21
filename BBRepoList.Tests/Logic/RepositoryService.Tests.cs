using System.Runtime.CompilerServices;

using BBRepoList.Abstractions;
using BBRepoList.Logic;
using BBRepoList.Models;

using FluentAssertions;

using Moq;

namespace BBRepoList.Tests.Logic;

public sealed class RepositoryServiceTests
{
    [Fact(DisplayName = "Constructor throws when api is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketApiClient api = null!;

        // Act
        Action act = () => _ = new RepositoryService(api);

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

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));
        api.Setup(m => m.PopulateOpenPullRequestCountAsync(It.IsAny<Repository>(), cts.Token))
            .Callback<Repository, CancellationToken>((_, _) => enrichCalls++)
            .ReturnsAsync((Repository repository, CancellationToken _) =>
                new Repository(repository.Name, repository.CreatedOn, repository.LastUpdatedOn, 1, repository.Slug));

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object);

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern(null), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(3);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2", "Repo-3");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 1);

        apiCalls.Should().Be(1);
        enrichCalls.Should().Be(3);
        progressReports.Should().HaveCount(3);
        progressReports[0].Seen.Should().Be(1);
        progressReports[0].Matched.Should().Be(1);
        progressReports[1].Seen.Should().Be(2);
        progressReports[1].Matched.Should().Be(2);
        progressReports[2].Seen.Should().Be(3);
        progressReports[2].Matched.Should().Be(3);
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

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));
        api.Setup(m => m.PopulateOpenPullRequestCountAsync(It.IsAny<Repository>(), cts.Token))
            .Callback<Repository, CancellationToken>((_, _) => enrichCalls++)
            .ReturnsAsync((Repository repository, CancellationToken _) =>
                new Repository(repository.Name, repository.CreatedOn, repository.LastUpdatedOn, 2, repository.Slug));

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object);

        // Act
        var repositories = await service.GetRepositoriesAsync(new FilterPattern("app"), progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(2);
        repositories.Select(r => r.Name).Should().ContainInOrder("App-One", "app-two");
        repositories.Select(r => r.OpenPullRequestsCount).Should().OnlyContain(count => count == 2);

        apiCalls.Should().Be(1);
        enrichCalls.Should().Be(2);
        progressReports.Should().HaveCount(4);
        progressReports[0].Seen.Should().Be(1);
        progressReports[0].Matched.Should().Be(0);
        progressReports[1].Seen.Should().Be(2);
        progressReports[1].Matched.Should().Be(1);
        progressReports[2].Seen.Should().Be(3);
        progressReports[2].Matched.Should().Be(2);
        progressReports[3].Seen.Should().Be(4);
        progressReports[3].Matched.Should().Be(2);

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

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(m => m.GetRepositoriesAsync(cts.Token))
            .Callback(() => apiCalls++)
            .Returns<CancellationToken>(token => StreamRepositories(pages, token));
        api.Setup(m => m.PopulateOpenPullRequestCountAsync(It.IsAny<Repository>(), cts.Token))
            .Callback<Repository, CancellationToken>((_, _) => enrichCalls++)
            .ReturnsAsync((Repository repository, CancellationToken _) => repository);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);

        var service = new RepositoryService(api.Object);

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
}
