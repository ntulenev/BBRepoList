using System.Globalization;

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
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new ConsoleApp(api, pdfReportRenderer, repoService, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when pdf report renderer is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPdfReportRendererIsNullThrowsArgumentNullException()
    {
        // Arrange
        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict).Object;
        IPdfReportRenderer pdfReportRenderer = null!;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new ConsoleApp(api, pdfReportRenderer, repoService, options);

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
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        IRepoService repoService = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new ConsoleApp(api, pdfReportRenderer, repoService, options);

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
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new ConsoleApp(api, pdfReportRenderer, repoService, options);

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
        var pdfCalls = 0;
        var repo1CreatedOn = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var repo2CreatedOn = new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var repo1UpdatedOn = new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var repo2UpdatedOn = new DateTimeOffset(2025, 1, 20, 0, 0, 0, TimeSpan.Zero);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .Callback(() => authCalls++)
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()))
            .Callback(() => pdfCalls++);

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
                new Repository("Repo-1", repo1CreatedOn, repo1UpdatedOn, 5),
                new Repository("Repo-2", repo2CreatedOn, repo2UpdatedOn, 2)
            ]);

        var options = Options.Create(CreateOptions());
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        authCalls.Should().Be(1);
        repoCalls.Should().Be(1);
        pdfCalls.Should().Be(1);
        output.Should().Contain("Created on");
        output.Should().Contain("Last updated");
        output.Should().Contain("Open pull requests");
        output.Should().Contain("Repositories with open pull requests");
        output.Should().Contain("2025-01-10");
        output.Should().Contain("2024-12-01");
        output.Should().Contain("2025-02-15");
        output.Should().Contain("2025-01-20");
        output.Should().Contain("5");
        output.Should().Contain("2");
    }

    [Fact(DisplayName = "RunAsync uses configured repository search mode from options")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenConfiguredSearchModeIsStartWithPassesStartWithFilterToService()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var repoCalls = 0;

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo", RepositorySearchMode.StartWith),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .Callback(() => repoCalls++)
            .ReturnsAsync([]);

        var options = Options.Create(CreateOptions(repositorySearchMode: RepositorySearchMode.StartWith));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        repoCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync does not render open pull requests table when there are no open pull requests")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenNoRepositoriesHaveOpenPullRequestsDoesNotRenderOpenPullRequestsTable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Repo-1", null, null, 0),
                new Repository("Repo-2")
            ]);

        var options = Options.Create(CreateOptions());
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        output.Should().NotContain("Repositories with open pull requests");
    }

    [Fact(DisplayName = "RunAsync renders open PR details report when enabled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenPrDetailsAreEnabledRendersPrDetailsReport()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var detailsCalls = 0;
        var repoCreatedOn = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var prOpenedOn = new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));
        api.Setup(a => a.GetOpenPullRequestDetailsAsync(
                It.IsAny<Repository>(),
                new BitbucketId("{uuid}"),
                cts.Token))
            .Callback(() => detailsCalls++)
            .ReturnsAsync(
            [
                new PullRequestDetail(
                    "Repo-1",
                    "repo-1",
                    repoCreatedOn,
                    101,
                    "Feature work",
                    prOpenedOn,
                    "{author}",
                    prOpenedOn.AddHours(5),
                    true)
            ]);

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Repo-1", repoCreatedOn, repoCreatedOn.AddYears(2), 1, "repo-1")
            ]);

        var options = Options.Create(CreateOptions(prDetailsEnabled: true, ttfrThresholdHours: 4));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        detailsCalls.Should().Be(1);
        output.Should().Contain("Open PR details");
        output.Should().Contain("TTFR");
        output.Should().NotContain("Alert");
        output.Should().Contain("Yes");
        output.Should().Contain("Feature");
    }

    [Fact(DisplayName = "RunAsync shows TTFR alert when first non-author response is missing over threshold")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenTtfrIsMissingOverThresholdShowsAlert()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var repoCreatedOn = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var prOpenedOn = DateTimeOffset.UtcNow.AddHours(-5);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));
        api.Setup(a => a.GetOpenPullRequestDetailsAsync(
                It.IsAny<Repository>(),
                new BitbucketId("{uuid}"),
                cts.Token))
            .ReturnsAsync(
            [
                new PullRequestDetail(
                    "Repo-1",
                    "repo-1",
                    repoCreatedOn,
                    101,
                    "No response yet",
                    prOpenedOn,
                    "{author}",
                    firstNonAuthorActivityOn: null,
                    hasCurrentUserDiscussion: false)
            ]);

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Repo-1", repoCreatedOn, repoCreatedOn.AddYears(2), 1, "repo-1")
            ]);

        var options = Options.Create(CreateOptions(prDetailsEnabled: true, ttfrThresholdHours: 4));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        output.Should().Contain("ALERT");
    }

    [Fact(DisplayName = "RunAsync renders abandoned repositories table when inactivity is above threshold")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenInactivityIsAboveThresholdRendersAbandonedRepositoriesTable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var now = DateTimeOffset.UtcNow;
        var oldCreatedOn = now.AddMonths(-30);
        var oldLastActivityOn = now.AddMonths(-16);
        var oldInactiveMonths = CalculateFullMonthsBetween(oldLastActivityOn, now);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Old-Repo", oldCreatedOn, oldLastActivityOn, 0),
                new Repository("Fresh-Repo", now.AddMonths(-2), now.AddMonths(-1), 0)
            ]);

        var options = Options.Create(CreateOptions(abandonedMonthsThreshold: 12));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        output.Should().Contain("Abandoned repositories");
        output.Should().Contain("Old-Repo");
        output.Should().Contain(oldCreatedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        output.Should().Contain(oldLastActivityOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        output.Should().Contain(oldInactiveMonths.ToString(CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "RunAsync does not render abandoned repositories table when inactivity is below threshold")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenInactivityIsBelowThresholdDoesNotRenderAbandonedRepositoriesTable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var now = DateTimeOffset.UtcNow;

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Recent-Repo", now.AddMonths(-5), now.AddMonths(-3), 0),
                new Repository("New-Repo", now.AddMonths(-1), now.AddMonths(-1), 0)
            ]);

        var options = Options.Create(CreateOptions(abandonedMonthsThreshold: 12));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        output.Should().NotContain("Abandoned repositories");
    }

    [Fact(DisplayName = "RunAsync does not render abandoned repositories table when loading is disabled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAbandonedRepositoriesLoadingIsDisabledDoesNotRenderAbandonedRepositoriesTable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var now = DateTimeOffset.UtcNow;
        var oldCreatedOn = now.AddMonths(-30);
        var oldLastActivityOn = now.AddMonths(-16);

        var api = new Mock<IBitbucketApiClient>(MockBehavior.Strict);
        api.Setup(a => a.AuthSelfCheckAsync(cts.Token))
            .ReturnsAsync(new BitbucketUser(new BitbucketId("{uuid}"), new UserName("Jane Doe")));

        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(r => r.RenderReport(It.IsAny<RepositoryPdfReportData>()));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(s => s.GetRepositoriesAsync(
                new FilterPattern("Repo"),
                It.IsAny<IProgress<RepoLoadProgress>>(),
                cts.Token))
            .ReturnsAsync(
            [
                new Repository("Old-Repo", oldCreatedOn, oldLastActivityOn, 0),
                new Repository("Fresh-Repo", now.AddMonths(-2), now.AddMonths(-1), 0)
            ]);

        var options = Options.Create(CreateOptions(
            abandonedMonthsThreshold: 12,
            loadAbandonedRepositoriesStatistics: false));
        var app = new ConsoleApp(api.Object, pdfReportRenderer.Object, repoService.Object, options);

        var output = await RunWithTestConsoleAsync(async console =>
        {
            console.Input.PushTextWithEnter("Repo");

            // Act
            await app.RunAsync(cts.Token);
        });

        // Assert
        output.Should().NotContain("Abandoned repositories");
    }

    private static BitbucketOptions CreateOptions(
        int abandonedMonthsThreshold = 120,
        bool loadAbandonedRepositoriesStatistics = true,
        bool prDetailsEnabled = false,
        int ttfrThresholdHours = 4,
        RepositorySearchMode repositorySearchMode = RepositorySearchMode.Contains)
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            Pdf = new PdfOptions
            {
                Enabled = true,
                OutputPath = "bbrepolist-report.pdf"
            },
            PullRequestDetails = new PullRequestDetailsOptions
            {
                IsEnabled = prDetailsEnabled,
                TtfrThresholdHours = ttfrThresholdHours
            },
            AbandonedMonthsThreshold = abandonedMonthsThreshold,
            LoadAbandonedRepositoriesStatistics = loadAbandonedRepositoriesStatistics,
            RepositorySearchMode = repositorySearchMode
        };
    }

    private static int CalculateFullMonthsBetween(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from)
        {
            return 0;
        }

        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
        if (to.Day < from.Day)
        {
            months--;
        }

        return Math.Max(months, 0);
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
