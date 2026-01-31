using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class RepoLoadProgressTests
{
    [Fact(DisplayName = "Constructor throws when pages is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPagesIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pages = -1;
        var seen = 0;
        var matched = 0;

        // Act
        Action act = () => _ = new RepoLoadProgress(pages, seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when seen is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSeenIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pages = 0;
        var seen = -1;
        var matched = 0;

        // Act
        Action act = () => _ = new RepoLoadProgress(pages, seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when matched is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenMatchedIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pages = 0;
        var seen = 0;
        var matched = -1;

        // Act
        Action act = () => _ = new RepoLoadProgress(pages, seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets pages, seen, and matched")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var pages = 3;
        var seen = 120;
        var matched = 7;

        // Act
        var progress = new RepoLoadProgress(pages, seen, matched);

        // Assert
        progress.Pages.Should().Be(pages);
        progress.Seen.Should().Be(seen);
        progress.Matched.Should().Be(matched);
    }
}
