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
        var id = "{uuid}";

        // Act
        var dto = new BitbucketUserDto(id, displayName);

        // Assert
        dto.DisplayName.Should().Be(displayName);
        dto.Id.Should().Be(id);
    }

    [Fact(DisplayName = "BitbucketUserDto serializes properties as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new BitbucketUserDto("{uuid}", "Jane Doe");

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"uuid\":\"{uuid}\"");
        json.Should().Contain("\"display_name\":\"Jane Doe\"");
    }
}
