using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Gets all available roles.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IEnumerable<RoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
    {
        try
        {
            var roles = await context.Roles
                .AsNoTracking()
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving roles", ex);
        }
    }

    /// <summary>
    /// Searches users with advanced filtering.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<UserManagementDto>>> SearchUsers([FromBody] UserSearchDto searchDto)
    {
        try
        {
            var query = context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchDto.SearchTerm))
            {
                var term = searchDto.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term));
            }

            if (searchDto.TenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == searchDto.TenantId.Value);
            }

            if (!string.IsNullOrEmpty(searchDto.Role) && searchDto.Role != "all")
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == searchDto.Role));
            }

            if (searchDto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == searchDto.IsActive.Value);
            }

            if (searchDto.MustChangePassword.HasValue)
            {
                query = query.Where(u => u.MustChangePassword == searchDto.MustChangePassword.Value);
            }

            if (searchDto.CreatedAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= searchDto.CreatedAfter.Value);
            }

            if (searchDto.CreatedBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= searchDto.CreatedBefore.Value);
            }

            if (searchDto.LastLoginAfter.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= searchDto.LastLoginAfter.Value);
            }

            if (searchDto.LastLoginBefore.HasValue)
            {
                query = query.Where(u => u.LastLoginAt <= searchDto.LastLoginBefore.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(searchDto.SortBy))
            {
                var isDesc = searchDto.SortOrder?.ToLower() == "desc";
                query = searchDto.SortBy.ToLower() switch
                {
                    "username" => isDesc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
                    "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                    "firstname" => isDesc ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                    "lastname" => isDesc ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                    "createdat" => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                    "lastloginat" => isDesc ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                    _ => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            // Add eager loading of Tenant before executing queries
            query = query.Include(u => u.Tenant);

            var totalCount = await query.CountAsync();

            var result = await query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .Select(u => new UserManagementDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    FullName = u.FullName,
                    IsActive = u.IsActive,
                    MustChangePassword = u.MustChangePassword,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    TenantId = u.TenantId,
                    TenantName = u.Tenant != null ? u.Tenant.Name : "Unknown",
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            var paginatedResponse = new PagedResult<UserManagementDto>
            {
                Items = result,
                TotalCount = totalCount,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(paginatedResponse);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error searching users", ex);
        }
    }

    /// <summary>
    /// Gets user statistics for the dashboard.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var query = context.Users.AsNoTracking().AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == tenantId.Value);
            }

            var totalUsers = await query.CountAsync();
            var activeUsers = await query.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var usersPendingPasswordChange = await query.CountAsync(u => u.MustChangePassword);
            var lockedUsers = await query.CountAsync(u => u.LockedUntil > DateTime.UtcNow);

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var newUsersThisMonth = await query.CountAsync(u => u.CreatedAt >= oneMonthAgo);

            var today = DateTime.UtcNow.Date;
            var loginsToday = await context.AuditTrails
                .AsNoTracking()
                .CountAsync(a => a.OperationType == AuditOperationType.TenantSwitch &&
                                a.PerformedAt >= today);

            var failedLoginsToday = await context.AuditTrails
                .AsNoTracking()
                .CountAsync(a => a.OperationType == AuditOperationType.TenantStatusChanged &&
                                a.PerformedAt >= today);

            var usersByRole = await context.UserRoles
                .AsNoTracking()
                .Include(ur => ur.Role)
                .Where(ur => tenantId == null || ur.User.TenantId == tenantId.Value)
                .GroupBy(ur => ur.Role.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count);

            var usersByTenantDict = await context.Users
                .AsNoTracking()
                .Include(u => u.Tenant)
                .Where(u => tenantId == null || u.TenantId == tenantId.Value)
                .GroupBy(u => new { u.TenantId, TenantName = u.Tenant != null ? u.Tenant.Name : null })
                .Select(g => new { TenantName = g.Key.TenantName ?? "Unknown", Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantName, x => x.Count);

            var statistics = new UserStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                UsersPendingPasswordChange = usersPendingPasswordChange,
                LockedUsers = lockedUsers,
                NewUsersThisMonth = newUsersThisMonth,
                LoginsToday = loginsToday,
                FailedLoginsToday = failedLoginsToday,
                UsersByRole = usersByRole,
                UsersByTenant = usersByTenantDict
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving user statistics", ex);
        }
    }

}
