using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class RepoLoadProgressTests
{
    [Fact(DisplayName = "Constructor throws when seen is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSeenIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = -1;
        var matched = 0;

        // Act
        Action act = () => _ = new RepoLoadProgress(seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when matched is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenMatchedIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = 0;
        var matched = -1;

        // Act
        Action act = () => _ = new RepoLoadProgress(seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when loaded pull request statistics is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestStatisticsLoadedIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = 1;
        var matched = 1;

        // Act
        Action act = () => _ = new RepoLoadProgress(
            seen,
            matched,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: -1,
            pullRequestStatisticsTotal: 5);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when total pull request statistics is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestStatisticsTotalIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = 1;
        var matched = 1;

        // Act
        Action act = () => _ = new RepoLoadProgress(
            seen,
            matched,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 0,
            pullRequestStatisticsTotal: -1);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when loaded pull request statistics exceeds total")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestStatisticsLoadedExceedsTotalThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = 1;
        var matched = 1;

        // Act
        Action act = () => _ = new RepoLoadProgress(
            seen,
            matched,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 6,
            pullRequestStatisticsTotal: 5);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets seen and matched")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var seen = 120;
        var matched = 7;

        // Act
        var progress = new RepoLoadProgress(seen, matched);

        // Assert
        progress.Seen.Should().Be(seen);
        progress.Matched.Should().Be(matched);
        progress.IsLoadingPullRequestStatistics.Should().BeFalse();
        progress.PullRequestStatisticsLoaded.Should().Be(0);
        progress.PullRequestStatisticsTotal.Should().Be(0);
    }

    [Fact(DisplayName = "Constructor sets pull request statistics progress properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestStatisticsArgumentsAreValidSetsPullRequestStatisticsProperties()
    {
        // Arrange
        var seen = 120;
        var matched = 7;

        // Act
        var progress = new RepoLoadProgress(
            seen,
            matched,
            isLoadingPullRequestStatistics: true,
            pullRequestStatisticsLoaded: 3,
            pullRequestStatisticsTotal: 7);

        // Assert
        progress.Seen.Should().Be(seen);
        progress.Matched.Should().Be(matched);
        progress.IsLoadingPullRequestStatistics.Should().BeTrue();
        progress.PullRequestStatisticsLoaded.Should().Be(3);
        progress.PullRequestStatisticsTotal.Should().Be(7);
    }
}
