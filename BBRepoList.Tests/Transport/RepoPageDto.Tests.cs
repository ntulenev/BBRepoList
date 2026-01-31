using FluentAssertions;

using System.Text.Json;

using BBRepoList.Transport;

namespace BBRepoList.Tests.Transport;

public sealed class RepoPageDtoTests
{
    [Fact(DisplayName = "RepoPageDto sets values and next")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var values = new List<RepositoryDto> { new("Repo-1") };
        var next = new Uri("https://example.test/page/2", UriKind.Absolute);

        // Act
        var dto = new RepoPageDto(values, next);

        // Assert
        dto.Values.Should().BeSameAs(values);
        dto.Next.Should().Be(next);
    }

    [Fact(DisplayName = "RepoPageDto serializes values and next as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAndNextAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var values = new List<RepositoryDto> { new("Repo-1") };
        var next = new Uri("https://example.test/page/2", UriKind.Absolute);
        var dto = new RepoPageDto(values, next);

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"values\"");
        json.Should().Contain("\"name\":\"Repo-1\"");
        json.Should().Contain("\"next\":\"https://example.test/page/2\"");
    }
}
