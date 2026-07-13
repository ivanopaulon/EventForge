using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Resets a user's password and generates a new temporary password.
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<ActionResult<PasswordResetResultDto>> ResetPassword(Guid userId, [FromBody] ResetPasswordDto resetDto)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);

            if (user is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            // Generate a new temporary password
            var newPassword = GenerateTemporaryPassword();

            // Hash the new password
            var (hash, salt) = passwordService.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            user.MustChangePassword = true;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Log the password reset
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "PasswordReset",
                "Update",
                "HashedPassword",
                "NewHashedPassword",
                user.ModifiedBy,
                $"User '{user.Username}'"
            );

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.ForcePasswordChange,
                TargetUserId = userId,
                Details = $"Password reset for user '{user.Username}'. Reason: {resetDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("PasswordReset", new { UserId = userId, Username = user.Username });

            var result = new PasswordResetResultDto
            {
                Success = true,
                UserId = userId,
                Username = user.Username,
                TemporaryPassword = newPassword,
                MustChangePassword = true,
                ResetAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error resetting password", ex);
        }
    }

    /// <summary>
    /// Forces a password change for a user.
    /// </summary>
    [HttpPost("{userId}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid userId, [FromBody] ForcePasswordChangeDto forceDto)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);

            if (user is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            user.MustChangePassword = true;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Log the password change requirement
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "MustChangePassword",
                "Update",
                "false",
                "true",
                user.ModifiedBy,
                $"User '{user.Username}'"
            );

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.ForcePasswordChange,
                TargetUserId = userId,
                Details = $"Password change forced for user '{user.Username}'. Reason: {forceDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("PasswordChangeForced", new { UserId = userId, Username = user.Username });

            return Ok(new { message = $"Password change forced for user {user.Username}" });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error forcing password change", ex);
        }
    }

}
