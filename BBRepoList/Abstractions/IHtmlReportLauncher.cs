namespace BBRepoList.Abstractions;

/// <summary>
/// Opens generated HTML reports in the system default browser.
/// </summary>
public interface IHtmlReportLauncher
{
    /// <summary>
    /// Opens the generated HTML report.
    /// </summary>
    /// <param name="htmlReportPath">Absolute path to generated HTML report.</param>
    void Open(string htmlReportPath);
}
