using FluentAssertions;

using BBRepoList.Models;
using BBRepoList.Transport;

namespace BBRepoList.Tests.Transport;

public sealed class BitbucketMappingsTests
{
    [Fact(DisplayName = "ToDomain throws when repository dto is null")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryDtoIsNullThrowsArgumentNullException()
    {
        // Arrange
        RepositoryDto dto = null!;

        // Act
        Action act = () => _ = dto.ToDomain();

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ToDomain throws when repository name is null")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryNameIsNullThrowsArgumentException()
    {
        // Arrange
        var dto = new RepositoryDto(null);

        // Act
        Action act = () => _ = dto.ToDomain();

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "ToDomain maps repository dto")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepositoryDtoIsValidCreatesRepository()
    {
        // Arrange
        var createdOn = new DateTimeOffset(2025, 2, 3, 12, 30, 0, TimeSpan.Zero);
        var dto = new RepositoryDto("Repo-1", createdOn);

        // Act
        var repository = dto.ToDomain();

        // Assert
        repository.Should().BeOfType<Repository>();
        repository.Name.Should().Be("Repo-1");
        repository.CreatedOn.Should().Be(createdOn);
    }

    [Fact(DisplayName = "ToDomain throws when repository page dto is null")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepoPageDtoIsNullThrowsArgumentNullException()
    {
        // Arrange
        RepoPageDto dto = null!;

        // Act
        Action act = () => _ = dto.ToDomain();

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ToDomain maps repository page dto with null values")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepoPageValuesAreNullCreatesEmptyPage()
    {
        // Arrange
        var dto = new RepoPageDto(null, null);

        // Act
        var page = dto.ToDomain();

        // Assert
        page.Values.Should().NotBeNull();
        page.Values.Should().BeEmpty();
        page.Next.Should().BeNull();
    }

    [Fact(DisplayName = "ToDomain throws when repository page values contain null")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepoPageValuesContainNullThrowsArgumentException()
    {
        // Arrange
        var values = new List<RepositoryDto?> { new("Repo-1"), null };
        var dto = new RepoPageDto(values!, null);

        // Act
        Action act = () => _ = dto.ToDomain();

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "ToDomain maps repository page dto values and next")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenRepoPageDtoIsValidCreatesRepoPage()
    {
        // Arrange
        var values = new List<RepositoryDto>
        {
            new("Repo-1"),
            new("Repo-2")
        };
        var next = new Uri("https://example.test/page/2", UriKind.Absolute);
        var dto = new RepoPageDto(values, next);

        // Act
        var page = dto.ToDomain();

        // Assert
        page.Values.Should().HaveCount(2);
        page.Values[0].Name.Should().Be("Repo-1");
        page.Values[1].Name.Should().Be("Repo-2");
        page.Next.Should().Be(next);
    }

    [Fact(DisplayName = "ToDomain throws when user dto is null")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenUserDtoIsNullThrowsArgumentNullException()
    {
        // Arrange
        BitbucketUserDto dto = null!;

        // Act
        Action act = () => _ = dto.ToDomain();

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ToDomain maps user dto")]
    [Trait("Category", "Unit")]
    public void ToDomainWhenUserDtoIsValidCreatesBitbucketUser()
    {
        // Arrange
        var dto = new BitbucketUserDto("{uuid}", "Jane Doe");

        // Act
        var user = dto.ToDomain();

        // Assert
        user.DisplayName.Value.Should().Be("Jane Doe");
        user.Uuid.Should().Be(new BitbucketId("{uuid}"));
    }
}
