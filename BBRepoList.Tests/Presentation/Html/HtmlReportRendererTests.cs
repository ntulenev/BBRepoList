using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Presentation.Html;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.Presentation.Html;

public sealed class HtmlReportRendererTests
{
    [Fact(DisplayName = "RenderReport opens browser when html open in browser is enabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenOpenInBrowserIsEnabledLaunchesSavedReport()
    {
        // Arrange
        var outputPath = Path.GetFullPath("reports\\open-prs.html", Directory.GetCurrentDirectory());
        var htmlContent = "<html></html>";
        var options = CreateOptions(openInBrowser: true, outputPath);
        var fileStore = new Mock<IHtmlReportFileStore>(MockBehavior.Strict);
        fileStore.Setup(store => store.Save(It.IsAny<string>(), htmlContent));

        var composer = new Mock<IHtmlContentComposer>(MockBehavior.Strict);
        composer.Setup(c => c.Compose(It.IsAny<RepositoryPdfReportData>())).Returns(htmlContent);

        var launcher = new Mock<IHtmlReportLauncher>(MockBehavior.Strict);
        launcher.Setup(l => l.Open(It.IsAny<string>()));

        var renderer = new HtmlReportRenderer(options, fileStore.Object, composer.Object, launcher.Object);
        var reportData = CreateReportData();

        // Act
        renderer.RenderReport(reportData);

        // Assert
        launcher.Verify(l => l.Open(It.Is<string>(path => path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact(DisplayName = "RenderReport does not open browser when html open in browser is disabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenOpenInBrowserIsDisabledDoesNotLaunchSavedReport()
    {
        // Arrange
        var outputPath = Path.GetFullPath("reports\\open-prs.html", Directory.GetCurrentDirectory());
        var htmlContent = "<html></html>";
        var options = CreateOptions(openInBrowser: false, outputPath);
        var fileStore = new Mock<IHtmlReportFileStore>(MockBehavior.Strict);
        fileStore.Setup(store => store.Save(It.IsAny<string>(), htmlContent));

        var composer = new Mock<IHtmlContentComposer>(MockBehavior.Strict);
        composer.Setup(c => c.Compose(It.IsAny<RepositoryPdfReportData>())).Returns(htmlContent);

        var launcher = new Mock<IHtmlReportLauncher>(MockBehavior.Strict);

        var renderer = new HtmlReportRenderer(options, fileStore.Object, composer.Object, launcher.Object);
        var reportData = CreateReportData();

        // Act
        renderer.RenderReport(reportData);

        // Assert
        launcher.Invocations.Should().BeEmpty();
    }

    private static IOptions<BitbucketOptions> CreateOptions(bool openInBrowser, string outputPath) =>
        Options.Create(new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            Html = new HtmlOptions
            {
                Enabled = true,
                OutputPath = outputPath,
                OpenInBrowser = openInBrowser
            }
        });

    private static RepositoryPdfReportData CreateReportData() =>
        new(
            "workspace",
            null,
            12,
            true,
            4,
            1,
            new DateTimeOffset(2026, 3, 13, 10, 0, 0, TimeSpan.Zero),
            [],
            []);
}
