using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class BitbucketIdTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new BitbucketId(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when value is empty")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsEmptyThrowsArgumentException()
    {
        // Arrange
        var value = string.Empty;

        // Act
        Action act = () => _ = new BitbucketId(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor sets value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsValidSetsValue()
    {
        // Arrange
        var value = "{uuid}";

        // Act
        var id = new BitbucketId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueHasOuterWhitespaceTrimsValue()
    {
        // Arrange
        var value = "  {uuid}  ";

        // Act
        var id = new BitbucketId(value);

        // Assert
        id.Value.Should().Be("{uuid}");
    }

    [Fact(DisplayName = "Equality ignores braces and casing")]
    [Trait("Category", "Unit")]
    public void EqualsWhenValuesDifferByBracesAndCasingReturnsTrue()
    {
        // Arrange
        var first = new BitbucketId("{AbC-123}");
        var second = new BitbucketId("abc-123");

        // Act
        var areEqual = first == second;

        // Assert
        areEqual.Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact(DisplayName = "TryCreate returns false for whitespace")]
    [Trait("Category", "Unit")]
    public void TryCreateWhenValueIsWhitespaceReturnsFalse()
    {
        // Act
        var success = BitbucketId.TryCreate("   ", out var id);

        // Assert
        success.Should().BeFalse();
        id.Should().Be(default(BitbucketId));
    }

    [Fact(DisplayName = "TryCreate returns true and trims value")]
    [Trait("Category", "Unit")]
    public void TryCreateWhenValueIsValidReturnsTrue()
    {
        // Act
        var success = BitbucketId.TryCreate("  {uuid}  ", out var id);

        // Assert
        success.Should().BeTrue();
        id.Value.Should().Be("{uuid}");
    }
}
