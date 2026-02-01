using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class BitbucketUserTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var displayName = new UserName("Jane Doe");
        var uuid = new BitbucketId("{uuid}");

        // Act
        var user = new BitbucketUser(uuid, displayName);

        // Assert
        user.DisplayName.Should().Be(displayName);
        user.Uuid.Should().Be(uuid);
    }

    [Fact(DisplayName = "Constructor uses default display name when null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDisplayNameIsNullSetsDefault()
    {
        // Arrange
        var displayName = new UserName(null);
        var uuid = new BitbucketId("{uuid}");

        // Act
        var user = new BitbucketUser(uuid, displayName);

        // Assert
        user.DisplayName.Value.Should().Be("<N/A>");
        user.Uuid.Should().Be(uuid);
    }
}
