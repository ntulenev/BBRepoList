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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = new Uri($"repositories/{_options.Workspace}?pagelen={_options.PageLen}", UriKind.Relative);

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

