using System.Globalization;

namespace BBRepoList.Configuration;

/// <summary>
/// Validated HTML report settings.
/// </summary>
public sealed record HtmlReportSettings
{
    private const string DEFAULT_OUTPUT_PATH = "bbrepolist-open-pr-details.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlReportSettings"/> class.
    /// </summary>
    /// <param name="enabled">Whether HTML generation is enabled.</param>
    /// <param name="outputPath">Configured output path.</param>
    /// <param name="openInBrowser">Whether generated HTML should be opened in the default browser.</param>
    public HtmlReportSettings(bool enabled = true, string? outputPath = null, bool openInBrowser = false)
    {
        Enabled = enabled;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? DEFAULT_OUTPUT_PATH : outputPath.Trim();
        OpenInBrowser = openInBrowser;
    }

    /// <summary>
    /// Gets a value indicating whether HTML generation is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets configured output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets a value indicating whether generated HTML should be opened in the default browser.
    /// </summary>
    public bool OpenInBrowser { get; }

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
