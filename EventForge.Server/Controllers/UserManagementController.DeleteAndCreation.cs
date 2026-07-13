using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace EventForge.Server.Controllers;

public partial class UserManagementController
{
    /// <summary>
    /// Deletes a user (soft delete).
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId, [FromBody] DeleteUserDto? deleteDto = null)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);

            if (user is null)
            {
                return CreateNotFoundProblem($"User {userId} not found");
            }

            // Soft delete by setting IsActive to false and marking as deleted
            user.IsActive = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Log the deletion
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "SoftDeleted",
                "Delete",
                "false",
                "true",
                user.ModifiedBy,
                $"User '{user.Username}'"
            );

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.TenantStatusChanged, // We can extend this enum
                TargetUserId = userId,
                Details = $"User '{user.Username}' soft deleted. Reason: {deleteDto?.Reason ?? "Not specified"}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            // Notify clients
            await hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserDeleted", new { UserId = userId, Username = user.Username });

            return Ok(new { message = $"User {user.Username} deleted successfully" });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting user", ex);
        }
    }

    /// <summary>
    /// Creates a new user using CreateUserManagementDto.
    /// </summary>
    [HttpPost("management")]
    public async Task<ActionResult<UserManagementDto>> CreateUserManagement([FromBody] CreateUserManagementDto createDto)
    {
        try
        {
            // Check if username or email already exists
            var existingUser = await context.Users
                .AnyAsync(u => u.Username == createDto.Username || u.Email == createDto.Email);

            if (existingUser)
            {
                return CreateValidationProblemDetails("Username or email already exists");
            }

            // Verify tenant exists
            var tenant = await context.Tenants.FindAsync(createDto.TenantId);
            if (tenant is null)
            {
                return CreateValidationProblemDetails("Invalid tenant ID");
            }

            // Generate a temporary password
            var tempPassword = GenerateTemporaryPassword();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = createDto.Username,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                TenantId = createDto.TenantId,
                IsActive = true, // Default to active for management creation
                MustChangePassword = true, // Force password change for new users
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            var (hash, salt) = passwordService.HashPassword(tempPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            _ = context.Users.Add(user);

            // Add roles if specified
            if (createDto.Roles.Any())
            {
                var roles = await context.Roles
                    .Where(r => createDto.Roles.Contains(r.Name))
                    .ToListAsync();

                foreach (var role in roles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = user.CreatedBy
                    });
                }
            }

            _ = await context.SaveChangesAsync();

            // Log user creation
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "UserCreated",
                "Create",
                "",
                $"Username: {user.Username}, Email: {user.Email}",
                user.CreatedBy,
                $"User '{user.Username}' created via management interface"
            );

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
                Roles = createDto.Roles,
                TenantId = user.TenantId,
                TenantName = tenant.Name,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating user", ex);
        }
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserCreationResultDto>> CreateUser([FromBody] CreateUserDto createDto)
    {
        try
        {
            // Check if username or email already exists
            var existingUser = await context.Users
                .AnyAsync(u => u.Username == createDto.Username || u.Email == createDto.Email);

            if (existingUser)
            {
                return CreateValidationProblemDetails("Username or email already exists");
            }

            // Verify tenant exists
            var tenant = await context.Tenants.FindAsync(createDto.TenantId);
            if (tenant is null)
            {
                return CreateValidationProblemDetails("Invalid tenant ID");
            }

            // Generate a temporary password
            var tempPassword = GenerateTemporaryPassword();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = createDto.Username,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                TenantId = createDto.TenantId,
                IsActive = createDto.IsActive,
                MustChangePassword = createDto.MustChangePassword,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            var (hash, salt) = passwordService.HashPassword(tempPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            _ = context.Users.Add(user);

            // Add roles if specified
            if (createDto.Roles.Any())
            {
                var roles = await context.Roles
                    .Where(r => createDto.Roles.Contains(r.Name))
                    .ToListAsync();

                foreach (var role in roles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = user.CreatedBy
                    });
                }
            }

            _ = await context.SaveChangesAsync();

            // Log user creation
            _ = await auditLogService.LogEntityChangeAsync(
                nameof(User),
                user.Id,
                "UserCreated",
                "Create",
                "",
                $"Username: {user.Username}, Email: {user.Email}",
                user.CreatedBy,
                $"User '{user.Username}' created"
            );

            var result = new UserCreationResultDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                GeneratedPassword = tempPassword,
                MustChangePassword = user.MustChangePassword,
                AssignedRoles = createDto.Roles
            };

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating user", ex);
        }
    }

    private string GenerateTemporaryPassword()
    {
        // Simple temporary password generation - in production, use a more secure method
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

}
