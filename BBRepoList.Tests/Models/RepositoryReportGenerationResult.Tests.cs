using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Tests.Models;

public sealed class RepositoryReportGenerationResultTests
{
    [Fact(DisplayName = "Constructor throws when repositories are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepositoriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<Repository> repositories = null!;
        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        IReadOnlyList<PullRequestDetail> pullRequestDetails = [];
        var reportData = CreateReportData([]);

        // Act
        Action act = () => _ = new RepositoryReportGenerationResult(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when merged pull requests are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenMergedPullRequestsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<Repository> repositories = [];
        IReadOnlyList<MergedPullRequest> mergedPullRequests = null!;
        IReadOnlyList<PullRequestDetail> pullRequestDetails = [];
        var reportData = CreateReportData(repositories);

        // Act
        Action act = () => _ = new RepositoryReportGenerationResult(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when pull request details are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestDetailsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<Repository> repositories = [];
        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        IReadOnlyList<PullRequestDetail> pullRequestDetails = null!;
        var reportData = CreateReportData(repositories);

        // Act
        Action act = () => _ = new RepositoryReportGenerationResult(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when report data is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenReportDataIsNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<Repository> repositories = [];
        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        IReadOnlyList<PullRequestDetail> pullRequestDetails = [];
        RepositoryReportData reportData = null!;

        // Act
        Action act = () => _ = new RepositoryReportGenerationResult(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor assigns properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledAssignsProperties()
    {
        // Arrange
        IReadOnlyList<Repository> repositories = [new Repository("Repo")];
        IReadOnlyList<MergedPullRequest> mergedPullRequests = [];
        IReadOnlyList<PullRequestDetail> pullRequestDetails = [];
        var reportData = CreateReportData(repositories);

        // Act
        var result = new RepositoryReportGenerationResult(
            repositories,
            mergedPullRequests,
            pullRequestDetails,
            reportData);

        // Assert
        result.Repositories.Should().BeSameAs(repositories);
        result.MergedPullRequests.Should().BeSameAs(mergedPullRequests);
        result.PullRequestDetails.Should().BeSameAs(pullRequestDetails);
        result.ReportData.Should().BeSameAs(reportData);
    }

    private static RepositoryReportData CreateReportData(IReadOnlyList<Repository> repositories)
    {
        return new RepositoryReportData(
            "workspace",
            "Repo",
            abandonedMonthsThreshold: 12,
            loadAbandonedRepositoriesStatistics: true,
            ttfrThresholdHours: 4,
            minimalDescriptionTextLength: 1,
            loadMergedPullRequests: false,
            mergedPullRequestsDays: 1,
            generatedAt: new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.FromHours(2)),
            repositories,
            mergedPullRequests: [],
            pullRequestDetails: []);
    }
}
