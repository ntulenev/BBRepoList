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
        var updatedOn = new DateTimeOffset(2025, 2, 10, 8, 0, 0, TimeSpan.Zero);
        var slug = "repo-1";
        var openPullRequestsCount = 4;

        // Act
        var dto = new RepositoryDto(name, createdOn, updatedOn, slug, openPullRequestsCount);

        // Assert
        dto.Name.Should().Be(name);
        dto.CreatedOn.Should().Be(createdOn);
        dto.UpdatedOn.Should().Be(updatedOn);
        dto.Slug.Should().Be(slug);
        dto.OpenPullRequestsCount.Should().Be(openPullRequestsCount);
    }

    [Fact(DisplayName = "RepositoryDto serializes name and created_on as expected")]
    [Trait("Category", "Unit")]
    public void SerializeWhenPropertiesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var createdOn = new DateTimeOffset(2025, 2, 3, 12, 30, 0, TimeSpan.Zero);
        var updatedOn = new DateTimeOffset(2025, 2, 10, 8, 0, 0, TimeSpan.Zero);
        var dto = new RepositoryDto("Repo-1", createdOn, updatedOn, "repo-1", 3);

        // Act
        var json = JsonSerializer.Serialize(dto);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Assert
        root.GetProperty("name").GetString().Should().Be("Repo-1");
        root.GetProperty("created_on").GetDateTimeOffset().Should().Be(createdOn);
        root.GetProperty("updated_on").GetDateTimeOffset().Should().Be(updatedOn);
        root.GetProperty("slug").GetString().Should().Be("repo-1");
        root.GetProperty("open_pull_requests_count").GetInt32().Should().Be(3);
    }
}
