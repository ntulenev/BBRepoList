using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class RepoPageTests
{
    [Fact(DisplayName = "Constructor throws when values is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesIsNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<Repository> values = null!;
        Uri? next = null;

        // Act
        Action act = () => _ = new RepoPage(values, next);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when values contains null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesContainNullThrowsArgumentException()
    {
        // Arrange
        var values = new List<Repository?> { new("Repo-1"), null };
        Uri? next = null;

        // Act
        Action act = () => _ = new RepoPage(values!, next);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor sets values and next")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var values = new List<Repository> { new("Repo-1"), new("Repo-2") };
        var next = new Uri("https://example.test/page/2", UriKind.Absolute);

        // Act
        var page = new RepoPage(values, next);

        // Assert
        page.Values.Should().BeSameAs(values);
        page.Next.Should().Be(next);
    }

    [Fact(DisplayName = "Constructor allows next to be null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenNextIsNullSetsNextToNull()
    {
        // Arrange
        var values = new List<Repository> { new("Repo-1") };
        Uri? next = null;

        // Act
        var page = new RepoPage(values, next);

        // Assert
        page.Values.Should().BeSameAs(values);
        page.Next.Should().BeNull();
    }
}
