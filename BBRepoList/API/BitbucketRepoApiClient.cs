using System.Runtime.CompilerServices;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client implementation for repository operations.
/// </summary>
public sealed class BitbucketRepoApiClient : IBitbucketRepoApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketRepoApiClient"/> class.
    /// </summary>
    /// <param name="transport">Bitbucket transport instance.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketRepoApiClient(IBitbucketTransport transport, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);

        _transport = transport;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Repository> GetRepositoriesAsync(
        FilterPattern filterPattern,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = BuildRepositoriesUri(filterPattern);

        while (url is not null)
        {
            var page = await GetRepositoriesPageAsync(url, cancellationToken).ConfigureAwait(false);

            foreach (var repository in page.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return repository;
            }

            url = page.Next;
        }
    }

    private Uri BuildRepositoriesUri(FilterPattern filterPattern)
    {
        var fields = Uri.EscapeDataString("values.name,values.slug,values.created_on,values.updated_on,next");
        var path = $"repositories/{_options.Workspace}?pagelen={_options.PageLen}&fields={fields}";
        var query = BuildRepositoryQuery(filterPattern);
        if (!string.IsNullOrWhiteSpace(query))
        {
            path += $"&q={Uri.EscapeDataString(query)}";
        }

        return new Uri(path, UriKind.Relative);
    }

    private static string? BuildRepositoryQuery(FilterPattern filterPattern)
    {
        if (!filterPattern.HasFilter)
        {
            return null;
        }

        return filterPattern.SearchMode switch
        {
            RepositorySearchMode.Contains => BuildNameQuery(filterPattern.Phrase!),
            RepositorySearchMode.StartWith => BuildNameQuery(filterPattern.Phrase!),
            _ => throw new InvalidOperationException($"Unsupported search mode: {filterPattern.SearchMode}.")
        };
    }

    private static string BuildNameQuery(string filterValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterValue);
        var escapedFilter = EscapeQueryStringLiteral(filterValue);
        return $"name ~ \"{escapedFilter}\"";
    }

    private static string EscapeQueryStringLiteral(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private async Task<RepoPage> GetRepositoriesPageAsync(Uri url, CancellationToken cancellationToken)
    {
        var dto = await _transport
            .GetAsync<RepoPageDto>(url, cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? new RepoPage([], null) : dto.ToDomain();
    }

    private readonly IBitbucketTransport _transport;
    private readonly BitbucketOptions _options;
}

