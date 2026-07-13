using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Gets all users across all tenants.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter users</param>
    /// <returns>List of users with management information</returns>
    /// <response code="200">Returns the list of users</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> GetAllUsers([FromQuery] Guid? tenantId = null)
    {
        try
        {

            var query = context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == tenantId.Value);
            }

            var result = await query
                .OrderBy(u => u.TenantId)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
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


            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving users", ex);
        }
    }

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User management information</returns>
    /// <response code="200">Returns the user information</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> GetUser(Guid userId)
    {
        try
        {
            var userDto = await context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Where(u => u.Id == userId)
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
                .FirstOrDefaultAsync();

            if (userDto is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving user", ex);
        }
    }

}
