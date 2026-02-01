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
    public BitbucketUser(BitbucketId uuid, UserName displayName)
    {
        Uuid = uuid;
        DisplayName = displayName;
    }

    /// <summary>
    /// User display name.
    /// </summary>
    public UserName DisplayName { get; }

    /// <summary>
    /// User UUID.
    /// </summary>
    public BitbucketId Uuid { get; }

}
