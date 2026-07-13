using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Performs quick actions on multiple users.
    /// </summary>
    [HttpPost("quick-actions")]
    public async Task<ActionResult<QuickActionResultDto>> PerformQuickActions([FromBody] QuickUserActionDto actionDto)
    {
        try
        {
            var results = new List<UserActionResultDto>();
            var successCount = 0;
            var failCount = 0;

            foreach (var userId in actionDto.UserIds)
            {
                try
                {
                    var user = await context.Users.FindAsync(userId);
                    if (user is null)
                    {
                        results.Add(new UserActionResultDto
                        {
                            UserId = userId,
                            Username = "Unknown",
                            Success = false,
                            ErrorMessage = "User not found"
                        });
                        failCount++;
                        continue;
                    }

                    bool actionPerformed = false;
                    var result = new UserActionResultDto
                    {
                        UserId = userId,
                        Username = user.Username,
                        Success = true
                    };

                    switch (actionDto.Action.ToLower())
                    {
                        case "enable":
                            user.IsActive = true;
                            actionPerformed = true;
                            break;
                        case "disable":
                            user.IsActive = false;
                            actionPerformed = true;
                            break;
                        case "resetpassword":
                            // Implementation would depend on your password reset logic
                            user.MustChangePassword = true;
                            actionPerformed = true;
                            result.Result = new Dictionary<string, object> { { "message", "Password reset required" } };
                            break;
                        case "forcepasswordchange":
                            user.MustChangePassword = true;
                            actionPerformed = true;
                            break;
                        case "lockout":
                            user.LockedUntil = DateTime.UtcNow.AddHours(24); // Lock for 24 hours
                            actionPerformed = true;
                            break;
                        case "unlock":
                            user.LockedUntil = null;
                            actionPerformed = true;
                            break;
                        default:
                            result.Success = false;
                            result.ErrorMessage = $"Unknown action: {actionDto.Action}";
                            break;
                    }

                    if (actionPerformed)
                    {
                        user.ModifiedAt = DateTime.UtcNow;
                        user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

                        // Log the action
                        _ = await auditLogService.LogEntityChangeAsync(
                            nameof(User),
                            user.Id,
                            actionDto.Action,
                            "QuickAction",
                            "",
                            actionDto.Action,
                            user.ModifiedBy,
                            $"User '{user.Username}' - {actionDto.Reason}"
                        );

                        successCount++;
                    }
                    else if (result.Success)
                    {
                        failCount++;
                    }

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(new UserActionResultDto
                    {
                        UserId = userId,
                        Username = "Error",
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    failCount++;
                }
            }

            _ = await context.SaveChangesAsync();

            // Create bulk audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.TenantStatusChanged, // Using closest available enum value
                Details = $"Bulk action '{actionDto.Action}' performed on {actionDto.UserIds.Count} users. Success: {successCount}, Failed: {failCount}. Reason: {actionDto.Reason}",
                WasSuccessful = successCount > 0,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("BulkUserActionCompleted", new
                {
                    Action = actionDto.Action,
                    TotalUsers = actionDto.UserIds.Count,
                    SuccessCount = successCount,
                    FailCount = failCount
                });

            var quickActionResult = new QuickActionResultDto
            {
                Action = actionDto.Action,
                TotalUsers = actionDto.UserIds.Count,
                SuccessfulActions = successCount,
                FailedActions = failCount,
                Results = results
            };

            return Ok(quickActionResult);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error performing quick actions", ex);
        }
    }

    /// <summary>
    /// Updates a user's complete information.
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(Guid userId, [FromBody] UpdateUserManagementDto updateDto)
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

            // Store old values for audit
            var oldEmail = user.Email;
            var oldFirstName = user.FirstName;
            var oldLastName = user.LastName;
            var oldIsActive = user.IsActive;
            var oldRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Update user properties
            user.Email = updateDto.Email;
            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.IsActive = updateDto.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            // Update roles if they changed
            if (!oldRoles.SequenceEqual(updateDto.Roles))
            {
                // Get the roles by name
                var roles = await context.Roles
                    .Where(r => updateDto.Roles.Contains(r.Name))
                    .ToListAsync();

                if (roles.Count != updateDto.Roles.Count)
                {
                    var missingRoles = updateDto.Roles.Except(roles.Select(r => r.Name));
                    return CreateValidationProblemDetails($"Invalid roles: {string.Join(", ", missingRoles)}");
                }

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
                        CreatedBy = user.ModifiedBy
                    });
                }
            }

            _ = await context.SaveChangesAsync();

            // Log the changes
            if (oldEmail != user.Email)
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    nameof(User), user.Id, "Email", "Update", oldEmail, user.Email, user.ModifiedBy, $"User '{user.Username}'");
            }

            if (oldFirstName != user.FirstName)
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    nameof(User), user.Id, "FirstName", "Update", oldFirstName, user.FirstName, user.ModifiedBy, $"User '{user.Username}'");
            }

            if (oldLastName != user.LastName)
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    nameof(User), user.Id, "LastName", "Update", oldLastName, user.LastName, user.ModifiedBy, $"User '{user.Username}'");
            }

            if (oldIsActive != user.IsActive)
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    nameof(User), user.Id, "IsActive", "Update", oldIsActive.ToString(), user.IsActive.ToString(), user.ModifiedBy, $"User '{user.Username}'");
            }

            if (!oldRoles.SequenceEqual(updateDto.Roles))
            {
                _ = await auditLogService.LogEntityChangeAsync(
                    nameof(User), user.Id, "Roles", "Update",
                    string.Join(", ", oldRoles), string.Join(", ", updateDto.Roles),
                    user.ModifiedBy, $"User '{user.Username}'");
            }

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.TenantStatusChanged, // We can extend this enum
                TargetUserId = userId,
                Details = $"User '{user.Username}' updated",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserUpdated", new { UserId = userId, Username = user.Username });

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
            return CreateInternalServerErrorProblem("Error updating user", ex);
        }
    }

}
