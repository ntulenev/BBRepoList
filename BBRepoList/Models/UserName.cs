namespace BBRepoList.Models;

/// <summary>
/// User display name value object.
/// </summary>
public readonly record struct UserName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserName"/> struct.
    /// </summary>
    /// <param name="value">User display name.</param>
    public UserName(string? value)
    {
        Value = value ?? NOTAVAILABLE;
    }

    /// <summary>
    /// User display name value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;

    private const string NOTAVAILABLE = "<N/A>";
}
