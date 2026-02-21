using System.Globalization;

namespace BBRepoList.Configuration;

/// <summary>
/// Validated PDF report settings.
/// </summary>
public sealed record PdfReportSettings
{
    private const string DEFAULT_OUTPUT_PATH = "bbrepolist-report.pdf";

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfReportSettings"/> class.
    /// </summary>
    /// <param name="enabled">Whether PDF generation is enabled.</param>
    /// <param name="outputPath">Configured output path.</param>
    public PdfReportSettings(bool enabled = true, string? outputPath = null)
    {
        Enabled = enabled;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? DEFAULT_OUTPUT_PATH : outputPath.Trim();
    }

    /// <summary>
    /// Gets a value indicating whether PDF generation is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets configured output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Resolves output path to absolute path and appends date suffix.
    /// </summary>
    /// <returns>Absolute dated output path.</returns>
    public string ResolveOutputPath()
    {
        var candidatePath = string.IsNullOrWhiteSpace(OutputPath)
            ? DEFAULT_OUTPUT_PATH
            : OutputPath.Trim();

        var absolutePath = Path.IsPathRooted(candidatePath)
            ? Path.GetFullPath(candidatePath)
            : Path.GetFullPath(candidatePath, Directory.GetCurrentDirectory());

        return AppendDateSuffix(absolutePath, DateTime.Now);
    }

    private static string AppendDateSuffix(string absolutePath, DateTime currentDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var directoryPath = Path.GetDirectoryName(absolutePath);
        var extension = Path.GetExtension(absolutePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
        var dateSuffix = currentDate.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var datedFileName = fileNameWithoutExtension + "_" + dateSuffix + extension;

        return string.IsNullOrWhiteSpace(directoryPath)
            ? datedFileName
            : Path.Combine(directoryPath, datedFileName);
    }
}
