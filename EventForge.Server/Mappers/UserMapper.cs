using EventForge.Server.Data.Entities.Auth;
using EventForge.DTOs.Auth;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for User entities and DTOs
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Maps User entity to UserDto
    /// </summary>
    public static UserDto ToDto(User entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new UserDto
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            FullName = entity.FullName,
            IsActive = entity.IsActive,
            LastLoginAt = entity.LastLoginAt,
            // Roles and Permissions need to be populated separately
            Roles = new List<string>(),
            Permissions = new List<string>()
        };
    }

    /// <summary>
    /// Maps User entity to UserDto with roles and permissions
    /// </summary>
    public static UserDto ToDto(User entity, IList<string> roles, IList<string> permissions)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var userDto = ToDto(entity);
        userDto.Roles = roles ?? new List<string>();
        userDto.Permissions = permissions ?? new List<string>();
        
        return userDto;
    }

    /// <summary>
    /// Maps a collection of User entities to UserDto collection
    /// </summary>
    public static IEnumerable<UserDto> ToDtoCollection(IEnumerable<User> entities)
    {
        if (entities == null)
            return Enumerable.Empty<UserDto>();

        return entities.Select(ToDto);
    }
}