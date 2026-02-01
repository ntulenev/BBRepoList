namespace BBRepoList.Models;

/// <summary>
/// Optional repository name filter.
/// </summary>
/// <param name="Phrase">Search phrase used for filtering.</param>
public readonly record struct FilterPattern(string? Phrase)
{
    /// <summary>
    /// Checks whether the repository matches this filter pattern.
    /// </summary>
    /// <param name="repository">Repository to check.</param>
    /// <returns>
    /// <see langword="true"/> when <see cref="Phrase"/> is <see langword="null"/>,
    /// otherwise whether repository name contains phrase (case-insensitive).
    /// </returns>
    public bool Filter(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return Phrase is null
            || repository.Name.Contains(Phrase, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether this filter pattern has a filter phrase.
    /// </summary>
    public bool HasFilter => string.IsNullOrWhiteSpace(Phrase);
}
