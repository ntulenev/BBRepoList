namespace BBRepoList.Models;

/// <summary>
/// Bitbucket user profile.
/// </summary>
public sealed class BitbucketUser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketUser"/> class.
    /// </summary>
    /// <param name="displayName">User display name.</param>
    /// <param name="nickname">User nickname.</param>
    /// <param name="uuid">User UUID.</param>
    /// <param name="accountId">User account identifier.</param>
    public BitbucketUser(string? displayName, string? nickname, string? uuid, string? accountId)
    {
        DisplayName = NormalizeOrThrow(displayName, nameof(displayName));
        Nickname = NormalizeOrThrow(nickname, nameof(nickname));
        Uuid = NormalizeOrThrow(uuid, nameof(uuid));
        AccountId = NormalizeOrThrow(accountId, nameof(accountId));
    }

    /// <summary>
    /// User display name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// User nickname.
    /// </summary>
    public string? Nickname { get; }

    /// <summary>
    /// User UUID.
    /// </summary>
    public string? Uuid { get; }

    /// <summary>
    /// User account identifier.
    /// </summary>
    public string? AccountId { get; }

    private static string? NormalizeOrThrow(string? value, string paramName)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
        }

        return trimmed;
    }
}
