using System.Collections.Concurrent;
using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

namespace BBRepoList.Telemetry;

/// <summary>
/// Thread-safe Bitbucket API telemetry collector.
/// </summary>
public sealed class BitbucketTelemetryService : IBitbucketTelemetryService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketTelemetryService"/> class.
    /// </summary>
    /// <param name="options">Bitbucket configuration options.</param>
    public BitbucketTelemetryService(IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _isEnabled = options.Value.Telemetry.Enabled;
    }

    /// <inheritdoc />
    public void TrackRequest(Uri requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        if (!_isEnabled)
        {
            return;
        }

        var apiName = NormalizeApiName(requestUri);
        IncrementRequestCount(apiName);
    }

    /// <inheritdoc />
    public BitbucketTelemetrySnapshot GetSnapshot()
    {
        if (!_isEnabled)
        {
            return new BitbucketTelemetrySnapshot(false, 0, []);
        }

        var requestStatistics = _requestCounts
            .Select(static pair => new BitbucketApiRequestStatistic(pair.Key, pair.Value))
            .OrderByDescending(static statistic => statistic.RequestCount)
            .ThenBy(static statistic => statistic.ApiName, StringComparer.Ordinal)
            .ToArray();

        return new BitbucketTelemetrySnapshot(
            true,
            requestStatistics.Sum(static statistic => statistic.RequestCount),
            requestStatistics);
    }

    private void IncrementRequestCount(string apiName)
    {
        _ = _requestCounts.AddOrUpdate(
            apiName,
            static _ => 1,
            static (_, currentCount) => currentCount + 1);
    }

    private static string NormalizeApiName(Uri requestUri)
    {
        var requestTarget = requestUri.IsAbsoluteUri ? requestUri.PathAndQuery : requestUri.OriginalString;
        var requestTargetParts = requestTarget.Split('?', count: 2, StringSplitOptions.TrimEntries);
        var path = requestTargetParts[0].Trim('/');
        var query = requestTargetParts.Length == 2 ? requestTargetParts[1] : string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return "GET /";
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (segments.Count > 0 && segments[0].Equals("2.0", StringComparison.OrdinalIgnoreCase))
        {
            segments.RemoveAt(0);
        }

        if (segments.Count == 1 && segments[0].Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            return "GET /user";
        }

        if (segments.Count == 2 && segments[0].Equals("repositories", StringComparison.OrdinalIgnoreCase))
        {
            return "GET /repositories/{workspace}";
        }

        if (segments.Count == 4
            && segments[0].Equals("repositories", StringComparison.OrdinalIgnoreCase)
            && segments[3].Equals("pullrequests", StringComparison.OrdinalIgnoreCase))
        {
            return IsPullRequestCountRequest(query)
                ? "GET /repositories/{workspace}/{repository}/pullrequests (count)"
                : "GET /repositories/{workspace}/{repository}/pullrequests";
        }

        if (segments.Count == 6
            && segments[0].Equals("repositories", StringComparison.OrdinalIgnoreCase)
            && segments[3].Equals("pullrequests", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[4], NumberStyles.None, CultureInfo.InvariantCulture, out _)
            && segments[5].Equals("activity", StringComparison.OrdinalIgnoreCase))
        {
            return "GET /repositories/{workspace}/{repository}/pullrequests/{pullRequestId}/activity";
        }

        return $"GET /{string.Join('/', segments)}";
    }

    private static bool IsPullRequestCountRequest(string query) =>
        query.Contains("fields=size", StringComparison.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _requestCounts = new(StringComparer.Ordinal);
    private readonly bool _isEnabled;
}
