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
}
