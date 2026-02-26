using System.Text.Json;

using BBRepoList.API.Helpers;
using BBRepoList.Models;

using FluentAssertions;

namespace BBRepoList.Tests.API.Helpers;

public sealed class BitbucketJsonParserTests
{
    [Fact(DisplayName = "TryReadUuidFromObject returns true when uuid is valid")]
    [Trait("Category", "Unit")]
    public void TryReadUuidFromObjectWhenUuidIsValidReturnsTrue()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("""{ "uuid": " {user-1} " }""");

        // Act
        var success = parser.TryReadUuidFromObject(element, out var uuid);

        // Assert
        success.Should().BeTrue();
        uuid.Should().Be(new BitbucketId("{user-1}"));
    }

    [Fact(DisplayName = "TryReadUuidFromObject returns false when uuid is missing")]
    [Trait("Category", "Unit")]
    public void TryReadUuidFromObjectWhenUuidIsMissingReturnsFalse()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("""{ "id": "x" }""");

        // Act
        var success = parser.TryReadUuidFromObject(element, out var uuid);

        // Assert
        success.Should().BeFalse();
        uuid.Should().Be(default(BitbucketId));
    }

    [Fact(DisplayName = "TryReadUuidFromObject returns false when element is not object")]
    [Trait("Category", "Unit")]
    public void TryReadUuidFromObjectWhenElementIsNotObjectReturnsFalse()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("\"{user-1}\"");

        // Act
        var success = parser.TryReadUuidFromObject(element, out var uuid);

        // Assert
        success.Should().BeFalse();
        uuid.Should().Be(default(BitbucketId));
    }

    [Fact(DisplayName = "TryReadDateTime returns true for valid string")]
    [Trait("Category", "Unit")]
    public void TryReadDateTimeWhenStringIsValidReturnsTrue()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("\" 2026-02-24T10:00:00+00:00 \"");

        // Act
        var success = parser.TryReadDateTime(element, out var value);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "TryReadDateTime returns false for non-string element")]
    [Trait("Category", "Unit")]
    public void TryReadDateTimeWhenElementIsNotStringReturnsFalse()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("{}");

        // Act
        var success = parser.TryReadDateTime(element, out var value);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson adds one entry from object")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenObjectHasUserAndDateAddsEntry()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var entries = new List<(BitbucketId ActorId, DateTimeOffset HappenedOn, bool IsComment)>();
        var element = ParseJsonElement(
            """
            {
              "user": { "uuid": "{user-1}" },
              "date": "2026-02-24T10:00:00+00:00"
            }
            """);

        // Act
        parser.AddActivityEntriesFromJson(
            element,
            isCommentContext: false,
            (actorId, happenedOn, isComment) => entries.Add((actorId, happenedOn, isComment)));

        // Assert
        entries.Should().ContainSingle();
        entries[0].ActorId.Should().Be(new BitbucketId("{user-1}"));
        entries[0].HappenedOn.Should().Be(new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero));
        entries[0].IsComment.Should().BeFalse();
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson propagates comment scope")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenNestedCommentMarksEntryAsComment()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var entries = new List<(BitbucketId ActorId, DateTimeOffset HappenedOn, bool IsComment)>();
        var element = ParseJsonElement(
            """
            {
              "comment": {
                "user": { "uuid": "{user-2}" },
                "created_on": "2026-02-24T11:00:00+00:00"
              }
            }
            """);

        // Act
        parser.AddActivityEntriesFromJson(
            element,
            isCommentContext: false,
            (actorId, happenedOn, isComment) => entries.Add((actorId, happenedOn, isComment)));

        // Assert
        entries.Should().ContainSingle();
        entries[0].ActorId.Should().Be(new BitbucketId("{user-2}"));
        entries[0].HappenedOn.Should().Be(new DateTimeOffset(2026, 2, 24, 11, 0, 0, TimeSpan.Zero));
        entries[0].IsComment.Should().BeTrue();
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson parses array recursively")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenArrayContainsObjectsParsesEachEntry()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var entries = new List<(BitbucketId ActorId, DateTimeOffset HappenedOn, bool IsComment)>();
        var element = ParseJsonElement(
            """
            [
              {
                "actor": { "uuid": "{user-1}" },
                "updated_on": "2026-02-24T12:00:00+00:00"
              },
              {
                "user": { "uuid": "{user-2}" },
                "date": "2026-02-24T13:00:00+00:00"
              }
            ]
            """);

        // Act
        parser.AddActivityEntriesFromJson(
            element,
            isCommentContext: false,
            (actorId, happenedOn, isComment) => entries.Add((actorId, happenedOn, isComment)));

        // Assert
        entries.Should().HaveCount(2);
        entries.Select(static x => x.ActorId).Should().ContainInOrder(
            new BitbucketId("{user-1}"),
            new BitbucketId("{user-2}"));
    }

    [Fact(DisplayName = "AddActivityEntriesFromJson throws when callback is null")]
    [Trait("Category", "Unit")]
    public void AddActivityEntriesFromJsonWhenCallbackIsNullThrowsArgumentNullException()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var element = ParseJsonElement("{}");
        Action<BitbucketId, DateTimeOffset, bool> onEntry = null!;

        // Act
        Action act = () => parser.AddActivityEntriesFromJson(element, isCommentContext: false, onEntry);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
