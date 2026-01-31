using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class BitbucketUserTests
{
    [Fact(DisplayName = "Constructor allows null values")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreNullSetsProperties()
    {
        // Arrange
        string? displayName = null;
        string? nickname = null;
        string? uuid = null;
        string? accountId = null;

        // Act
        var user = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        user.DisplayName.Should().BeNull();
        user.Nickname.Should().BeNull();
        user.Uuid.Should().BeNull();
        user.AccountId.Should().BeNull();
    }

    [Fact(DisplayName = "Constructor trims non-empty values")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsContainWhitespaceTrimsValues()
    {
        // Arrange
        var displayName = "  Jane Doe  ";
        var nickname = "  jdoe  ";
        var uuid = "  123  ";
        var accountId = "  acc-1  ";

        // Act
        var user = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        user.DisplayName.Should().Be("Jane Doe");
        user.Nickname.Should().Be("jdoe");
        user.Uuid.Should().Be("123");
        user.AccountId.Should().Be("acc-1");
    }

    [Fact(DisplayName = "Constructor throws when display name is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDisplayNameIsWhitespaceThrowsArgumentException()
    {
        // Arrange
        var displayName = " ";
        var nickname = "jdoe";
        var uuid = "123";
        var accountId = "acc-1";

        // Act
        Action act = () => _ = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when nickname is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNicknameIsWhitespaceThrowsArgumentException()
    {
        // Arrange
        var displayName = "Jane Doe";
        var nickname = "  ";
        var uuid = "123";
        var accountId = "acc-1";

        // Act
        Action act = () => _ = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when uuid is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenUuidIsWhitespaceThrowsArgumentException()
    {
        // Arrange
        var displayName = "Jane Doe";
        var nickname = "jdoe";
        var uuid = "\t";
        var accountId = "acc-1";

        // Act
        Action act = () => _ = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when account id is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAccountIdIsWhitespaceThrowsArgumentException()
    {
        // Arrange
        var displayName = "Jane Doe";
        var nickname = "jdoe";
        var uuid = "123";
        var accountId = "\r\n";

        // Act
        Action act = () => _ = new BitbucketUser(displayName, nickname, uuid, accountId);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }
}
