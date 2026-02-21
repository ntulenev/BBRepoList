namespace BBRepoList.Configuration;

/// <summary>
/// Raw PDF report options bound from configuration.
/// </summary>
public sealed class PdfOptions
{
    /// <summary>
    /// Gets or sets whether PDF report generation is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets output file path for generated PDF report.
    /// </summary>
    public string OutputPath { get; init; } = "bbrepolist-report.pdf";
}
