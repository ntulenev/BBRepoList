using BBRepoList.Models;

namespace BBRepoList.Transport;

/// <summary>
/// Mapping helpers from DTOs to domain models.
/// </summary>
public static class BitbucketMappings
{
    /// <summary>
    /// Converts a repository DTO into a domain model.
    /// </summary>
    /// <param name="dto">Repository DTO.</param>
    public static Repository ToDomain(this RepositoryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new Repository(dto.Name ?? string.Empty, dto.CreatedOn);
    }

    /// <summary>
    /// Converts a repository page DTO into a domain model.
    /// </summary>
    /// <param name="dto">Repository page DTO.</param>
    public static RepoPage ToDomain(this RepoPageDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var values = dto.Values ?? [];
        var repositories = values.Select(static r => r?.ToDomain()
                                           ?? throw new ArgumentException("Repository item cannot be null.", nameof(dto)))
                                 .ToList();

        return new RepoPage(repositories, dto.Next);
    }

    /// <summary>
    /// Converts a user DTO into a domain model.
    /// </summary>
    /// <param name="dto">User DTO.</param>
    public static BitbucketUser ToDomain(this BitbucketUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new BitbucketUser(new BitbucketId(dto.Id), new UserName(dto.DisplayName));
    }
}
