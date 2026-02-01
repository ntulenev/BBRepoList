namespace BBRepoList.Models;

/// <summary>
/// Bitbucket identifier value object.
/// </summary>
public readonly record struct BitbucketId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BitbucketId"/> struct.
    /// </summary>
    /// <param name="value">Bitbucket identifier value.</param>
    public BitbucketId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    /// <summary>
    /// Bitbucket identifier value.
    /// </summary>
    public string Value { get; }

}
