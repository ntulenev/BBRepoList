namespace BBRepoList.Models;

/// <summary>
/// Aggregated data used by PDF report generation.
/// </summary>
public sealed class RepositoryPdfReportData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryPdfReportData"/> class.
    /// </summary>
    /// <param name="workspace">Workspace name.</param>
    /// <param name="filterPhrase">Filter phrase.</param>
    /// <param name="abandonedMonthsThreshold">Abandoned repository threshold in months.</param>
    /// <param name="generatedAt">Generation timestamp.</param>
    /// <param name="repositories">Repositories included in report.</param>
    public RepositoryPdfReportData(
        string workspace,
        string? filterPhrase,
        int abandonedMonthsThreshold,
        DateTimeOffset generatedAt,
        IReadOnlyList<Repository> repositories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        ArgumentNullException.ThrowIfNull(repositories);

        Workspace = workspace.Trim();
        FilterPhrase = string.IsNullOrWhiteSpace(filterPhrase) ? null : filterPhrase.Trim();
        AbandonedMonthsThreshold = abandonedMonthsThreshold;
        GeneratedAt = generatedAt;
        Repositories = repositories;
    }

    /// <summary>
    /// Workspace name.
    /// </summary>
    public string Workspace { get; }

    /// <summary>
    /// Optional filter phrase used by user.
    /// </summary>
    public string? FilterPhrase { get; }

    /// <summary>
    /// Abandoned repository threshold in months.
    /// </summary>
    public int AbandonedMonthsThreshold { get; }

    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Repositories included in report.
    /// </summary>
    public IReadOnlyList<Repository> Repositories { get; }
}
