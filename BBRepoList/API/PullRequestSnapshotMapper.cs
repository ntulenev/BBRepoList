using System.Globalization;

using BBRepoList.Abstractions;
using BBRepoList.Models;
using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// Default Bitbucket pull request snapshot mapper.
/// </summary>
public sealed class PullRequestSnapshotMapper : IPullRequestSnapshotMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestSnapshotMapper"/> class.
    /// </summary>
    /// <param name="jsonParser">Bitbucket JSON parser.</param>
    /// <param name="fingerprintBuilder">Pull request fingerprint builder.</param>
    public PullRequestSnapshotMapper(
        IBitbucketJsonParser jsonParser,
        IPullRequestFingerprintBuilder fingerprintBuilder)
    {
        ArgumentNullException.ThrowIfNull(jsonParser);
        ArgumentNullException.ThrowIfNull(fingerprintBuilder);

        _jsonParser = jsonParser;
        _fingerprintBuilder = fingerprintBuilder;
    }

    /// <inheritdoc />
    public PullRequestSnapshot CreateSnapshot(PullRequestDto pullRequestDto, BitbucketId currentUserId)
    {
        ArgumentNullException.ThrowIfNull(pullRequestDto);

        var authorId = BitbucketId.TryCreate(pullRequestDto.Author?.Uuid, out var parsedAuthorId)
            ? parsedAuthorId
            : (BitbucketId?)null;
        var descriptionText = string.IsNullOrWhiteSpace(pullRequestDto.Description)
            ? pullRequestDto.Summary?.Raw
            : pullRequestDto.Description;
        var (requestChangesCount, hasCurrentUserRequestChanges, approvalsCount, hasCurrentUserApproval) =
            GetPullRequestReviewState(pullRequestDto.Participants, currentUserId);

        return new PullRequestSnapshot(
            pullRequestDto.Id!.Value,
            string.IsNullOrWhiteSpace(pullRequestDto.Title)
                ? $"PR-{pullRequestDto.Id.Value.ToString(CultureInfo.InvariantCulture)}"
                : pullRequestDto.Title.Trim(),
            pullRequestDto.CreatedOn!.Value,
            descriptionText,
            authorId,
            pullRequestDto.Author?.DisplayName,
            requestChangesCount,
            hasCurrentUserRequestChanges,
            approvalsCount,
            hasCurrentUserApproval,
            _fingerprintBuilder.BuildFingerprint(pullRequestDto));
    }

    private (int RequestChangesCount, bool HasCurrentUserRequestChanges, int ApprovalsCount, bool HasCurrentUserApproval)
        GetPullRequestReviewState(
        ICollection<PullRequestParticipantDto>? participants,
        BitbucketId currentUserId)
    {
        if (participants is null || participants.Count == 0)
        {
            return default;
        }

        var requestChangesCount = 0;
        var hasCurrentUserRequestChanges = false;
        var approvalsCount = 0;
        var hasCurrentUserApproval = false;

        foreach (var participant in participants)
        {
            if (!BitbucketId.TryCreate(participant.User?.Uuid, out var participantId))
            {
                continue;
            }

            if (_jsonParser.IsRequestChangesState(participant.State))
            {
                requestChangesCount++;
                hasCurrentUserRequestChanges |= participantId == currentUserId;
            }

            if (_jsonParser.IsApprovalState(participant))
            {
                approvalsCount++;
                hasCurrentUserApproval |= participantId == currentUserId;
            }
        }

        return (requestChangesCount, hasCurrentUserRequestChanges, approvalsCount, hasCurrentUserApproval);
    }

    private readonly IBitbucketJsonParser _jsonParser;
    private readonly IPullRequestFingerprintBuilder _fingerprintBuilder;
}
