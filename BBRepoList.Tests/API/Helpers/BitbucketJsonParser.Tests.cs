using System.Text.Json;

using BBRepoList.API.Helpers;
using BBRepoList.Models;
using BBRepoList.Transport;

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

    [Theory(DisplayName = "IsRequestChangesState recognizes supported request-changes variants")]
    [InlineData("changes_requested")]
    [InlineData("changes requested")]
    [InlineData("changes-requested")]
    [InlineData("requested_changes")]
    [InlineData("request_changes")]
    [InlineData("needs_work")]
    [InlineData("needs work")]
    [InlineData("CHANGES_REQUESTED")]
    [Trait("Category", "Unit")]
    public void IsRequestChangesStateWhenStateMatchesSupportedVariantReturnsTrue(string state)
    {
        // Arrange
        var parser = new BitbucketJsonParser();

        // Act
        var result = parser.IsRequestChangesState(state);

        // Assert
        result.Should().BeTrue();
    }

    [Theory(DisplayName = "IsRequestChangesState returns false for unsupported values")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("approved")]
    [InlineData("commented")]
    [Trait("Category", "Unit")]
    public void IsRequestChangesStateWhenStateDoesNotMatchReturnsFalse(string? state)
    {
        // Arrange
        var parser = new BitbucketJsonParser();

        // Act
        var result = parser.IsRequestChangesState(state);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsApprovalState returns true when approved flag is set")]
    [Trait("Category", "Unit")]
    public void IsApprovalStateWhenApprovedFlagIsSetReturnsTrue()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var participant = new PullRequestParticipantDto(
            User: new PullRequestAuthorDto("{user-1}", "User 1"),
            Approved: true);

        // Act
        var result = parser.IsApprovalState(participant);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsApprovalState returns true when state is approved")]
    [Trait("Category", "Unit")]
    public void IsApprovalStateWhenStateIsApprovedReturnsTrue()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var participant = new PullRequestParticipantDto(
            User: new PullRequestAuthorDto("{user-1}", "User 1"),
            State: "approved");

        // Act
        var result = parser.IsApprovalState(participant);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsApprovalState returns false when participant is not approved")]
    [Trait("Category", "Unit")]
    public void IsApprovalStateWhenParticipantIsNotApprovedReturnsFalse()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        var participant = new PullRequestParticipantDto(
            User: new PullRequestAuthorDto("{user-1}", "User 1"),
            State: "changes_requested");

        // Act
        var result = parser.IsApprovalState(participant);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsApprovalState throws when participant is null")]
    [Trait("Category", "Unit")]
    public void IsApprovalStateWhenParticipantIsNullThrowsArgumentNullException()
    {
        // Arrange
        var parser = new BitbucketJsonParser();
        PullRequestParticipantDto participant = null!;

        // Act
        Action act = () => _ = parser.IsApprovalState(participant);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
