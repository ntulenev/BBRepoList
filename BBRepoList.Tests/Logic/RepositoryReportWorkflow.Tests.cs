using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Models;
using BBRepoList.Presentation;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.Logic;

public sealed class RepositoryReportWorkflowTests
{
    [Fact(DisplayName = "Constructor throws when repo service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepoServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IRepoService repoService = null!;
        var reportDataFactory = new Mock<IRepositoryReportDataFactory>(MockBehavior.Strict).Object;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryReportWorkflow(
            repoService,
            reportDataFactory,
            htmlReportRenderer,
            pdfReportRenderer,
            options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when report data factory is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenReportDataFactoryIsNullThrowsArgumentNullException()
    {
        // Arrange
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        IRepositoryReportDataFactory reportDataFactory = null!;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryReportWorkflow(
            repoService,
            reportDataFactory,
            htmlReportRenderer,
            pdfReportRenderer,
            options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when HTML report renderer is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHtmlReportRendererIsNullThrowsArgumentNullException()
    {
        // Arrange
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var reportDataFactory = new Mock<IRepositoryReportDataFactory>(MockBehavior.Strict).Object;
        IHtmlReportRenderer htmlReportRenderer = null!;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryReportWorkflow(
            repoService,
            reportDataFactory,
            htmlReportRenderer,
            pdfReportRenderer,
            options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when PDF report renderer is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPdfReportRendererIsNullThrowsArgumentNullException()
    {
        // Arrange
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var reportDataFactory = new Mock<IRepositoryReportDataFactory>(MockBehavior.Strict).Object;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        IPdfReportRenderer pdfReportRenderer = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new RepositoryReportWorkflow(
            repoService,
            reportDataFactory,
            htmlReportRenderer,
            pdfReportRenderer,
            options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var reportDataFactory = new Mock<IRepositoryReportDataFactory>(MockBehavior.Strict).Object;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new RepositoryReportWorkflow(
            repoService,
            reportDataFactory,
            htmlReportRenderer,
            pdfReportRenderer,
            options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GenerateAsync loads pull request data and returns sorted report data")]
    [Trait("Category", "Unit")]
    public async Task GenerateAsyncWhenPullRequestReportsAreEnabledReturnsSortedReportData()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var filterPattern = new FilterPattern("Repo");
        var currentUserId = new BitbucketId("{user}");
        var reportOpenedAt = new DateTimeOffset(2026, 5, 12, 8, 0, 0, TimeSpan.Zero);
        var generatedAt = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.FromHours(2));
        var repositoryProgress = new Progress<RepoLoadProgress>();
        var pullRequestDetailsProgress = new Progress<PullRequestRepositoryLoadProgress>();
        var mergedPullRequestsProgress = new Progress<PullRequestRepositoryLoadProgress>();
        var repoZeta = new Repository("Zeta.Repo", null, null, "zeta-repo");
        var repoAlpha = new Repository("Alpha.Repo", null, null, "alpha-repo");
        var pullRequestDetail = CreatePullRequestDetail(repoAlpha);
        var mergedPullRequest = CreateMergedPullRequest(repoAlpha);
        var options = Options.Create(CreateOptions(prDetailsEnabled: true, mergedPullRequestsEnabled: true, mergedPullRequestsDays: 7));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(service => service.GetRepositoriesAsync(filterPattern, repositoryProgress, cts.Token))
            .ReturnsAsync([repoZeta, repoAlpha]);
        repoService.Setup(service => service.GetOpenPullRequestDetailsAsync(
                It.Is<IReadOnlyList<Repository>>(repositories => HasRepositoryOrder(repositories, "Alpha.Repo", "Zeta.Repo")),
                currentUserId,
                pullRequestDetailsProgress,
                cts.Token))
            .ReturnsAsync([pullRequestDetail]);
        repoService.Setup(service => service.GetMergedPullRequestsAsync(
                It.Is<IReadOnlyList<Repository>>(repositories => HasRepositoryOrder(repositories, "Alpha.Repo", "Zeta.Repo")),
                reportOpenedAt.AddDays(-7),
                currentUserId,
                mergedPullRequestsProgress,
                cts.Token))
            .ReturnsAsync([mergedPullRequest]);

        var workflow = CreateWorkflow(repoService.Object, options);

        // Act
        var result = await workflow.GenerateAsync(
            filterPattern,
            currentUserId,
            reportOpenedAt,
            generatedAt,
            repositoryProgress,
            pullRequestDetailsProgress,
            mergedPullRequestsProgress,
            cts.Token);

        // Assert
        result.Repositories.Select(static repository => repository.Name)
            .Should()
            .ContainInOrder("Alpha.Repo", "Zeta.Repo");
        result.PullRequestDetails.Should().ContainSingle().Which.Should().BeSameAs(pullRequestDetail);
        result.MergedPullRequests.Should().ContainSingle().Which.Should().BeSameAs(mergedPullRequest);
        result.ReportData.Repositories.Should().BeSameAs(result.Repositories);
        result.ReportData.PullRequestDetails.Should().BeSameAs(result.PullRequestDetails);
        result.ReportData.MergedPullRequests.Should().BeSameAs(result.MergedPullRequests);
        result.ReportData.FilterPhrase.Should().Be("Repo");
        result.ReportData.GeneratedAt.Should().Be(generatedAt);
        result.ReportData.MergedPullRequestsDays.Should().Be(7);

        repoService.VerifyAll();
    }

    [Fact(DisplayName = "GenerateAsync skips optional pull request loading when reports are disabled")]
    [Trait("Category", "Unit")]
    public async Task GenerateAsyncWhenPullRequestReportsAreDisabledSkipsPullRequestLoading()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var filterPattern = new FilterPattern(null);
        var currentUserId = new BitbucketId("{user}");
        var reportOpenedAt = new DateTimeOffset(2026, 5, 12, 8, 0, 0, TimeSpan.Zero);
        var generatedAt = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.FromHours(2));
        var repo = new Repository("Repo");
        var options = Options.Create(CreateOptions(prDetailsEnabled: false, mergedPullRequestsEnabled: false));

        var repoService = new Mock<IRepoService>(MockBehavior.Strict);
        repoService.Setup(service => service.GetRepositoriesAsync(filterPattern, null, cts.Token))
            .ReturnsAsync([repo]);

        var workflow = CreateWorkflow(repoService.Object, options);

        // Act
        var result = await workflow.GenerateAsync(
            filterPattern,
            currentUserId,
            reportOpenedAt,
            generatedAt,
            repositoryProgress: null,
            pullRequestDetailsProgress: null,
            mergedPullRequestsProgress: null,
            cts.Token);

        // Assert
        result.Repositories.Should().ContainSingle().Which.Should().BeSameAs(repo);
        result.PullRequestDetails.Should().BeEmpty();
        result.MergedPullRequests.Should().BeEmpty();

        repoService.VerifyAll();
        repoService.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "RenderReports delegates to HTML and PDF renderers")]
    [Trait("Category", "Unit")]
    public void RenderReportsWhenCalledDelegatesToReportRenderers()
    {
        // Arrange
        var reportData = CreateReportData();
        var htmlCalls = 0;
        var pdfCalls = 0;
        var repoService = new Mock<IRepoService>(MockBehavior.Strict).Object;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict);
        htmlReportRenderer.Setup(renderer => renderer.RenderReport(reportData))
            .Callback(() => htmlCalls++);
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        pdfReportRenderer.Setup(renderer => renderer.RenderReport(reportData))
            .Callback(() => pdfCalls++);

        var workflow = new RepositoryReportWorkflow(
            repoService,
            new Mock<IRepositoryReportDataFactory>(MockBehavior.Strict).Object,
            htmlReportRenderer.Object,
            pdfReportRenderer.Object,
            Options.Create(CreateOptions()));

        // Act
        workflow.RenderReports(reportData);

        // Assert
        htmlCalls.Should().Be(1);
        pdfCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RenderReports throws when report data is null")]
    [Trait("Category", "Unit")]
    public void RenderReportsWhenReportDataIsNullThrowsArgumentNullException()
    {
        // Arrange
        var workflow = CreateWorkflow(new Mock<IRepoService>(MockBehavior.Strict).Object, Options.Create(CreateOptions()));
        RepositoryReportData reportData = null!;

        // Act
        Action act = () => workflow.RenderReports(reportData);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    private static RepositoryReportWorkflow CreateWorkflow(
        IRepoService repoService,
        IOptions<BitbucketOptions> options)
    {
        return new RepositoryReportWorkflow(
            repoService,
            new RepositoryReportDataFactory(options),
            new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object,
            new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object,
            options);
    }

    private static bool HasRepositoryOrder(IReadOnlyList<Repository> repositories, params string[] names) =>
        repositories.Select(static repository => repository.Name).SequenceEqual(names, StringComparer.Ordinal);

    private static PullRequestDetail CreatePullRequestDetail(Repository repository)
    {
        var openedOn = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        return new PullRequestDetail(
            repository,
            101,
            "Open PR",
            openedOn,
            new BitbucketId("{author}"),
            "Author",
            openedOn.AddHours(1),
            openedOn.AddHours(2),
            hasCurrentUserDiscussion: true);
    }

    private static MergedPullRequest CreateMergedPullRequest(Repository repository)
    {
        var openedOn = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        return new MergedPullRequest(
            repository,
            202,
            "Merged PR",
            openedOn,
            new BitbucketId("{author}"),
            "Author",
            openedOn.AddHours(1),
            openedOn.AddHours(2),
            hasCurrentUserDiscussion: true,
            openedOn.AddHours(3),
            "Author");
    }

    private static RepositoryReportData CreateReportData()
    {
        return new RepositoryReportData(
            "workspace",
            "Repo",
            abandonedMonthsThreshold: 12,
            loadAbandonedRepositoriesStatistics: true,
            ttfrThresholdHours: 4,
            minimalDescriptionTextLength: 1,
            loadMergedPullRequests: true,
            mergedPullRequestsDays: 7,
            generatedAt: new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.FromHours(2)),
            repositories: [],
            mergedPullRequests: [],
            pullRequestDetails: []);
    }

    private static BitbucketOptions CreateOptions(
        bool prDetailsEnabled = false,
        bool mergedPullRequestsEnabled = false,
        int mergedPullRequestsDays = 1)
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
                TtfrThresholdHours = 4,
                MinimalDescriptionTextLength = 1
            },
            MergedPullRequests = new MergedPullRequestsOptions
            {
                IsEnabled = mergedPullRequestsEnabled,
                Days = mergedPullRequestsDays
            },
            Telemetry = new BitbucketTelemetryOptions(),
            AbandonedMonthsThreshold = 12,
            LoadAbandonedRepositoriesStatistics = true
        };
    }
}
