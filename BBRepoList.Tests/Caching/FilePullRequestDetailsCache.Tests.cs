using System.Text.Json;

using BBRepoList.Caching;
using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Tests.Caching;

public sealed class FilePullRequestDetailsCacheTests
{
    [Fact(DisplayName = "SaveEntriesAsync throws when entries are null")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var cache = new FilePullRequestDetailsCache();
        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries = null!;

        // Act
        Func<Task> act = () => cache.SaveEntriesAsync(
            "workspace",
            "repo-1",
            new BitbucketId("{current-user}"),
            entries,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "SaveEntriesAsync and ReadEntriesAsync round-trip valid entries in pull request order")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreValidPersistsAndReadsEntriesSortedByPullRequestId()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var currentUserId = new BitbucketId("{current-user}");
        var entries = new[]
        {
            new PullRequestDetailsCacheEntry(
                300,
                "fingerprint-300",
                new DateTimeOffset(2026, 3, 20, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.Zero),
                false,
                0),
            new PullRequestDetailsCacheEntry(
                100,
                "fingerprint-100",
                null,
                new DateTimeOffset(2026, 3, 19, 12, 0, 0, TimeSpan.Zero),
                true,
                4)
        };

        // Act
        await cache.SaveEntriesAsync("workspace", "repo-1", currentUserId, entries, CancellationToken.None);
        var result = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);

        // Assert
        result.Should().Equal(
            new PullRequestDetailsCacheEntry(
                100,
                "fingerprint-100",
                null,
                new DateTimeOffset(2026, 3, 19, 12, 0, 0, TimeSpan.Zero),
                true,
                4),
            new PullRequestDetailsCacheEntry(
                300,
                "fingerprint-300",
                new DateTimeOffset(2026, 3, 20, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.Zero),
                false,
                0));
    }

    [Fact(DisplayName = "SaveEntriesAsync filters invalid entries before persisting")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesContainInvalidItemsPersistsOnlyValidEntries()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var currentUserId = new BitbucketId("{current-user}");
        var validEntry = new PullRequestDetailsCacheEntry(
            200,
            "fingerprint-200",
            new DateTimeOffset(2026, 3, 18, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 18, 11, 0, 0, TimeSpan.Zero),
            true,
            2);

        IReadOnlyCollection<PullRequestDetailsCacheEntry> entries =
        [
            new PullRequestDetailsCacheEntry(0, "fingerprint-0", null, null, false, 0),
            new PullRequestDetailsCacheEntry(100, "   ", null, null, false, 1),
            new PullRequestDetailsCacheEntry(150, "fingerprint-150", null, null, false, -1),
            validEntry
        ];

        // Act
        await cache.SaveEntriesAsync("workspace", "repo-1", currentUserId, entries, CancellationToken.None);
        var result = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);

        // Assert
        result.Should().Equal(validEntry);
        Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Should().ContainSingle();
    }

    [Fact(DisplayName = "ReadEntriesAsync returns empty list when cache document is invalid")]
    [Trait("Category", "Unit")]
    public async Task ReadEntriesAsyncWhenCacheDocumentIsInvalidReturnsEmptyList()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var currentUserId = new BitbucketId("{current-user}");
        await cache.SaveEntriesAsync(
            "workspace",
            "repo-1",
            currentUserId,
            [new PullRequestDetailsCacheEntry(100, "fingerprint-100", null, null, false, 1)],
            CancellationToken.None);

        var cacheFilePath = Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(
            cacheFilePath,
            JsonSerializer.Serialize(new
            {
                Version = 999,
                Entries = new[]
                {
                    new
                    {
                        PullRequestId = 100,
                        Fingerprint = "fingerprint-100",
                        FirstNonAuthorActivityOn = (DateTimeOffset?)null,
                        LastActivityOn = (DateTimeOffset?)null,
                        HasCurrentUserDiscussion = false,
                        CommentsCount = 1
                    }
                }
            }),
            CancellationToken.None);

        // Act
        var unsupportedVersionResult = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);
        await File.WriteAllTextAsync(cacheFilePath, "{ bad json", CancellationToken.None);
        var malformedJsonResult = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);

        // Assert
        unsupportedVersionResult.Should().BeEmpty();
        malformedJsonResult.Should().BeEmpty();
    }

    [Fact(DisplayName = "SaveEntriesAsync deletes cache file when entries are empty")]
    [Trait("Category", "Unit")]
    public async Task SaveEntriesAsyncWhenEntriesAreEmptyDeletesExistingCacheFile()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var currentUserId = new BitbucketId("{current-user}");
        await cache.SaveEntriesAsync(
            "workspace",
            "repo-1",
            currentUserId,
            [new PullRequestDetailsCacheEntry(100, "fingerprint-100", null, null, false, 1)],
            CancellationToken.None);

        // Act
        await cache.SaveEntriesAsync("workspace", "repo-1", currentUserId, [], CancellationToken.None);
        var result = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteAsync removes persisted cache file")]
    [Trait("Category", "Unit")]
    public async Task DeleteAsyncWhenCacheExistsRemovesPersistedFile()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        var cache = new FilePullRequestDetailsCache(cacheDirectory.Path);
        var currentUserId = new BitbucketId("{current-user}");
        await cache.SaveEntriesAsync(
            "workspace",
            "repo-1",
            currentUserId,
            [new PullRequestDetailsCacheEntry(100, "fingerprint-100", null, null, false, 1)],
            CancellationToken.None);

        // Act
        await cache.DeleteAsync("workspace", "repo-1", currentUserId, CancellationToken.None);
        var result = await cache.ReadEntriesAsync("workspace", "repo-1", currentUserId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        Directory.GetFiles(cacheDirectory.Path, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BBRepoList.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
