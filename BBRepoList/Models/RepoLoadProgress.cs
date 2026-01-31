namespace BBRepoList.Models;

/// <summary>
/// Progress snapshot for repository loading.
/// </summary>
/// <param name="Pages">Number of pages loaded so far.</param>
/// <param name="Seen">Total repositories seen so far.</param>
/// <param name="Matched">Total repositories matched so far.</param>
public sealed record RepoLoadProgress(int Pages, int Seen, int Matched);