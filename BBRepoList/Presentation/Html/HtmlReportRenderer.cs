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
    /// <param name="htmlReportLauncher">HTML report launcher.</param>
    /// <param name="timeProvider">Time provider for output path date suffixes.</param>
    public HtmlReportRenderer(
        IOptions<BitbucketOptions> options,
        IHtmlReportFileStore htmlReportFileStore,
        IHtmlContentComposer htmlContentComposer,
        IHtmlReportLauncher htmlReportLauncher,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(htmlReportFileStore);
        ArgumentNullException.ThrowIfNull(htmlContentComposer);
        ArgumentNullException.ThrowIfNull(htmlReportLauncher);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _settings = options.Value;
        _htmlReportFileStore = htmlReportFileStore;
        _htmlContentComposer = htmlContentComposer;
        _htmlReportLauncher = htmlReportLauncher;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void RenderReport(RepositoryReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var htmlSettings = new HtmlReportSettings(_settings.Html.Enabled, _settings.Html.OutputPath, _settings.Html.OpenInBrowser);
        if (!htmlSettings.Enabled)
        {
            return;
        }

        var outputPath = htmlSettings.ResolveOutputPath(_timeProvider);
        var html = _htmlContentComposer.Compose(reportData);

        _htmlReportFileStore.Save(outputPath, html);
        AnsiConsole.MarkupLine($"[grey]HTML report saved:[/] {Markup.Escape(outputPath)}");

        if (!htmlSettings.OpenInBrowser)
        {
            return;
        }

        _htmlReportLauncher.Open(outputPath);
        AnsiConsole.MarkupLine("[grey]HTML report opened in default browser.[/]");
    }

    private readonly BitbucketOptions _settings;
    private readonly IHtmlReportFileStore _htmlReportFileStore;
    private readonly IHtmlContentComposer _htmlContentComposer;
    private readonly IHtmlReportLauncher _htmlReportLauncher;
    private readonly TimeProvider _timeProvider;
}
