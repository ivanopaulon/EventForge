using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin user management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class UserManagementController : BaseApiController
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IHubContext<AuditLogHub> _hubContext;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IAuditLogService auditLogService,
        IHubContext<AuditLogHub> hubContext,
        ILogger<UserManagementController> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditLogService = auditLogService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users across all tenants.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> GetAllUsers([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(u => u.TenantId == tenantId.Value);
            }

            var users = await query
                .OrderBy(u => u.TenantId)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var result = new List<UserManagementDto>();

            foreach (var user in users)
            {
                var tenant = await _context.Tenants.FindAsync(user.TenantId);
                
                var userDto = new UserManagementDto
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
                    TenantName = tenant?.Name ?? "Unknown",
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                result.Add(userDto);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserManagementDto>> GetUser(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            var tenant = await _context.Tenants.FindAsync(user.TenantId);

            var userDto = new UserManagementDto
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
                TenantName = tenant?.Name ?? "Unknown",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates user status (active/inactive).
    /// </summary>
    [HttpPut("{userId}/status")]
    public async Task<ActionResult<UserManagementDto>> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusDto updateDto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            var oldStatus = user.IsActive;
            user.IsActive = updateDto.IsActive;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

            await _context.SaveChangesAsync();

            // Log the status change
            await _auditLogService.LogEntityChangeAsync(
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
                PerformedByUserId = _tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = EventForge.Server.Data.Entities.Auth.AuditOperationType.TenantStatusChanged, // We can extend this enum
                TargetUserId = userId,
                Details = $"User status changed to {(updateDto.IsActive ? "Active" : "Inactive")}. Reason: {updateDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserStatusChanged", new { UserId = userId, IsActive = updateDto.IsActive });

            var tenant = await _context.Tenants.FindAsync(user.TenantId);

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
                TenantName = tenant?.Name ?? "Unknown",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for {UserId}", userId);
            return StatusCode(500, new { message = "Error updating user status", error = ex.Message });
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
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            // Get the roles by name
            var roles = await _context.Roles
                .Where(r => updateDto.Roles.Contains(r.Name))
                .ToListAsync();

            if (roles.Count != updateDto.Roles.Count)
            {
                var missingRoles = updateDto.Roles.Except(roles.Select(r => r.Name));
                return BadRequest(new { message = $"Invalid roles: {string.Join(", ", missingRoles)}" });
            }

            var oldRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Remove all existing roles
            _context.UserRoles.RemoveRange(user.UserRoles);

            // Add new roles
            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _tenantContext.CurrentUserId?.ToString() ?? "System"
                });
            }

            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

            await _context.SaveChangesAsync();

            // Log the role change
            await _auditLogService.LogEntityChangeAsync(
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
                PerformedByUserId = _tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = EventForge.Server.Data.Entities.Auth.AuditOperationType.AdminTenantGranted, // We can extend this enum
                TargetUserId = userId,
                Details = $"User roles changed from [{string.Join(", ", oldRoles)}] to [{string.Join(", ", updateDto.Roles)}]. Reason: {updateDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("UserRolesChanged", new { UserId = userId, Roles = updateDto.Roles });

            var tenant = await _context.Tenants.FindAsync(user.TenantId);

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
                TenantName = tenant?.Name ?? "Unknown",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user roles for {UserId}", userId);
            return StatusCode(500, new { message = "Error updating user roles", error = ex.Message });
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
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            user.MustChangePassword = true;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

            await _context.SaveChangesAsync();

            // Log the password change requirement
            await _auditLogService.LogEntityChangeAsync(
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
                PerformedByUserId = _tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = EventForge.Server.Data.Entities.Auth.AuditOperationType.ForcePasswordChange,
                TargetUserId = userId,
                Details = $"Password change forced for user '{user.Username}'. Reason: {forceDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.ModifiedBy
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group("AuditLogUpdates")
                .SendAsync("PasswordChangeForced", new { UserId = userId, Username = user.Username });

            return Ok(new { message = $"Password change forced for user {user.Username}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing password change for {UserId}", userId);
            return StatusCode(500, new { message = "Error forcing password change", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all available roles.
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<object>>> GetRoles()
    {
        try
        {
            var roles = await _context.Roles
                .Select(r => new { r.Id, r.Name, r.Description })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { message = "Error retrieving roles", error = ex.Message });
        }
    }
}