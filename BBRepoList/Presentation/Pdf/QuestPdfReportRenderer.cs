using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

using Spectre.Console;

using QLicenseType = QuestPDF.Infrastructure.LicenseType;

namespace BBRepoList.Presentation.Pdf;

/// <summary>
/// QuestPDF implementation for repository report rendering.
/// </summary>
public sealed class QuestPdfReportRenderer : IPdfReportRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestPdfReportRenderer"/> class.
    /// </summary>
    /// <param name="options">Bitbucket options.</param>
    /// <param name="pdfReportFileStore">PDF output file store.</param>
    /// <param name="pdfContentComposer">PDF content composer.</param>
    public QuestPdfReportRenderer(
        IOptions<BitbucketOptions> options,
        IPdfReportFileStore pdfReportFileStore,
        IPdfContentComposer pdfContentComposer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(pdfReportFileStore);
        ArgumentNullException.ThrowIfNull(pdfContentComposer);

        _settings = options.Value;
        _pdfReportFileStore = pdfReportFileStore;
        _pdfContentComposer = pdfContentComposer;
    }

    /// <inheritdoc />
    public void RenderReport(RepositoryPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var pdfSettings = new PdfReportSettings(_settings.Pdf.Enabled, _settings.Pdf.OutputPath);
        if (!pdfSettings.Enabled)
        {
            return;
        }

        var outputPath = pdfSettings.ResolveOutputPath();

        QuestPDF.Settings.License = QLicenseType.Community;

        var document = Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(static style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Spacing(2);
                    _ = column.Item().Text("Bitbucket Repository Report").Bold().FontSize(16);
                    _ = column.Item().Text(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Generated: {0:yyyy-MM-dd HH:mm:ss zzz}",
                            reportData.GeneratedAt));
                    _ = column.Item().Text("Workspace: " + reportData.Workspace);
                    _ = column.Item()
                        .Text("Filter: " + (string.IsNullOrWhiteSpace(reportData.FilterPhrase) ? "(none)" : reportData.FilterPhrase));
                    _ = column.Item().Text("Results: " + reportData.Repositories.Count.ToString(CultureInfo.InvariantCulture));
                });

                page.Content().PaddingTop(8).Column(column => _pdfContentComposer.ComposeContent(column, reportData));

                page.Footer().AlignRight().Text(text =>
                {
                    _ = text.Span("Page ");
                    _ = text.CurrentPageNumber();
                    _ = text.Span(" / ");
                    _ = text.TotalPages();
                });
            });
        });

        _pdfReportFileStore.Save(outputPath, document);
        AnsiConsole.MarkupLine($"[grey]PDF report saved:[/] {Markup.Escape(outputPath)}");
    }

    private readonly BitbucketOptions _settings;
    private readonly IPdfReportFileStore _pdfReportFileStore;
    private readonly IPdfContentComposer _pdfContentComposer;
}
