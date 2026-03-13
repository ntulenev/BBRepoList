namespace BBRepoList.Configuration;

/// <summary>
/// Raw HTML report options bound from configuration.
/// </summary>
public sealed class HtmlOptions
{
    /// <summary>
    /// Gets or sets whether HTML report generation is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets output file path for generated HTML report.
    /// </summary>
    public string OutputPath { get; init; } = "bbrepolist-open-pr-details.html";

    /// <summary>
    /// Gets or sets whether generated HTML report should be opened in the default browser.
    /// </summary>
    public bool OpenInBrowser { get; init; }
}
