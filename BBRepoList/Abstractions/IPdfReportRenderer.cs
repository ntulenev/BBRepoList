using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Renders repository PDF report.
/// </summary>
public interface IPdfReportRenderer
{
    /// <summary>
    /// Renders and saves PDF report.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    void RenderReport(RepositoryPdfReportData reportData);
}
