namespace BBRepoList.Models;

/// <summary>
/// Bitbucket user profile.
/// </summary>
public sealed class BitbucketUser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketUser"/> class.
    /// </summary>
    /// <param name="uuid">User UUID.</param>
    /// <param name="displayName">User display name.</param>
    public BitbucketUser(BitbucketId uuid, string? displayName)
    {
        Uuid = uuid;
        DisplayName = displayName ?? NOTAVAILABLE;
    }

    /// <summary>
    /// User display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// User UUID.
    /// </summary>
    public BitbucketId Uuid { get; }

    /// <summary>
    /// 
    /// </summary>
    private const string NOTAVAILABLE = "<N/A>";
}
