using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class UserNameTests
{
    [Fact(DisplayName = "Constructor uses default when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullSetsDefault()
    {
        // Arrange
        string? value = null;

        // Act
        var userName = new UserName(value);

        // Assert
        userName.Value.Should().Be("<N/A>");
    }

    [Fact(DisplayName = "Constructor preserves non-null value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsProvidedSetsValue()
    {
        // Arrange
        var value = "Jane Doe";

        // Act
        var userName = new UserName(value);

        // Assert
        userName.Value.Should().Be(value);
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var value = "Jane Doe";
        var userName = new UserName(value);

        // Act
        var text = userName.ToString();

        // Assert
        text.Should().Be(value);
    }
}
