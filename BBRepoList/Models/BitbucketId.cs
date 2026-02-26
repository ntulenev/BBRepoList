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
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
        _normalizedValue = Normalize(Value);
    }

    /// <summary>
    /// Bitbucket identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Attempts to create a <see cref="BitbucketId"/> from raw input.
    /// </summary>
    /// <param name="value">Raw identifier value.</param>
    /// <param name="id">Parsed identifier when successful.</param>
    /// <returns><see langword="true" /> when value can be converted to a valid identifier.</returns>
    public static bool TryCreate(string? value, out BitbucketId id)
    {
        id = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            id = new BitbucketId(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether two identifiers represent the same Bitbucket entity.
    /// </summary>
    /// <param name="other">Other identifier.</param>
    /// <returns><see langword="true" /> when values match ignoring braces and casing.</returns>
    public bool Equals(BitbucketId other) =>
        string.Equals(
            _normalizedValue ?? string.Empty,
            other._normalizedValue ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(_normalizedValue ?? string.Empty);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('{', '}');
    }

    private readonly string? _normalizedValue;
}
