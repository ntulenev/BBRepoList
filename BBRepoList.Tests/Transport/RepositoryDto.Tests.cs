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

        // Act
        var dto = new RepositoryDto(name);

        // Assert
        dto.Name.Should().Be(name);
    }

    [Fact(DisplayName = "RepositoryDto serializes name as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenNameIsSetUsesExpectedJsonProperty()
    {
        // Arrange
        var dto = new RepositoryDto("Repo-1");

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"name\":\"Repo-1\"");
    }
}
