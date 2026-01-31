using FluentAssertions;

using System.Text.Json;

using BBRepoList.Transport;

namespace BBRepoList.Tests.Transport;

public sealed class BitbucketUserDtoTests
{
    [Fact(DisplayName = "BitbucketUserDto sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var displayName = "Jane Doe";
        var nickname = "jdoe";
        var uuid = "{uuid}";
        var accountId = "acc-1";

        // Act
        var dto = new BitbucketUserDto(displayName, nickname, uuid, accountId);

        // Assert
        dto.DisplayName.Should().Be(displayName);
        dto.Nickname.Should().Be(nickname);
        dto.Uuid.Should().Be(uuid);
        dto.AccountId.Should().Be(accountId);
    }

    [Fact(DisplayName = "BitbucketUserDto serializes properties as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new BitbucketUserDto("Jane Doe", "jdoe", "{uuid}", "acc-1");

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"display_name\":\"Jane Doe\"");
        json.Should().Contain("\"nickname\":\"jdoe\"");
        json.Should().Contain("\"uuid\":\"{uuid}\"");
        json.Should().Contain("\"account_id\":\"acc-1\"");
    }
}
