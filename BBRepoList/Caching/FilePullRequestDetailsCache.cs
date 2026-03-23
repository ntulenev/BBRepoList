using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.Caching;

/// <summary>
/// File-based pull request details cache stored under local application data.
/// </summary>
public sealed class FilePullRequestDetailsCache : IPullRequestDetailsCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilePullRequestDetailsCache"/> class.
    /// </summary>
    /// <param name="cacheRootDirectory">Optional custom cache root directory.</param>
    public FilePullRequestDetailsCache(string? cacheRootDirectory = null)
    {
        _cacheRootDirectory = string.IsNullOrWhiteSpace(cacheRootDirectory)
            ? Path.Combine(
                AppContext.BaseDirectory,
                "cache",
                "pull-request-details")
            : cacheRootDirectory.Trim();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestDetailsCacheEntry>> ReadEntriesAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return [];
        }

        var cacheFilePath = GetCacheFilePath(workspace, repositorySlug, currentUserId);
        if (!File.Exists(cacheFilePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize<PullRequestDetailsCacheDocument>(json, _serializerOptions);
            if (document is null || document.Version != CURRENT_VERSION || document.Entries is null)
            {
                return [];
            }

            return
            [
                .. document.Entries
                    .Where(static entry => entry is not null)
                    .OfType<PullRequestDetailsCacheEntry>()
                    .Where(static entry =>
                        entry.PullRequestId > 0
                        && !string.IsNullOrWhiteSpace(entry.Fingerprint)
                        && entry.CommentsCount >= 0)
                    .OrderBy(static entry => entry.PullRequestId)
            ];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task SaveEntriesAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return;
        }

        if (entries.Count == 0)
        {
            await DeleteAsync(workspace, repositorySlug, currentUserId, cancellationToken).ConfigureAwait(false);
            return;
        }

        var cacheFilePath = GetCacheFilePath(workspace, repositorySlug, currentUserId);
        var cacheDirectory = Path.GetDirectoryName(cacheFilePath);
        if (string.IsNullOrWhiteSpace(cacheDirectory))
        {
            return;
        }

        try
        {
            _ = Directory.CreateDirectory(cacheDirectory);

            var validEntries = entries
                .Where(static entry => entry.PullRequestId > 0
                                       && !string.IsNullOrWhiteSpace(entry.Fingerprint)
                                       && entry.CommentsCount >= 0)
                .OrderBy(static entry => entry.PullRequestId)
                .ToArray();

            if (validEntries.Length == 0)
            {
                await DeleteAsync(workspace, repositorySlug, currentUserId, cancellationToken).ConfigureAwait(false);
                return;
            }

            var document = new PullRequestDetailsCacheDocument
            {
                Version = CURRENT_VERSION,
                Entries = validEntries
            };

            var tempFilePath = $"{cacheFilePath}.{Guid.NewGuid():N}.tmp";
            var json = JsonSerializer.Serialize(document, _serializerOptions);
            await File.WriteAllTextAsync(tempFilePath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempFilePath, cacheFilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return;
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string workspace,
        string repositorySlug,
        BitbucketId currentUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repositorySlug))
        {
            return Task.CompletedTask;
        }

        try
        {
            var cacheFilePath = GetCacheFilePath(workspace, repositorySlug, currentUserId);
            if (File.Exists(cacheFilePath))
            {
                File.Delete(cacheFilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private string GetCacheFilePath(string workspace, string repositorySlug, BitbucketId currentUserId)
    {
        var workspaceSegment = GetHashSegment(workspace);
        var userSegment = GetHashSegment(currentUserId.Value);
        var repositorySegment = GetHashSegment(repositorySlug);

        return Path.Combine(_cacheRootDirectory, workspaceSegment, userSegment, $"{repositorySegment}.json");
    }

    private static string GetHashSegment(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }

    private const int CURRENT_VERSION = 1;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _cacheRootDirectory;

    private sealed class PullRequestDetailsCacheDocument
    {
        public int Version { get; init; }

        public IReadOnlyList<PullRequestDetailsCacheEntry>? Entries { get; init; }
    }
}
