using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Presentation;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using Spectre.Console;
using Spectre.Console.Testing;

namespace BBRepoList.Tests.Presentation;

public sealed class ConsoleAppTests
{
    [Fact(DisplayName = "Constructor throws when api client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketApiClient api = null!;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new ConsoleApp(api, repoService, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when repo service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepoServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict).Object;
        IRepoService repoService = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new ConsoleApp(api, repoService, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict).Object;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new ConsoleApp(api, repoService, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RunAsync validates and runs with no errors")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncValidatesAndRunsWithNoErrors()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var authCalls = 0;
        var repoCalls = 0;
        var repo1CreatedOn = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var repo2CreatedOn = new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .Callback(() => authCalls++)
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .Callback<FilterPattern, IProgress<RepoLoadProgress>?, CancellationToken>((_, progress, __) =>
            {
                repoCalls++;
                progress?.Report(new RepoLoadProgress(2, 2));
            })
            .ReturnsAsync(
            [
                new Repository("Repo-1", repo1CreatedOn),
                new Repository("Repo-2", repo2CreatedOn)
            ]);

        var options = Options.Create(CreateOptions());
        var app = new ConsoleApp(api.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        authCalls.Should().Be(1);
        repoCalls.Should().Be(1);
        output.Should().Contain("Created on");
        output.Should().Contain("2025-01-10");
        output.Should().Contain("2024-12-01");
    }

    private static BitbucketOptions CreateOptions()
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };
    }

    private static async Task<string> RunWithTestConsoleAsync(Func<TestConsole, Task> action)
    {
        var original = AnsiConsole.Console;
        var console = new TestConsole();
        AnsiConsole.Console = console;

        try
        {
            await action(console);
            return console.Output;
        }
        finally
        {
            AnsiConsole.Console = original;
        }
    }
}
