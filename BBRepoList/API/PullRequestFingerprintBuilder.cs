using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BBRepoList.Transport;

namespace BBRepoList.API;

/// <summary>
/// SHA-256 based pull request fingerprint builder.
/// </summary>
public sealed class PullRequestFingerprintBuilder : IPullRequestFingerprintBuilder
{
    /// <inheritdoc />
    public string BuildFingerprint(PullRequestDto pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        var participantReviewState = string.Join(
            ';',
            (pullRequest.Participants ?? [])
                .Select(static participant => string.Join(
                    '|',
                    participant.User?.Uuid?.Trim() ?? string.Empty,
                    participant.State?.Trim() ?? string.Empty,
                    participant.Approved == true ? "1" : "0"))
                .OrderBy(static value => value, StringComparer.Ordinal));

        var rawFingerprint = string.Join(
            '\n',
            (pullRequest.Id ?? 0).ToString(CultureInfo.InvariantCulture),
            pullRequest.UpdatedOn?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            pullRequest.State?.Trim() ?? string.Empty,
            pullRequest.Source?.Commit?.Hash?.Trim() ?? string.Empty,
            pullRequest.CommentCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            pullRequest.TaskCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            participantReviewState);

        var fingerprintBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprint));
        return Convert.ToHexString(fingerprintBytes);
    }
}
