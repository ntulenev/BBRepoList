using FluentAssertions;

using System.Text.Json;

using BBRepoList.Transport;

namespace BBRepoList.Tests.Transport;

public sealed class RepositoryDtoTests
{
    [Fact(DisplayName = "RepositoryDto sets name")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var name = "Repo-1";
        var createdOn = new DateTimeOffset(2025, 2, 3, 12, 30, 0, TimeSpan.Zero);

        // Act
        var dto = new RepositoryDto(name, createdOn);

        // Assert
        dto.Name.Should().Be(name);
        dto.CreatedOn.Should().Be(createdOn);
    }

    [Fact(DisplayName = "RepositoryDto serializes name and created_on as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenPropertiesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var createdOn = new DateTimeOffset(2025, 2, 3, 12, 30, 0, TimeSpan.Zero);
        var dto = new RepositoryDto("Repo-1", createdOn);

        // Act
        var json = JsonSerializer.Serialize(dto);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Assert
        root.GetProperty("name").GetString().Should().Be("Repo-1");
        root.GetProperty("created_on").GetDateTimeOffset().Should().Be(createdOn);
    }
}
