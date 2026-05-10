using BBRepoList.Configuration;

using FluentAssertions;

namespace BBRepoList.Tests.Configuration;

public sealed class HtmlReportSettingsTests
{
    [Fact(DisplayName = "Constructor sets defaults when arguments are omitted")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreOmittedSetsDefaults()
    {
        // Act
        var settings = new HtmlReportSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.OutputPath.Should().Be("bbrepolist-pr-details.html");
        settings.OpenInBrowser.Should().BeFalse();
    }

    [Fact(DisplayName = "ResolveOutputPath returns absolute dated path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenOutputPathIsRelativeReturnsAbsolutePathWithDateSuffix()
    {
        // Arrange
        var settings = new HtmlReportSettings(true, "reports\\bbrepolist-pr-details.html");
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 5, 10, 13, 15, 0, TimeSpan.Zero));

        // Act
        var resolvedPath = settings.ResolveOutputPath(timeProvider);

        // Assert
        Path.IsPathRooted(resolvedPath).Should().BeTrue();
        resolvedPath.Should().Contain("bbrepolist-pr-details_10_05_2026.html");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
