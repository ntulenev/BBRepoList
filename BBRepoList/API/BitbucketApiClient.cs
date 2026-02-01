using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using Microsoft.Extensions.Options;

namespace BBRepoList.API;

/// <summary>
/// Bitbucket REST API client implementation.
/// </summary>
public sealed class BitbucketApiClient : IBitbucketApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketApiClient"/> class.
    /// </summary>
    /// <param name="http">HTTP client instance.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketApiClient(HttpClient http, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);

        _http = http;
        _options = options.Value;
    }

    /// <summary>
    /// Builds a basic auth header value for Bitbucket API requests.
    /// </summary>
    /// <param name="authEmail">Authentication email.</param>
    /// <param name="authApiToken">Authentication API token.</param>
    /// <returns>Authorization header value.</returns>
    public static AuthenticationHeaderValue BuildAuthHeader(string authEmail, string authApiToken)
    {
        ArgumentNullException.ThrowIfNull(authEmail);
        ArgumentNullException.ThrowIfNull(authApiToken);

        var user = authEmail;
        var raw = $"{user}:{authApiToken}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", b64);
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

    /// <inheritdoc />
    public async Task<BitbucketUser> AuthSelfCheckAsync(CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync(new Uri("user", UriKind.Relative), cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Bitbucket API error {(int)resp.StatusCode} {resp.ReasonPhrase}. Url=user. Body={body}");
        }

        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<BitbucketUserDto>(json);
        return dto is null ? throw new InvalidOperationException("Bitbucket user response is empty.") : dto.ToDomain();
    }

    private async Task<RepoPage> GetRepositoriesPageAsync(Uri url, CancellationToken cancellationToken)
    {
        using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Bitbucket API error {(int)resp.StatusCode} {resp.ReasonPhrase}. Url={url}. Body={body}");
        }

        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<RepoPageDto>(json);
        return dto is null ? new RepoPage([], null) : dto.ToDomain();
    }

    private readonly HttpClient _http;
    private readonly BitbucketOptions _options;
}
