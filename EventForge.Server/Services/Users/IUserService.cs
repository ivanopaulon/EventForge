using EventForge.DTOs.Common;
using EventForge.DTOs.Auth;

namespace EventForge.Server.Services.Users;

/// <summary>
/// Service interface for managing users.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated users</returns>
    Task<PagedResult<UserDto>> GetUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets users by role with pagination.
    /// </summary>
    /// <param name="role">Role name</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated users with the specified role</returns>
    Task<PagedResult<UserDto>> GetUsersByRoleAsync(
        string role,
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets active users with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated active users</returns>
    Task<PagedResult<UserDto>> GetActiveUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);
}
