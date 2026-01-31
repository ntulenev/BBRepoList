using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Logic;

/// <summary>
/// Loads repositories from Bitbucket with optional name filtering.
/// </summary>
public sealed class RepositoryService : IRepoService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryService"/> class.
    /// </summary>
    /// <param name="api">Bitbucket API client.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public RepositoryService(IBitbucketApiClient api, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(options);

        _api = api;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(
        string? searchPhrase,
        IProgress<RepoLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var all = new List<Repository>();
        var hasFilter = !string.IsNullOrWhiteSpace(searchPhrase);
        var url = new Uri($"repositories/{_options.Workspace}?pagelen={_options.PageLen}", UriKind.Relative);

        var pages = 0;
        var seen = 0;
        var matched = 0;

        while (url is not null)
        {
            pages++;

            var page = await _api.GetRepositoriesPageAsync(url, cancellationToken).ConfigureAwait(false);
            seen += page.Values.Count;

            if (hasFilter)
            {
                foreach (var r in page.Values)
                {
                    if (r.Name.Contains(searchPhrase!, StringComparison.OrdinalIgnoreCase))
                    {
                        all.Add(r);
                        matched++;
                    }
                }
            }
            else
            {
                all.AddRange(page.Values);
                matched = all.Count;
            }

            progress?.Report(new RepoLoadProgress(pages, seen, matched));
            url = page.Next;
        }

        return all;
    }

    private readonly IBitbucketApiClient _api;
    private readonly BitbucketOptions _options;
}
