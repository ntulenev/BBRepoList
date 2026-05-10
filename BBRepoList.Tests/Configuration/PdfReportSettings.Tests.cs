using BBRepoList.Configuration;

using FluentAssertions;

namespace BBRepoList.Tests.Configuration;

public sealed class PdfReportSettingsTests
{
    [Fact(DisplayName = "Constructor sets defaults when arguments are omitted")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreOmittedSetsDefaults()
    {
        // Act
        var settings = new PdfReportSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.OutputPath.Should().Be("bbrepolist-report.pdf");
    }

    [Fact(DisplayName = "ResolveOutputPath returns absolute dated path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenOutputPathIsRelativeReturnsAbsolutePathWithDateSuffix()
    {
        // Arrange
        var settings = new PdfReportSettings(true, "reports\\bbrepolist-report.pdf");
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 5, 10, 13, 15, 0, TimeSpan.Zero));

        // Act
        var resolvedPath = settings.ResolveOutputPath(timeProvider);

        // Assert
        Path.IsPathRooted(resolvedPath).Should().BeTrue();
        resolvedPath.Should().Contain("bbrepolist-report_10_05_2026.pdf");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
