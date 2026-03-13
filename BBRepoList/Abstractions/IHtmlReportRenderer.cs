using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Renders repository HTML report.
/// </summary>
public interface IHtmlReportRenderer
{
    /// <summary>
    /// Renders and saves HTML report.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    void RenderReport(RepositoryPdfReportData reportData);
}
