using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Services.Auth;

/// <summary>
/// Service interface for tenant-scoped user management operations.
/// Provides comprehensive user management capabilities for tenant administrators.
/// </summary>
public interface ITenantUserManagementService
{
    /// <summary>
    /// Gets all users belonging to the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users in the current tenant</returns>
    Task<IEnumerable<UserManagementDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user by ID within the current tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User management information or null if not found</returns>
    Task<UserManagementDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users in the current tenant by query.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching users</returns>
    Task<IEnumerable<UserManagementDto>> SearchUsersAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user in the current tenant.
    /// </summary>
    /// <param name="dto">User creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    Task<UserManagementDto> CreateUserAsync(CreateUserManagementDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user in the current tenant.
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="dto">Updated user data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    Task<UserManagementDto> UpdateUserAsync(Guid userId, UpdateUserManagementDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from the current tenant.
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user status (active/inactive).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Status update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    Task<UserManagementDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user roles within the current tenant.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">New list of role names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    Task<UserManagementDto> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password and optionally forces password change on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPassword">New password</param>
    /// <param name="mustChangePassword">Whether user must change password on next login</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a user to change password on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ForcePasswordChangeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for users in the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User statistics</returns>
    Task<object> GetUserStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs quick actions on a user (lock, unlock, etc.).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<UserManagementDto> PerformQuickActionAsync(Guid userId, string action, CancellationToken cancellationToken = default);
}
