using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class FilterPatternTests
{
    [Fact(DisplayName = "Filter returns true when phrase is null")]
    [Trait("Category", "Unit")]
    public void FilterWhenPhraseIsNullReturnsTrue()
    {
        // Arrange
        var pattern = new FilterPattern(null);
        var repository = new Repository("Any-Name");

        // Act
        var result = pattern.Filter(repository);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Filter matches repository name case-insensitively")]
    [Trait("Category", "Unit")]
    public void FilterWhenPhraseMatchesReturnsTrue()
    {
        // Arrange
        var pattern = new FilterPattern("app");
        var repository = new Repository("My-App-Repo");

        // Act
        var result = pattern.Filter(repository);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Filter returns false when phrase does not match")]
    [Trait("Category", "Unit")]
    public void FilterWhenPhraseDoesNotMatchReturnsFalse()
    {
        // Arrange
        var pattern = new FilterPattern("zzz");
        var repository = new Repository("My-App-Repo");

        // Act
        var result = pattern.Filter(repository);

        // Assert
        result.Should().BeFalse();
    }

    [Theory(DisplayName = "HasFilter returns true when phrase is null, empty, or whitespace")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [Trait("Category", "Unit")]
    public void HasFilterWhenPhraseIsNullOrWhiteSpaceReturnsTrue(string? phrase)
    {
        // Arrange
        var pattern = new FilterPattern(phrase);

        // Act
        var result = pattern.HasFilter;

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "HasFilter returns false when phrase contains non-whitespace")]
    [Trait("Category", "Unit")]
    public void HasFilterWhenPhraseHasValueReturnsFalse()
    {
        // Arrange
        var pattern = new FilterPattern("app");

        // Act
        var result = pattern.HasFilter;

        // Assert
        result.Should().BeTrue();
    }
}
