using System.Globalization;
using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.Models;

namespace BBRepoList.API.Helpers;

/// <summary>
/// Bitbucket JSON parser for frequently used payload fragments.
/// </summary>
public sealed class BitbucketJsonParser : IBitbucketJsonParser
{
    /// <inheritdoc />
    public bool TryReadUuidFromObject(JsonElement element, out BitbucketId uuid)
    {
        uuid = default;

        if (element.ValueKind is not JsonValueKind.Object
            || !element.TryGetProperty("uuid", out var uuidElement)
            || uuidElement.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var rawUuid = uuidElement.GetString();
        if (string.IsNullOrWhiteSpace(rawUuid))
        {
            return false;
        }

        return BitbucketId.TryCreate(rawUuid, out uuid);
    }

    /// <inheritdoc />
    public bool TryReadDateTime(JsonElement element, out DateTimeOffset value)
    {
        value = default;

        if (element.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var raw = element.GetString();
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }

    /// <inheritdoc />
    public void AddActivityEntriesFromJson(
        JsonElement element,
        bool isCommentContext,
        Action<BitbucketId, DateTimeOffset, bool> onEntry)
    {
        ArgumentNullException.ThrowIfNull(onEntry);

        if (element.ValueKind == JsonValueKind.Object)
        {
            var currentScopeIsComment = isCommentContext;
            var actorId = default(BitbucketId?);
            var happenedOn = default(DateTimeOffset?);

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("comment", StringComparison.OrdinalIgnoreCase))
                {
                    currentScopeIsComment = true;
                }

                if (actorId is null
                    && (property.Name.Equals("user", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("actor", StringComparison.OrdinalIgnoreCase))
                    && TryReadUuidFromObject(property.Value, out var parsedUuid))
                {
                    actorId = parsedUuid;
                }

                if (happenedOn is null
                    && IsDateProperty(property.Name)
                    && TryReadDateTime(property.Value, out var parsedDate))
                {
                    happenedOn = parsedDate;
                }
            }

            if (actorId is not null && happenedOn is not null)
            {
                onEntry(actorId.Value, happenedOn.Value, currentScopeIsComment);
            }

            foreach (var property in element.EnumerateObject())
            {
                AddActivityEntriesFromJson(
                    property.Value,
                    currentScopeIsComment || property.Name.Equals("comment", StringComparison.OrdinalIgnoreCase),
                    onEntry);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AddActivityEntriesFromJson(item, isCommentContext, onEntry);
            }
        }
    }

    private static bool IsDateProperty(string propertyName) =>
        propertyName.Equals("date", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("created_on", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("updated_on", StringComparison.OrdinalIgnoreCase);
}
