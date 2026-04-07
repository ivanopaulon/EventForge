using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Users;

/// <summary>
/// Service implementation for managing users.
/// </summary>
public class UserService(
    PrymDbContext context,
    ITenantContext tenantContext) : IUserService
{

    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted && u.TenantId == tenantContext.CurrentTenantId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                DateOfBirth = u.DateOfBirth,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets users by role with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersByRoleAsync(
        string role,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted
                && u.TenantId == tenantContext.CurrentTenantId
                && u.UserRoles.Any(ur => ur.Role.Name == role));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                DateOfBirth = u.DateOfBirth,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets active users with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetActiveUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted
                && u.TenantId == tenantContext.CurrentTenantId
                && u.IsActive);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                DateOfBirth = u.DateOfBirth,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

}
