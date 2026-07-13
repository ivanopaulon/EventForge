using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Gets all permissions assigned to a specific role.
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>List of permissions assigned to the role</returns>
    /// <response code="200">Returns the list of permissions</response>
    /// <response code="404">If the role is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("roles/{roleId}/permissions")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetRolePermissions(Guid roleId)
    {
        try
        {
            var role = await context.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role is null)
            {
                return NotFound($"Role with ID {roleId} not found");
            }

            var permissions = role.RolePermissions
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    PermissionName = rp.Permission.Name,
                    Resource = rp.Permission.Resource,
                    Action = rp.Permission.Action,
                    Description = rp.Permission.Description
                })
                .ToList();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving role permissions", ex);
        }
    }

    /// <summary>
    /// Updates the permissions assigned to a specific role.
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="dto">Update role permissions data</param>
    /// <returns>Success result</returns>
    /// <response code="200">Permissions updated successfully</response>
    /// <response code="404">If the role is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("roles/{roleId}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateRolePermissions(Guid roleId, [FromBody] UpdateRolePermissionsDto dto)
    {
        try
        {
            var role = await context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role is null)
            {
                return NotFound($"Role with ID {roleId} not found");
            }

            // Remove all existing role permissions
            context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new role permissions
            foreach (var permissionId in dto.PermissionIds)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    GrantedBy = User.Identity?.Name ?? "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow,
                    TenantId = Guid.Empty
                });
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Updated permissions for role {RoleId} ({RoleName}). {PermissionCount} permissions assigned",
                roleId, role.Name, dto.PermissionIds.Count);

            // Notify via SignalR
            await hubContext.Clients.All.SendAsync("RolePermissionsUpdated", roleId);

            return Ok();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating role permissions", ex);
        }
    }

    /// <summary>
    /// Gets all available permissions in the system.
    /// </summary>
    /// <returns>List of all permissions</returns>
    /// <response code="200">Returns the list of permissions</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAllPermissions()
    {
        try
        {
            var permissions = await context.Permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    PermissionName = p.Name,
                    Resource = p.Resource,
                    Action = p.Action,
                    Description = p.Description
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving permissions", ex);
        }
    }
}
