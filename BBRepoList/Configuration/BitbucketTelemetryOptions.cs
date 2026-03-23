namespace BBRepoList.Configuration;

/// <summary>
/// Configuration settings for Bitbucket request telemetry.
/// </summary>
public sealed class BitbucketTelemetryOptions
{
    /// <summary>
    /// Gets a value indicating whether Bitbucket request telemetry collection and console output are enabled.
    /// </summary>
    public bool Enabled { get; init; }
}
