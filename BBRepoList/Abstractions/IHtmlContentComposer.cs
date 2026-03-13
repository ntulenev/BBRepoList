using BBRepoList.Models;

namespace BBRepoList.Abstractions;

/// <summary>
/// Composes HTML content for repository reports.
/// </summary>
public interface IHtmlContentComposer
{
    /// <summary>
    /// Builds a complete HTML document for the provided report data.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    /// <returns>HTML document text.</returns>
    string Compose(RepositoryPdfReportData reportData);
}
