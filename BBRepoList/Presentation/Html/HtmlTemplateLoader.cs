namespace BBRepoList.Presentation.Html;

/// <summary>
/// Loads embedded HTML templates used by the report composer.
/// </summary>
internal static class HtmlTemplateLoader
{
    private static readonly Lazy<string> _reportTemplate = new(() => LoadTemplate("BBRepoList.HtmlTemplates.ReportDocument.html"));
    private static readonly Lazy<string> _pullRequestRowTemplate = new(() => LoadTemplate("BBRepoList.HtmlTemplates.PullRequestRow.html"));

    public static string LoadReportTemplate() => _reportTemplate.Value;

    public static string LoadPullRequestRowTemplate() => _pullRequestRowTemplate.Value;

    private static string LoadTemplate(string resourceName)
    {
        var assembly = typeof(HtmlTemplateLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded HTML template '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
