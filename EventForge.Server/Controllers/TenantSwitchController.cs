using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = EventForge.DTOs.Common.AuditOperationType;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin tenant switching and user impersonation operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class TenantSwitchController : BaseApiController
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IHubContext<AuditLogHub> _hubContext;
    private readonly ILogger<TenantSwitchController> _logger;

    public TenantSwitchController(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IAuditLogService auditLogService,
        IHubContext<AuditLogHub> hubContext,
        ILogger<TenantSwitchController> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditLogService = auditLogService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets current context information for the SuperAdmin.
    /// </summary>
    [HttpGet("context")]
    public async Task<ActionResult<CurrentContextDto>> GetCurrentContext()
    {
        try
        {
            var currentUserId = _tenantContext.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return BadRequest(new { message = "No current user context" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null)
            {
                return NotFound(new { message = "Current user not found" });
            }

            var currentTenant = await _context.Tenants.FindAsync(user.TenantId);

            var context = new CurrentContextDto
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CurrentTenantId = user.TenantId,
                CurrentTenantName = currentTenant?.Name,
                OriginalTenantId = user.TenantId, // In a real implementation, this would track the original tenant
                OriginalTenantName = currentTenant?.Name,
                IsImpersonating = false, // Would be determined by session state
                ImpersonatedUserId = null,
                ImpersonatedUsername = null,
                IsSuperAdmin = user.UserRoles.Any(ur => ur.Role.Name == "SuperAdmin"),
                SessionId = HttpContext.Session.Id,
                LoginTime = DateTime.UtcNow, // Would be tracked in session
                LastActivity = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ActiveSessions = new List<string> { HttpContext.Session.Id }
            };

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current context");
            return StatusCode(500, new { message = "Error retrieving current context", error = ex.Message });
        }
    }

    /// <summary>
    /// Validates security for tenant switching or impersonation operations.
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<SecurityValidationResultDto>> ValidateSecurity([FromBody] SecurityValidationDto validationDto)
    {
        try
        {
            var result = new SecurityValidationResultDto
            {
                IsValid = true,
                ValidationMessage = "Security validation passed",
                Warnings = new List<string>(),
                Requirements = new List<string>()
            };

            // Basic validation checks
            var currentUserId = _tenantContext.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                result.IsValid = false;
                result.ValidationMessage = "No current user context";
                return Ok(result);
            }

            // Check if user is SuperAdmin
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null || !user.UserRoles.Any(ur => ur.Role.Name == "SuperAdmin"))
            {
                result.IsValid = false;
                result.ValidationMessage = "SuperAdmin privileges required";
                return Ok(result);
            }

            // Validate target tenant/user based on action
            switch (validationDto.Action.ToLower())
            {
                case "switch":
                    if (validationDto.TargetTenantId.HasValue)
                    {
                        var tenant = await _context.Tenants.FindAsync(validationDto.TargetTenantId.Value);
                        if (tenant == null)
                        {
                            result.IsValid = false;
                            result.ValidationMessage = "Target tenant not found";
                        }
                        else if (!tenant.IsEnabled)
                        {
                            result.Warnings.Add("Target tenant is currently disabled");
                        }
                    }
                    break;

                case "impersonate":
                    if (validationDto.TargetUserId.HasValue)
                    {
                        var targetUser = await _context.Users.FindAsync(validationDto.TargetUserId.Value);
                        if (targetUser == null)
                        {
                            result.IsValid = false;
                            result.ValidationMessage = "Target user not found";
                        }
                        else if (!targetUser.IsActive)
                        {
                            result.Warnings.Add("Target user is currently inactive");
                        }
                        else if (targetUser.IsLockedOut)
                        {
                            result.Warnings.Add("Target user is currently locked out");
                        }
                    }
                    break;
            }

            // Check if reason is required for sensitive operations
            if (string.IsNullOrEmpty(validationDto.Reason))
            {
                result.Requirements.Add("Reason is required for audit trail");
                result.RequiresAdditionalConfirmation = true;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating security");
            return StatusCode(500, new { message = "Error validating security", error = ex.Message });
        }
    }

    /// <summary>
    /// Switches SuperAdmin to a different tenant context.
    /// </summary>
    [HttpPost("switch")]
    public async Task<ActionResult<CurrentContextDto>> SwitchTenant([FromBody] TenantSwitchWithAuditDto switchDto)
    {
        try
        {
            var currentUserId = _tenantContext.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return BadRequest(new { message = "No current user context" });
            }

            // Validate target tenant
            var targetTenant = await _context.Tenants.FindAsync(switchDto.TenantId);
            if (targetTenant == null)
            {
                return NotFound(new { message = "Target tenant not found" });
            }

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser == null)
            {
                return NotFound(new { message = "Current user not found" });
            }

            var originalTenantId = currentUser.TenantId;

            // Create tenant switch audit trail
            if (switchDto.CreateAuditEntry)
            {
                var auditTrail = new AuditTrail
                {
                    PerformedByUserId = currentUserId.Value,
                    OperationType = AuthAuditOperationType.TenantSwitch,
                    SourceTenantId = originalTenantId,
                    TargetTenantId = switchDto.TenantId,
                    SessionId = HttpContext.Session.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Details = $"SuperAdmin switched from tenant '{originalTenantId}' to '{targetTenant.Name}'. Reason: {switchDto.Reason}",
                    WasSuccessful = true,
                    PerformedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser.Username
                };

                _context.AuditTrails.Add(auditTrail);
            }

            // In a real implementation, you would update session state or token claims here
            // For now, we'll just log the switch
            _logger.LogInformation("SuperAdmin {Username} switched to tenant {TenantName}",
                currentUser.Username, targetTenant.Name);

            await _context.SaveChangesAsync();

            // Notify other SuperAdmin sessions
            await _hubContext.Clients.Group("SuperAdminUpdates")
                .SendAsync("TenantSwitched", new
                {
                    UserId = currentUserId.Value,
                    Username = currentUser.Username,
                    FromTenantId = originalTenantId,
                    ToTenantId = switchDto.TenantId,
                    ToTenantName = targetTenant.Name,
                    Timestamp = DateTime.UtcNow
                });

            // Return updated context
            var context = new CurrentContextDto
            {
                UserId = currentUser.Id,
                Username = currentUser.Username,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                Roles = new List<string> { "SuperAdmin" },
                CurrentTenantId = switchDto.TenantId,
                CurrentTenantName = targetTenant.Name,
                OriginalTenantId = originalTenantId,
                OriginalTenantName = "SuperAdmin",
                IsImpersonating = false,
                IsSuperAdmin = true,
                SessionId = HttpContext.Session.Id,
                LoginTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching tenant");
            return StatusCode(500, new { message = "Error switching tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Starts impersonating a user.
    /// </summary>
    [HttpPost("impersonate")]
    public async Task<ActionResult<CurrentContextDto>> StartImpersonation([FromBody] ImpersonationWithAuditDto impersonationDto)
    {
        try
        {
            var currentUserId = _tenantContext.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return BadRequest(new { message = "No current user context" });
            }

            // Validate target user
            var targetUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == impersonationDto.UserId);

            if (targetUser == null)
            {
                return NotFound(new { message = "Target user not found" });
            }

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser == null)
            {
                return NotFound(new { message = "Current user not found" });
            }

            // Create impersonation audit trail
            if (impersonationDto.CreateAuditEntry)
            {
                var auditTrail = new AuditTrail
                {
                    PerformedByUserId = currentUserId.Value,
                    OperationType = AuthAuditOperationType.ImpersonationStart,
                    SourceTenantId = currentUser.TenantId,
                    TargetTenantId = impersonationDto.TargetTenantId ?? targetUser.TenantId,
                    TargetUserId = targetUser.Id,
                    SessionId = HttpContext.Session.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Details = $"SuperAdmin '{currentUser.Username}' started impersonating user '{targetUser.Username}'. Reason: {impersonationDto.Reason}",
                    WasSuccessful = true,
                    PerformedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser.Username
                };

                _context.AuditTrails.Add(auditTrail);
            }

            // In a real implementation, you would update session state or token claims here
            _logger.LogInformation("SuperAdmin {SuperAdminUsername} started impersonating user {TargetUsername}",
                currentUser.Username, targetUser.Username);

            await _context.SaveChangesAsync();

            // Notify other SuperAdmin sessions
            await _hubContext.Clients.Group("SuperAdminUpdates")
                .SendAsync("ImpersonationStarted", new
                {
                    SuperAdminId = currentUserId.Value,
                    SuperAdminUsername = currentUser.Username,
                    TargetUserId = targetUser.Id,
                    TargetUsername = targetUser.Username,
                    Timestamp = DateTime.UtcNow
                });

            // Return context as impersonated user
            var targetTenant = targetUser.TenantId != Guid.Empty ?
                await _context.Tenants.FindAsync(targetUser.TenantId) : null;

            var context = new CurrentContextDto
            {
                UserId = targetUser.Id,
                Username = targetUser.Username,
                FullName = targetUser.FullName,
                Email = targetUser.Email,
                Roles = targetUser.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CurrentTenantId = targetUser.TenantId,
                CurrentTenantName = targetTenant?.Name,
                OriginalTenantId = currentUser.TenantId,
                OriginalTenantName = "SuperAdmin",
                IsImpersonating = true,
                ImpersonatedUserId = targetUser.Id,
                ImpersonatedUsername = targetUser.Username,
                IsSuperAdmin = true, // Still SuperAdmin, just impersonating
                SessionId = HttpContext.Session.Id,
                LoginTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting impersonation");
            return StatusCode(500, new { message = "Error starting impersonation", error = ex.Message });
        }
    }

    /// <summary>
    /// Ends current impersonation and returns to SuperAdmin context.
    /// </summary>
    [HttpPost("end-impersonation")]
    public async Task<ActionResult<CurrentContextDto>> EndImpersonation([FromBody] EndImpersonationDto endDto)
    {
        try
        {
            var currentUserId = _tenantContext.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return BadRequest(new { message = "No current user context" });
            }

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser == null)
            {
                return NotFound(new { message = "Current user not found" });
            }

            // Create impersonation end audit trail
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = currentUserId.Value,
                OperationType = AuthAuditOperationType.ImpersonationEnd,
                SourceTenantId = currentUser.TenantId,
                SessionId = HttpContext.Session.Id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Details = $"SuperAdmin '{currentUser.Username}' ended impersonation. Reason: {endDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.Username
            };

            _context.AuditTrails.Add(auditTrail);

            _logger.LogInformation("SuperAdmin {Username} ended impersonation", currentUser.Username);

            await _context.SaveChangesAsync();

            // Notify other SuperAdmin sessions
            await _hubContext.Clients.Group("SuperAdminUpdates")
                .SendAsync("ImpersonationEnded", new
                {
                    SuperAdminId = currentUserId.Value,
                    SuperAdminUsername = currentUser.Username,
                    Timestamp = DateTime.UtcNow
                });

            // Return to SuperAdmin context
            var context = new CurrentContextDto
            {
                UserId = currentUser.Id,
                Username = currentUser.Username,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                Roles = new List<string> { "SuperAdmin" },
                CurrentTenantId = currentUser.TenantId,
                CurrentTenantName = "SuperAdmin",
                OriginalTenantId = currentUser.TenantId,
                OriginalTenantName = "SuperAdmin",
                IsImpersonating = false,
                ImpersonatedUserId = null,
                ImpersonatedUsername = null,
                IsSuperAdmin = true,
                SessionId = HttpContext.Session.Id,
                LoginTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending impersonation");
            return StatusCode(500, new { message = "Error ending impersonation", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets tenant switching history.
    /// </summary>
    [HttpGet("history/tenant-switches")]
    public async Task<ActionResult<PagedResult<TenantSwitchHistoryDto>>> GetTenantSwitchHistory([FromQuery] OperationHistorySearchDto searchDto)
    {
        try
        {
            var query = _context.AuditTrails
                .Where(a => a.OperationType == AuthAuditOperationType.TenantSwitch)
                .AsQueryable();

            // Apply filters
            if (searchDto.UserId.HasValue)
            {
                query = query.Where(a => a.PerformedByUserId == searchDto.UserId.Value);
            }

            if (searchDto.TenantId.HasValue)
            {
                query = query.Where(a => a.TargetTenantId == searchDto.TenantId.Value || a.SourceTenantId == searchDto.TenantId.Value);
            }

            if (searchDto.FromDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt >= searchDto.FromDate.Value);
            }

            if (searchDto.ToDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt <= searchDto.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(searchDto.SessionId))
            {
                query = query.Where(a => a.SessionId == searchDto.SessionId);
            }

            // Apply sorting
            var isDesc = searchDto.SortOrder?.ToLower() == "desc";
            query = searchDto.SortBy?.ToLower() switch
            {
                "performedat" => isDesc ? query.OrderByDescending(a => a.PerformedAt) : query.OrderBy(a => a.PerformedAt),
                _ => isDesc ? query.OrderByDescending(a => a.PerformedAt) : query.OrderBy(a => a.PerformedAt)
            };

            var totalCount = await query.CountAsync();
            var auditTrails = await query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var results = new List<TenantSwitchHistoryDto>();
            foreach (var audit in auditTrails)
            {
                var user = await _context.Users.FindAsync(audit.PerformedByUserId);
                var fromTenant = audit.SourceTenantId.HasValue ? await _context.Tenants.FindAsync(audit.SourceTenantId.Value) : null;
                var toTenant = audit.TargetTenantId.HasValue ? await _context.Tenants.FindAsync(audit.TargetTenantId.Value) : null;

                results.Add(new TenantSwitchHistoryDto
                {
                    Id = audit.Id,
                    UserId = audit.PerformedByUserId,
                    Username = user?.Username ?? "Unknown",
                    FromTenantId = audit.SourceTenantId,
                    FromTenantName = fromTenant?.Name,
                    ToTenantId = audit.TargetTenantId ?? Guid.Empty,
                    ToTenantName = toTenant?.Name ?? "Unknown",
                    Reason = ExtractReasonFromDetails(audit.Details),
                    SwitchedAt = audit.PerformedAt,
                    SwitchedBackAt = null, // Would need to track this separately
                    IsActive = false, // Would need to track this from session state
                    SessionId = audit.SessionId,
                    IpAddress = audit.IpAddress
                });
            }

            var response = new PagedResult<TenantSwitchHistoryDto>
            {
                Items = results,
                TotalCount = totalCount,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant switch history");
            return StatusCode(500, new { message = "Error retrieving tenant switch history", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets user impersonation history.
    /// </summary>
    [HttpGet("history/impersonations")]
    public async Task<ActionResult<PagedResult<ImpersonationHistoryDto>>> GetImpersonationHistory([FromQuery] OperationHistorySearchDto searchDto)
    {
        try
        {
            var query = _context.AuditTrails
                .Where(a => a.OperationType == AuthAuditOperationType.ImpersonationStart || a.OperationType == AuthAuditOperationType.ImpersonationEnd)
                .AsQueryable();

            // Apply filters similar to tenant switch history
            if (searchDto.UserId.HasValue)
            {
                query = query.Where(a => a.PerformedByUserId == searchDto.UserId.Value || a.TargetUserId == searchDto.UserId.Value);
            }

            if (searchDto.FromDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt >= searchDto.FromDate.Value);
            }

            if (searchDto.ToDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt <= searchDto.ToDate.Value);
            }

            var totalCount = await query.CountAsync();
            var auditTrails = await query
                .OrderByDescending(a => a.PerformedAt)
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var results = new List<ImpersonationHistoryDto>();
            // Group by session and match start/end operations
            var grouped = auditTrails.GroupBy(a => a.SessionId);

            foreach (var group in grouped)
            {
                var startOperation = group.FirstOrDefault(a => a.OperationType == AuthAuditOperationType.ImpersonationStart);
                var endOperation = group.FirstOrDefault(a => a.OperationType == AuthAuditOperationType.ImpersonationEnd);

                if (startOperation != null)
                {
                    var impersonator = await _context.Users.FindAsync(startOperation.PerformedByUserId);
                    var impersonated = startOperation.TargetUserId.HasValue ?
                        await _context.Users.FindAsync(startOperation.TargetUserId.Value) : null;
                    var tenant = startOperation.TargetTenantId.HasValue ?
                        await _context.Tenants.FindAsync(startOperation.TargetTenantId.Value) : null;

                    results.Add(new ImpersonationHistoryDto
                    {
                        Id = startOperation.Id,
                        ImpersonatorUserId = startOperation.PerformedByUserId,
                        ImpersonatorUsername = impersonator?.Username ?? "Unknown",
                        ImpersonatedUserId = startOperation.TargetUserId ?? Guid.Empty,
                        ImpersonatedUsername = impersonated?.Username ?? "Unknown",
                        TenantId = startOperation.TargetTenantId,
                        TenantName = tenant?.Name,
                        Reason = ExtractReasonFromDetails(startOperation.Details),
                        StartedAt = startOperation.PerformedAt,
                        EndedAt = endOperation?.PerformedAt,
                        IsActive = endOperation == null,
                        SessionId = startOperation.SessionId,
                        IpAddress = startOperation.IpAddress,
                        ActionsPerformed = new List<string>() // Would need to track this separately
                    });
                }
            }

            var response = new PagedResult<ImpersonationHistoryDto>
            {
                Items = results.OrderByDescending(r => r.StartedAt).ToList(),
                TotalCount = totalCount,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving impersonation history");
            return StatusCode(500, new { message = "Error retrieving impersonation history", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets operation summary statistics.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<OperationSummaryDto>> GetOperationSummary()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var oneWeekAgo = today.AddDays(-7);
            var oneMonthAgo = today.AddMonths(-1);

            var tenantSwitches = await _context.AuditTrails
                .Where(a => a.OperationType == AuthAuditOperationType.TenantSwitch)
                .ToListAsync();

            var impersonations = await _context.AuditTrails
                .Where(a => a.OperationType == AuthAuditOperationType.ImpersonationStart)
                .ToListAsync();

            var summary = new OperationSummaryDto
            {
                TotalTenantSwitches = tenantSwitches.Count,
                ActiveTenantSwitches = 0, // Would need session tracking
                TotalImpersonations = impersonations.Count,
                ActiveImpersonations = 0, // Would need session tracking
                OperationsToday = tenantSwitches.Count(a => a.PerformedAt >= today) + impersonations.Count(a => a.PerformedAt >= today),
                OperationsThisWeek = tenantSwitches.Count(a => a.PerformedAt >= oneWeekAgo) + impersonations.Count(a => a.PerformedAt >= oneWeekAgo),
                OperationsThisMonth = tenantSwitches.Count(a => a.PerformedAt >= oneMonthAgo) + impersonations.Count(a => a.PerformedAt >= oneMonthAgo)
            };

            // Get recent operations
            var recentOperations = await _context.AuditTrails
                .Where(a => a.OperationType == AuthAuditOperationType.TenantSwitch ||
                           a.OperationType == AuthAuditOperationType.ImpersonationStart ||
                           a.OperationType == AuthAuditOperationType.ImpersonationEnd)
                .OrderByDescending(a => a.PerformedAt)
                .Take(10)
                .ToListAsync();

            summary.RecentOperations = recentOperations.Select(a => new RecentOperationDto
            {
                Type = a.OperationType.ToString(),
                Username = "SuperAdmin", // Would get from user lookup
                TargetUsername = "Target", // Would get from user lookup
                TenantName = "Tenant", // Would get from tenant lookup
                Timestamp = a.PerformedAt,
                IsActive = false, // Would determine from session state
                Reason = ExtractReasonFromDetails(a.Details)
            }).ToList();

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operation summary");
            return StatusCode(500, new { message = "Error retrieving operation summary", error = ex.Message });
        }
    }

    private string? ExtractReasonFromDetails(string? details)
    {
        if (string.IsNullOrEmpty(details))
            return null;

        var reasonIndex = details.IndexOf("Reason: ");
        if (reasonIndex >= 0)
        {
            return details.Substring(reasonIndex + 8);
        }

        return null;
    }
}