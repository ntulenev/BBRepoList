using System.Diagnostics;

using BBRepoList.Abstractions;

namespace BBRepoList.Presentation.Html;

/// <summary>
/// Default implementation that opens HTML reports via the shell.
/// </summary>
public sealed class HtmlReportLauncher : IHtmlReportLauncher
{
    /// <inheritdoc />
    public void Open(string htmlReportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlReportPath);

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = htmlReportPath,
            UseShellExecute = true
        });
    }
}
