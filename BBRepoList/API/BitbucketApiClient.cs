using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Models;

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
    public BitbucketApiClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);

        _http = http;
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
    public async Task<RepoPage> GetRepositoriesPageAsync(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Bitbucket API error {(int)resp.StatusCode} {resp.ReasonPhrase}. Url={url}. Body={body}");
        }

        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<RepoPage>(json)
               ?? new RepoPage([], null);
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
        return JsonSerializer.Deserialize<BitbucketUser>(json)
               ?? new BitbucketUser(null, null, null, null);
    }

    private readonly HttpClient _http;
}
