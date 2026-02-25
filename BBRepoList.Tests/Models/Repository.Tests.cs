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

    [Fact(DisplayName = "Constructor initializes open pull requests count to zero")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledInitializesOpenPullRequestsCountToZero()
    {
        // Act
        var repo = new Repository("Repo-1");

        // Assert
        repo.OpenPullRequestsCount.Should().Be(0);
    }

    [Fact(DisplayName = "UpdateOpenPullRequestsCount updates open pull requests count")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestsCountWhenCalledUpdatesOpenPullRequestsCount()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");

        // Act
        repo.UpdateOpenPullRequestsCount(5);

        // Assert
        repo.OpenPullRequestsCount.Should().Be(5);
    }

    [Fact(DisplayName = "UpdateOpenPullRequestsCount throws when value is negative")]
    [Trait("Category", "Unit")]
    public void UpdateOpenPullRequestsCountWhenValueIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");

        // Act
        Action act = () => repo.UpdateOpenPullRequestsCount(-1);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "CanPopulateOpenPullRequestsCount is true when slug is provided")]
    [Trait("Category", "Unit")]
    public void CanPopulateOpenPullRequestsCountWhenSlugIsProvidedReturnsTrue()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");

        // Assert
        repo.CanPopulateOpenPullRequestsCount.Should().BeTrue();
    }

    [Fact(DisplayName = "CanPopulateOpenPullRequestsCount is true when count is already set and slug is provided")]
    [Trait("Category", "Unit")]
    public void CanPopulateOpenPullRequestsCountWhenCountIsSetReturnsTrue()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");
        repo.UpdateOpenPullRequestsCount(2);

        // Assert
        repo.CanPopulateOpenPullRequestsCount.Should().BeTrue();
    }

    [Fact(DisplayName = "CanPopulateOpenPullRequestsCount is false when slug is missing")]
    [Trait("Category", "Unit")]
    public void CanPopulateOpenPullRequestsCountWhenSlugIsMissingReturnsFalse()
    {
        // Arrange
        var repo = new Repository("Repo-1");

        // Assert
        repo.CanPopulateOpenPullRequestsCount.Should().BeFalse();
    }

    [Fact(DisplayName = "CanLoadOpenPullRequestDetails is true when slug is present and count is greater than zero")]
    [Trait("Category", "Unit")]
    public void CanLoadOpenPullRequestDetailsWhenSlugIsPresentAndCountIsGreaterThanZeroReturnsTrue()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");
        repo.UpdateOpenPullRequestsCount(1);

        // Assert
        repo.CanLoadOpenPullRequestDetails.Should().BeTrue();
    }

    [Fact(DisplayName = "CanLoadOpenPullRequestDetails is false when slug is missing")]
    [Trait("Category", "Unit")]
    public void CanLoadOpenPullRequestDetailsWhenSlugIsMissingReturnsFalse()
    {
        // Arrange
        var repo = new Repository("Repo-1");

        // Assert
        repo.CanLoadOpenPullRequestDetails.Should().BeFalse();
    }

    [Fact(DisplayName = "CanLoadOpenPullRequestDetails is false when open pull requests count is zero")]
    [Trait("Category", "Unit")]
    public void CanLoadOpenPullRequestDetailsWhenOpenPullRequestsCountIsZeroReturnsFalse()
    {
        // Arrange
        var repo = new Repository("Repo-1", null, null, "repo-1");

        // Assert
        repo.CanLoadOpenPullRequestDetails.Should().BeFalse();
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
        var repo = new Repository("Repo-1", null, null, slug);

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
        repo.OpenPullRequestsCount.Should().Be(0);
        repo.Slug.Should().BeNull();
        repo.CanPopulateOpenPullRequestsCount.Should().BeFalse();
        repo.CanLoadOpenPullRequestDetails.Should().BeFalse();
        repo.CanCalculateInactivityTiming.Should().BeFalse();
        repo.MonthsWithoutActivity.Should().Be(0);
    }
}
