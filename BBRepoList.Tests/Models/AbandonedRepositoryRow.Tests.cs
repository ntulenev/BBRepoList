using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Tests.Models;

public sealed class AbandonedRepositoryRowTests
{
    [Fact(DisplayName = "Constructor throws when repository is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRepositoryIsNullThrowsArgumentNullException()
    {
        // Arrange
        Repository repository = null!;
        var lastActivityOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        Action act = () => _ = new AbandonedRepositoryRow(repository, lastActivityOn, 15);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var repository = new Repository("Repo-1");
        var lastActivityOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const int monthsWithoutActivity = 15;

        // Act
        var row = new AbandonedRepositoryRow(repository, lastActivityOn, monthsWithoutActivity);

        // Assert
        row.Repository.Should().BeSameAs(repository);
        row.LastActivityOn.Should().Be(lastActivityOn);
        row.MonthsWithoutActivity.Should().Be(monthsWithoutActivity);
    }
}
