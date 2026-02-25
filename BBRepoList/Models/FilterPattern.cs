namespace BBRepoList.Models;

/// <summary>
/// Optional repository name filter.
/// </summary>
/// <param name="Phrase">Search phrase used for filtering.</param>
/// <param name="SearchMode">Search mode for matching repository names.</param>
public readonly record struct FilterPattern(string? Phrase, RepositorySearchMode SearchMode = RepositorySearchMode.Contains)
{
    /// <summary>
    /// Checks whether the repository matches this filter pattern.
    /// </summary>
    /// <param name="repository">Repository to check.</param>
    /// <returns>
    /// <see langword="true"/> when <see cref="Phrase"/> is <see langword="null"/>,
    /// otherwise whether repository name matches by selected search mode (case-insensitive).
    /// </returns>
    public bool Filter(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!HasFilter)
        {
            return true;
        }

        return SearchMode switch
        {
            RepositorySearchMode.StartWith => repository.Name.StartsWith(Phrase!, StringComparison.OrdinalIgnoreCase),
            RepositorySearchMode.Contains => repository.Name.Contains(Phrase!, StringComparison.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException($"Unsupported search mode: {SearchMode}.")
        };
    }

    /// <summary>
    /// Checks whether this filter pattern has a filter phrase.
    /// </summary>
    public bool HasFilter => !string.IsNullOrWhiteSpace(Phrase);
}
