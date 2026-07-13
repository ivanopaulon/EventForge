using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Updates user status (active/inactive).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateDto">Status update data</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Returns the updated user information</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{userId}/status")]
    [ProducesResponseType(typeof(UserManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserManagementDto>> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusDto updateDto)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            var oldStatus = user.IsActive;
            user.IsActive = updateDto.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Log the status change
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "IsActive",
                "Update",
                oldStatus.ToString(),
                updateDto.IsActive.ToString(),
                user.ModifiedBy,
                $"User '{user.Username}'"
            );

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.TenantStatusChanged, // We can extend this enum
                TargetUserId = userId,
                Details = $"User status changed to {(updateDto.IsActive ? "Active" : "Inactive")}. Reason: {updateDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserStatusChanged", new { UserId = userId, IsActive = updateDto.IsActive });

            var result = new UserManagementDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                TenantId = user.TenantId,
                TenantName = user.Tenant?.Name ?? "Unknown",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating user status", ex);
        }
    }

    /// <summary>
    /// Updates user roles.
    /// </summary>
    [HttpPut("{userId}/roles")]
    public async Task<ActionResult<UserManagementDto>> UpdateUserRoles(Guid userId, [FromBody] UpdateUserRolesDto updateDto)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            // Get the roles by name
            var roles = await context.Roles
                .Where(r => updateDto.Roles.Contains(r.Name))
                .ToListAsync();

            if (roles.Count != updateDto.Roles.Count)
            {
                var missingRoles = updateDto.Roles.Except(roles.Select(r => r.Name));
                return CreateValidationProblemDetails($"Invalid roles: {string.Join(", ", missingRoles)}");
            }

            var oldRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Remove all existing roles
            context.UserRoles.RemoveRange(user.UserRoles);

            // Add new roles
            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = tenantContext.CurrentUserId?.ToString() ?? "System"
                });
            }

            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Log the role change
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "Roles",
                "Update",
                string.Join(", ", oldRoles),
                string.Join(", ", updateDto.Roles),
                user.ModifiedBy,
                $"User '{user.Username}'"
            );

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.AdminTenantGranted, // We can extend this enum
                TargetUserId = userId,
                Details = $"User roles changed from [{string.Join(", ", oldRoles)}] to [{string.Join(", ", updateDto.Roles)}]. Reason: {updateDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserRolesChanged", new { UserId = userId, Roles = updateDto.Roles });

            var result = new UserManagementDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                Roles = updateDto.Roles,
                TenantId = user.TenantId,
                TenantName = user.Tenant?.Name ?? "Unknown",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating user roles", ex);
        }
    }

}
