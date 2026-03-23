using System.Text.Json;

using BBRepoList.Abstractions;
namespace BBRepoList.Transport;

/// <summary>
/// HTTP transport implementation for Bitbucket API.
/// </summary>
public sealed class BitbucketTransport : IBitbucketTransport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketTransport"/> class.
    /// </summary>
    /// <param name="http">HTTP client instance.</param>
    /// <param name="retryPolicy">Retry policy instance.</param>
    /// <param name="telemetryService">Telemetry service instance.</param>
    public BitbucketTransport(
        HttpClient http,
        IBitbucketRetryPolicy retryPolicy,
        IBitbucketTelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(telemetryService);
        _http = http;
        _retryPolicy = retryPolicy;
        _telemetryService = telemetryService;
    }

    /// <inheritdoc />
    public async Task<TDto?> GetAsync<TDto>(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        var attempt = 0;

        while (true)
        {
            try
            {
                _telemetryService.TrackRequest(url);
                using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Deserialize<TDto>(json);
                }

                if (_retryPolicy.TryGetDelay(attempt + 1, resp.StatusCode, null, out var delay))
                {
                    attempt++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Bitbucket API error {(int)resp.StatusCode} {resp.ReasonPhrase}. Url={url}. Body={body}");
            }
            catch (HttpRequestException ex) when (_retryPolicy.TryGetDelay(attempt + 1, null, ex, out var delay))
            {
                attempt++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private readonly HttpClient _http;
    private readonly IBitbucketRetryPolicy _retryPolicy;
    private readonly IBitbucketTelemetryService _telemetryService;
}
