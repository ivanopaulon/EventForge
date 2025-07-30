using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using EventForge.DTOs.Common;

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
                OperationType = AuditOperationType.TenantStatusChanged, // We can extend this enum
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
                OperationType = AuditOperationType.AdminTenantGranted, // We can extend this enum
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
                OperationType = AuditOperationType.ForcePasswordChange,
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

    /// <summary>
    /// Searches users with advanced filtering.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<UserManagementDto>>> SearchUsers([FromBody] UserSearchDto searchDto)
    {
        try
        {
            var query = _context.Users
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

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
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
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "Error searching users", error = ex.Message });
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
            var query = _context.Users.AsQueryable();

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
            var loginsToday = await _context.AuditTrails
                .CountAsync(a => a.OperationType == AuditOperationType.TenantSwitch &&
                                a.PerformedAt >= today);

            var failedLoginsToday = await _context.AuditTrails
                .CountAsync(a => a.OperationType == AuditOperationType.TenantStatusChanged &&
                                a.PerformedAt >= today);

            var usersByRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => tenantId == null || ur.User.TenantId == tenantId.Value)
                .GroupBy(ur => ur.Role.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count);

            var usersByTenant = await _context.Users
                .Where(u => tenantId == null || u.TenantId == tenantId.Value)
                .GroupBy(u => u.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToListAsync();

            var usersByTenantDict = new Dictionary<string, int>();
            foreach (var item in usersByTenant)
            {
                var tenant = await _context.Tenants.FindAsync(item.TenantId);
                usersByTenantDict[tenant?.Name ?? "Unknown"] = item.Count;
            }

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
            _logger.LogError(ex, "Error retrieving user statistics");
            return StatusCode(500, new { message = "Error retrieving user statistics", error = ex.Message });
        }
    }

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
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
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
                        user.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

                        // Log the action
                        await _auditLogService.LogEntityChangeAsync(
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

            await _context.SaveChangesAsync();

            // Create bulk audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = _tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuditOperationType.TenantStatusChanged, // Using closest available enum value
                Details = $"Bulk action '{actionDto.Action}' performed on {actionDto.UserIds.Count} users. Success: {successCount}, Failed: {failCount}. Reason: {actionDto.Reason}",
                WasSuccessful = successCount > 0,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group("AuditLogUpdates")
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
            _logger.LogError(ex, "Error performing quick actions");
            return StatusCode(500, new { message = "Error performing quick actions", error = ex.Message });
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
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == createDto.Username || u.Email == createDto.Email);

            if (existingUser)
            {
                return BadRequest(new { message = "Username or email already exists" });
            }

            // Verify tenant exists
            var tenant = await _context.Tenants.FindAsync(createDto.TenantId);
            if (tenant == null)
            {
                return BadRequest(new { message = "Invalid tenant ID" });
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
                CreatedBy = _tenantContext.CurrentUserId?.ToString() ?? "System"
            };

            // Hash the temporary password (implementation depends on your password service)
            // user.PasswordHash = _passwordService.HashPassword(tempPassword);

            _context.Users.Add(user);

            // Add roles if specified
            if (createDto.Roles.Any())
            {
                var roles = await _context.Roles
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

            await _context.SaveChangesAsync();

            // Log user creation
            await _auditLogService.LogEntityChangeAsync(
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
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error creating user", error = ex.Message });
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