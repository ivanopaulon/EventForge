using EventForge.Server.Data.Entities.Auth;
using EventForge.DTOs.Auth;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for User entity to DTOs.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Maps User entity to UserDto with roles and permissions.
    /// </summary>
    public static UserDto ToDto(User user, List<string> roles, List<string> permissions)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            IsActive = user.IsActive,
            Roles = roles,
            Permissions = permissions,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    /// <summary>
    /// Maps User entity to UserManagementDto.
    /// </summary>
    public static EventForge.DTOs.SuperAdmin.UserManagementDto ToManagementDto(User user, List<string> roles, string? tenantName = null)
    {
        return new EventForge.DTOs.SuperAdmin.UserManagementDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            TenantId = user.TenantId,
            TenantName = tenantName,
            Roles = roles,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}