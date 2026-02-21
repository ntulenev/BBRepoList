using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class RepositoryTests
{
    [Fact(DisplayName = "Constructor throws when name is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNameIsNullThrowsArgumentException()
    {
        // Arrange
        string name = null!;

        // Act
        Action act = () => _ = new Repository(name);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when name is empty")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNameIsEmptyThrowsArgumentException()
    {
        // Arrange
        var name = string.Empty;

        // Act
        Action act = () => _ = new Repository(name);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when name is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNameIsWhitespaceThrowsArgumentException()
    {
        // Arrange
        var name = "   ";

        // Act
        Action act = () => _ = new Repository(name);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims name")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNameHasWhitespaceTrimsName()
    {
        // Arrange
        var name = "  Repo-1  ";

        // Act
        var repo = new Repository(name);

        // Assert
        repo.Name.Should().Be("Repo-1");
    }

    [Fact(DisplayName = "Constructor sets name")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNameIsValidSetsName()
    {
        // Arrange
        var name = "Repo-1";

        // Act
        var repo = new Repository(name);

        // Assert
        repo.Name.Should().Be(name);
    }

    [Fact(DisplayName = "Constructor sets created on")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCreatedOnIsProvidedSetsCreatedOn()
    {
        // Arrange
        var createdOn = new DateTimeOffset(2025, 1, 10, 9, 15, 0, TimeSpan.Zero);

        // Act
        var repo = new Repository("Repo-1", createdOn);

        // Assert
        repo.CreatedOn.Should().Be(createdOn);
    }

    [Fact(DisplayName = "Constructor sets last updated on")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenLastUpdatedOnIsProvidedSetsLastUpdatedOn()
    {
        // Arrange
        var lastUpdatedOn = new DateTimeOffset(2025, 1, 15, 18, 10, 0, TimeSpan.Zero);

        // Act
        var repo = new Repository("Repo-1", null, lastUpdatedOn);

        // Assert
        repo.LastUpdatedOn.Should().Be(lastUpdatedOn);
    }

    [Fact(DisplayName = "Constructor sets open pull requests count")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOpenPullRequestsCountIsProvidedSetsOpenPullRequestsCount()
    {
        // Arrange
        var openPullRequestsCount = 7;

        // Act
        var repo = new Repository("Repo-1", null, null, openPullRequestsCount);

        // Assert
        repo.OpenPullRequestsCount.Should().Be(openPullRequestsCount);
    }

    [Fact(DisplayName = "Constructor marks inactivity timing as calculable when created and updated dates are provided")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCreatedAndUpdatedDatesAreProvidedMarksInactivityTimingAsCalculable()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var createdOn = now.AddMonths(-20);
        var lastUpdatedOn = now.AddMonths(-15);

        // Act
        var repo = new Repository("Repo-1", createdOn, lastUpdatedOn);

        // Assert
        repo.CanCalculateInactivityTiming.Should().BeTrue();
        repo.MonthsWithoutActivity.Should().Be(15);
    }

    [Fact(DisplayName = "Constructor marks inactivity timing as non calculable when at least one date is missing")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCreatedOrUpdatedDateIsMissingMarksInactivityTimingAsNonCalculable()
    {
        // Arrange
        var createdOnly = new Repository("Repo-1", DateTimeOffset.UtcNow.AddMonths(-12), null);
        var updatedOnly = new Repository("Repo-2", null, DateTimeOffset.UtcNow.AddMonths(-12));

        // Assert
        createdOnly.CanCalculateInactivityTiming.Should().BeFalse();
        createdOnly.MonthsWithoutActivity.Should().Be(0);
        updatedOnly.CanCalculateInactivityTiming.Should().BeFalse();
        updatedOnly.MonthsWithoutActivity.Should().Be(0);
    }

    [Fact(DisplayName = "Constructor trims slug")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSlugHasWhitespaceTrimsSlug()
    {
        // Arrange
        var slug = "  repo-1  ";

        // Act
        var repo = new Repository("Repo-1", null, null, null, slug);

        // Assert
        repo.Slug.Should().Be("repo-1");
    }

    [Fact(DisplayName = "Constructor leaves created on null when omitted")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCreatedOnIsOmittedLeavesCreatedOnNull()
    {
        // Arrange
        var name = "Repo-1";

        // Act
        var repo = new Repository(name);

        // Assert
        repo.CreatedOn.Should().BeNull();
        repo.LastUpdatedOn.Should().BeNull();
        repo.OpenPullRequestsCount.Should().BeNull();
        repo.Slug.Should().BeNull();
        repo.CanCalculateInactivityTiming.Should().BeFalse();
        repo.MonthsWithoutActivity.Should().Be(0);
    }
}
