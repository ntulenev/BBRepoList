using System.Globalization;

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
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);

        // Act
        var resolvedPath = settings.ResolveOutputPath();

        // Assert
        Path.IsPathRooted(resolvedPath).Should().BeTrue();
        resolvedPath.Should().Contain("bbrepolist-report_" + dateSuffix + ".pdf");
    }
}
