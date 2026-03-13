using System.Text;

using BBRepoList.Abstractions;

namespace BBRepoList.Presentation.Html;

/// <summary>
/// Filesystem-backed HTML report store.
/// </summary>
public sealed class HtmlReportFileStore : IHtmlReportFileStore
{
    /// <inheritdoc />
    public void Save(string outputPath, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(html);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, html, Encoding.UTF8);
    }
}
