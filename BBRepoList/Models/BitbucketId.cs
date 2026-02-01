namespace BBRepoList.Models;

/// <summary>
/// Bitbucket identifier value object.
/// </summary>
public readonly record struct BitbucketId
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public BitbucketId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Value { get; }

}
