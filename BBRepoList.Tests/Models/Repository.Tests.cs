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
    }
}
