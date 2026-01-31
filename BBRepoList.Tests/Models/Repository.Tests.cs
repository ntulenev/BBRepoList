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
}
