namespace BBRepoList.Abstractions;

/// <summary>
/// Abstraction for Bitbucket HTTP transport and JSON handling.
/// </summary>
public interface IBitbucketTransport
{
    /// <summary>
    /// Issues a GET request and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TDto">DTO type to deserialize.</typeparam>
    /// <param name="url">Relative or absolute URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized DTO or null if response body is null.</returns>
    Task<TDto?> GetAsync<TDto>(Uri url, CancellationToken cancellationToken);
}
