using BBRepoList.Models;

using QuestPDF.Fluent;

namespace BBRepoList.Abstractions;

/// <summary>
/// Composes PDF content body for repository report.
/// </summary>
public interface IPdfContentComposer
{
    /// <summary>
    /// Composes content section for PDF report.
    /// </summary>
    /// <param name="column">QuestPDF column descriptor.</param>
    /// <param name="reportData">Aggregated report data.</param>
    void ComposeContent(ColumnDescriptor column, RepositoryPdfReportData reportData);
}
