using FluentAssertions;

using BBRepoList.Models;

namespace BBRepoList.Tests.Models;

public sealed class RepoLoadProgressTests
{
    [Fact(DisplayName = "Constructor throws when seen is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSeenIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = -1;
        var matched = 0;

        // Act
        Action act = () => _ = new RepoLoadProgress(seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when matched is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenMatchedIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var seen = 0;
        var matched = -1;

        // Act
        Action act = () => _ = new RepoLoadProgress(seen, matched);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets seen and matched")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var seen = 120;
        var matched = 7;

        // Act
        var progress = new RepoLoadProgress(seen, matched);

        // Assert
        progress.Seen.Should().Be(seen);
        progress.Matched.Should().Be(matched);
    }
}
