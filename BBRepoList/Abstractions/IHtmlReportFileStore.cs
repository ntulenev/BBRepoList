namespace BBRepoList.Abstractions;

/// <summary>
/// Persists generated HTML report content.
/// </summary>
public interface IHtmlReportFileStore
{
    /// <summary>
    /// Saves generated HTML content to output path.
    /// </summary>
    /// <param name="outputPath">Resolved output path.</param>
    /// <param name="html">HTML content.</param>
    void Save(string outputPath, string html);
}
