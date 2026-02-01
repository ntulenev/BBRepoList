using FluentAssertions;
using Moq;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Tests.Logic;

public sealed class RepositoryServiceTests
{
    [Fact(DisplayName = "Constructor throws when api is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketApiClient api = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryService(api, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsIsNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new RepositoryService(api, options);

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
        var urls = new List<Uri>();
        var pages = new Queue<RepoPage>();

        pages.Enqueue(new RepoPage(
            [new("Repo-1"), new("Repo-2")],
            new Uri("next", UriKind.Relative)));

        pages.Enqueue(new RepoPage(
            [new("Repo-3")],
            null));

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        var firstUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var nextUrl = new Uri("next", UriKind.Relative);
        api.Setup(m => m.GetRepositoriesPageAsync(firstUrl, cts.Token))
            .Callback<Uri, CancellationToken>((url, _) =>
            {
                apiCalls++;
                urls.Add(url);
            })
            .ReturnsAsync(pages.Dequeue);
        api.Setup(m => m.GetRepositoriesPageAsync(nextUrl, cts.Token))
            .Callback<Uri, CancellationToken>((url, _) =>
            {
                apiCalls++;
                urls.Add(url);
            })
            .ReturnsAsync(pages.Dequeue);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);
        var options = Options.Create(CreateOptions());

        var service = new RepositoryService(api.Object, options);

        // Act
        var repositories = await service.GetRepositoriesAsync(null, progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(3);
        repositories.Select(r => r.Name).Should().ContainInOrder("Repo-1", "Repo-2", "Repo-3");

        apiCalls.Should().Be(2);
        urls.Should().HaveCount(2);
        urls[0].Should().Be(firstUrl);
        urls[1].Should().Be(nextUrl);

        progressReports.Should().HaveCount(2);
        progressReports[0].Pages.Should().Be(1);
        progressReports[0].Seen.Should().Be(2);
        progressReports[0].Matched.Should().Be(2);
        progressReports[1].Pages.Should().Be(2);
        progressReports[1].Seen.Should().Be(3);
        progressReports[1].Matched.Should().Be(3);

        apiCalls.Should().Be(2);
    }

    [Fact(DisplayName = "GetRepositoriesAsync filters repositories when search phrase is provided")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedReturnsMatches()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var pages = new Queue<RepoPage>();

        pages.Enqueue(new RepoPage(
            [new Repository("Repo-1"), new Repository("App-One")],
            new Uri("next", UriKind.Relative)));

        pages.Enqueue(new RepoPage(
            [new Repository("app-two"), new Repository("Other")],
            null));

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        var firstUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var nextUrl = new Uri("next", UriKind.Relative);
        api.Setup(m => m.GetRepositoriesPageAsync(firstUrl, cts.Token))
            .Callback(() => apiCalls++)
            .ReturnsAsync(pages.Dequeue);
        api.Setup(m => m.GetRepositoriesPageAsync(nextUrl, cts.Token))
            .Callback(() => apiCalls++)
            .ReturnsAsync(pages.Dequeue);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);
        var options = Options.Create(CreateOptions());

        var service = new RepositoryService(api.Object, options);

        // Act
        var repositories = await service.GetRepositoriesAsync("app", progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(2);
        repositories.Select(r => r.Name).Should().ContainInOrder("App-One", "app-two");

        apiCalls.Should().Be(2);
        progressReports.Should().HaveCount(2);
        progressReports[0].Pages.Should().Be(1);
        progressReports[0].Seen.Should().Be(2);
        progressReports[0].Matched.Should().Be(1);
        progressReports[1].Pages.Should().Be(2);
        progressReports[1].Seen.Should().Be(4);
        progressReports[1].Matched.Should().Be(2);

    }

    [Fact(DisplayName = "GetRepositoriesAsync filters repositories and return none when search phrase is not fit")]
    [Trait("Category", "Unit")]
    public async Task GetRepositoriesAsyncWhenFilterIsProvidedButNotFitReturnsNoMatches()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var apiCalls = 0;
        var pages = new Queue<RepoPage>();

        pages.Enqueue(new RepoPage(
            [new Repository("Repo-1"), new Repository("App-One")],
            new Uri("next", UriKind.Relative)));

        pages.Enqueue(new RepoPage(
            [new Repository("app-two"), new Repository("Other")],
            null));

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        var firstUrl = new Uri("repositories/workspace?pagelen=25", UriKind.Relative);
        var nextUrl = new Uri("next", UriKind.Relative);
        api.Setup(m => m.GetRepositoriesPageAsync(firstUrl, cts.Token))
            .Callback(() => apiCalls++)
            .ReturnsAsync(pages.Dequeue);
        api.Setup(m => m.GetRepositoriesPageAsync(nextUrl, cts.Token))
            .Callback(() => apiCalls++)
            .ReturnsAsync(pages.Dequeue);

        var progressReports = new List<RepoLoadProgress>();
        var progress = new Progress<RepoLoadProgress>(progressReports.Add);
        var options = Options.Create(CreateOptions());

        var service = new RepositoryService(api.Object, options);

        // Act
        var repositories = await service.GetRepositoriesAsync("XXX", progress, cts.Token);

        // Assert
        repositories.Should().HaveCount(0);

        apiCalls.Should().Be(2);
        progressReports.Should().HaveCount(2);
        progressReports[0].Pages.Should().Be(1);
        progressReports[0].Seen.Should().Be(2);
        progressReports[0].Matched.Should().Be(0);
        progressReports[1].Pages.Should().Be(2);
        progressReports[1].Seen.Should().Be(4);
        progressReports[1].Matched.Should().Be(0);

    }

    private static BitbucketOptions CreateOptions()
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25
        };
    }
}
