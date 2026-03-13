using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using Spectre.Console;

namespace BBRepoList.Presentation.Html;

/// <summary>
/// HTML implementation for open pull request analysis rendering.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlReportRenderer"/> class.
    /// </summary>
    /// <param name="options">Bitbucket options.</param>
    /// <param name="htmlReportFileStore">HTML output file store.</param>
    /// <param name="htmlContentComposer">HTML content composer.</param>
    public HtmlReportRenderer(
        IOptions<BitbucketOptions> options,
        IHtmlReportFileStore htmlReportFileStore,
        IHtmlContentComposer htmlContentComposer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(htmlReportFileStore);
        ArgumentNullException.ThrowIfNull(htmlContentComposer);

        _settings = options.Value;
        _htmlReportFileStore = htmlReportFileStore;
        _htmlContentComposer = htmlContentComposer;
    }

    /// <inheritdoc />
    public void RenderReport(RepositoryPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var htmlSettings = new HtmlReportSettings(_settings.Html.Enabled, _settings.Html.OutputPath);
        if (!htmlSettings.Enabled)
        {
            return;
        }

        var outputPath = htmlSettings.ResolveOutputPath();
        var html = _htmlContentComposer.Compose(reportData);

        _htmlReportFileStore.Save(outputPath, html);
        AnsiConsole.MarkupLine($"[grey]HTML report saved:[/] {Markup.Escape(outputPath)}");
    }

    private readonly BitbucketOptions _settings;
    private readonly IHtmlReportFileStore _htmlReportFileStore;
    private readonly IHtmlContentComposer _htmlContentComposer;
}
