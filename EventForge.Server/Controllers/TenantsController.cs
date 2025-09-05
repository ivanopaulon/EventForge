using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = EventForge.DTOs.Common.AuditOperationType;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant management operations (super admin only).
/// Provides comprehensive CRUD operations for tenant management with proper multi-tenant support.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class TenantsController : BaseApiController
{
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;
    private readonly EventForgeDbContext _context;

    public TenantsController(ITenantService tenantService, ITenantContext tenantContext, EventForgeDbContext context)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _context = context;
    }

    /// <summary>
    /// Creates a new tenant without generating any default admin users.
    /// </summary>
    /// <param name="createDto">Tenant creation data</param>
    /// <returns>Created tenant details</returns>
    /// <response code="201">Returns the newly created tenant</response>
    /// <response code="400">If the tenant data is invalid</response>
    /// <response code="500">If an error occurred during creation</response>
    [HttpPost]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantResponseDto>> CreateTenant([FromBody] CreateTenantDto createDto)
    {
        try
        {
            var result = await _tenantService.CreateTenantAsync(createDto);
            return CreatedAtAction(nameof(GetTenant), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Creates a new tenant with a default admin user (SuperAdmin only).
    /// </summary>
    /// <param name="createDto">Tenant creation data including admin user information</param>
    /// <returns>Created tenant details with admin user information</returns>
    /// <response code="201">Returns the newly created tenant with admin user</response>
    /// <response code="400">If the tenant data is invalid</response>
    /// <response code="500">If an error occurred during creation</response>
    [HttpPost("with-admin")]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantResponseDto>> CreateTenantWithAdmin([FromBody] CreateTenantDto createDto)
    {
        try
        {
            var result = await _tenantService.CreateTenantWithAdminAsync(createDto);
            return CreatedAtAction(nameof(GetTenant), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Tenant details</returns>
    /// <response code="200">Returns the tenant</response>
    /// <response code="404">If the tenant is not found</response>
    /// <response code="500">If an error occurred</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantResponseDto>> GetTenant(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantAsync(id);
            if (tenant == null)
            {
                return CreateNotFoundProblem("Tenant {id} not found.");
            }
            return Ok(tenant);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
    }

    /// <summary>
    /// Gets all tenants (super admin only).
    /// </summary>
    /// <returns>List of all tenants</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
    }

    /// <summary>
    /// Updates tenant information (DisplayName, Description, Domain, ContactEmail, MaxUsers, SubscriptionExpiresAt).
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="updateDto">Updated tenant data</param>
    /// <returns>Updated tenant details</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<TenantResponseDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantDto updateDto)
    {
        try
        {
            var result = await _tenantService.UpdateTenantAsync(id, updateDto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Enables a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="reason">Reason for enabling the tenant</param>
    [HttpPost("{id}/enable")]
    public async Task<IActionResult> EnableTenant(Guid id, [FromBody] string reason = "Enabled by admin")
    {
        try
        {
            await _tenantService.SetTenantStatusAsync(id, true, reason);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Disables a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="reason">Reason for disabling the tenant</param>
    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableTenant(Guid id, [FromBody] string reason = "Disabled by admin")
    {
        try
        {
            await _tenantService.SetTenantStatusAsync(id, false, reason);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Gets all admins for a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>List of tenant admins</returns>
    [HttpGet("{id}/admins")]
    public async Task<ActionResult<IEnumerable<AdminTenantResponseDto>>> GetTenantAdmins(Guid id)
    {
        try
        {
            var admins = await _tenantService.GetTenantAdminsAsync(id);
            return Ok(admins);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
    }

    /// <summary>
    /// Adds an admin to a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="userId">User ID to make admin</param>
    /// <param name="accessLevel">Admin access level</param>
    /// <returns>Admin tenant mapping details</returns>
    [HttpPost("{id}/admins/{userId}")]
    public async Task<ActionResult<AdminTenantResponseDto>> AddTenantAdmin(
        Guid id,
        Guid userId,
        [FromQuery] AdminAccessLevel accessLevel = AdminAccessLevel.TenantAdmin)
    {
        try
        {
            var result = await _tenantService.AddTenantAdminAsync(id, userId, accessLevel);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Removes an admin from a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="userId">User ID to remove as admin</param>
    [HttpDelete("{id}/admins/{userId}")]
    public async Task<IActionResult> RemoveTenantAdmin(Guid id, Guid userId)
    {
        try
        {
            await _tenantService.RemoveTenantAdminAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Forces a user to change their password on next login.
    /// </summary>
    /// <param name="id">Tenant ID (for context)</param>
    /// <param name="userId">User ID</param>
    [HttpPost("{id}/users/{userId}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid id, Guid userId)
    {
        try
        {
            await _tenantService.ForcePasswordChangeAsync(userId);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Gets tenant statistics for the dashboard.
    /// </summary>
    /// <returns>Tenant statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<TenantStatisticsDto>> GetTenantStatistics()
    {
        try
        {
            var statistics = await _tenantService.GetTenantStatisticsAsync();
            return Ok(statistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving tenant statistics", ex);
        }
    }

    /// <summary>
    /// Searches tenants with advanced filtering.
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated tenant results</returns>
    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<TenantResponseDto>>> SearchTenants([FromBody] TenantSearchDto searchDto)
    {
        try
        {
            var results = await _tenantService.SearchTenantsAsync(searchDto);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error searching tenants", ex);
        }
    }

    /// <summary>
    /// Gets detailed information for a tenant including limits and usage.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Detailed tenant information</returns>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<TenantDetailDto>> GetTenantDetails(Guid id)
    {
        try
        {
            var details = await _tenantService.GetTenantDetailsAsync(id);
            if (details == null)
            {
                return CreateNotFoundProblem("Tenant {id} not found.");
            }
            return Ok(details);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving tenant details", ex);
        }
    }

    /// <summary>
    /// Gets tenant limits and usage information.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Tenant limits information</returns>
    [HttpGet("{id}/limits")]
    public async Task<ActionResult<TenantLimitsDto>> GetTenantLimits(Guid id)
    {
        try
        {
            var limits = await _tenantService.GetTenantLimitsAsync(id);
            if (limits == null)
            {
                return CreateNotFoundProblem("Tenant {id} not found.");
            }
            return Ok(limits);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving tenant limits", ex);
        }
    }

    /// <summary>
    /// Updates tenant limits.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="updateDto">Updated limits data</param>
    /// <returns>Updated limits information</returns>
    [HttpPut("{id}/limits")]
    public async Task<ActionResult<TenantLimitsDto>> UpdateTenantLimits(Guid id, [FromBody] UpdateTenantLimitsDto updateDto)
    {
        try
        {
            var result = await _tenantService.UpdateTenantLimitsAsync(id, updateDto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating tenant limits", ex);
        }
    }

    /// <summary>
    /// Gets live tenant statistics with real-time updates.
    /// </summary>
    /// <returns>Live tenant statistics including active users, current sessions, and usage metrics</returns>
    [HttpGet("statistics/live")]
    public async Task<ActionResult<object>> GetLiveStatistics()
    {
        try
        {
            // Get basic statistics
            var statistics = await _tenantService.GetTenantStatisticsAsync();

            // Get additional live metrics
            var liveStats = new
            {
                // Basic statistics from service
                BasicStats = statistics,

                // Live metrics calculated in real-time
                CurrentTimestamp = DateTime.UtcNow,
                ActiveSessions = await GetActiveSessionsCount(),
                RecentActivity = await GetRecentActivitySummary(),
                SystemHealth = await GetSystemHealthMetrics(),
                TenantUsage = await GetTenantUsageMetrics()
            };

            return Ok(liveStats);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving live tenant statistics", ex);
        }
    }

    /// <summary>
    /// Gets real-time tenant activity stream for monitoring.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="limit">Number of recent activities to return</param>
    /// <returns>Real-time activity stream</returns>
    [HttpGet("activity/live")]
    public async Task<ActionResult<object>> GetLiveActivity([FromQuery] Guid? tenantId = null, [FromQuery] int limit = 50)
    {
        try
        {
            var activities = await GetRecentActivities(tenantId, limit);

            var result = new
            {
                Timestamp = DateTime.UtcNow,
                TenantId = tenantId,
                Activities = activities,
                Count = activities.Count(),
                HasMore = activities.Count() >= limit
            };

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving live activity", ex);
        }
    }

    #region Private Helper Methods for Live Statistics

    private async Task<int> GetActiveSessionsCount()
    {
        // Get count of active user sessions from the last hour
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        return await _context.AuditTrails
            .Where(at => at.PerformedAt >= oneHourAgo &&
                        (at.OperationType == AuthAuditOperationType.TenantSwitch ||
                         at.OperationType == AuthAuditOperationType.ImpersonationStart))
            .Select(at => at.PerformedByUserId)
            .Distinct()
            .CountAsync();
    }

    private async Task<object> GetRecentActivitySummary()
    {
        var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);

        var activities = await _context.AuditTrails
            .Where(at => at.PerformedAt >= fifteenMinutesAgo)
            .GroupBy(at => at.OperationType)
            .Select(g => new
            {
                OperationType = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        return new
        {
            TimeWindow = "Last 15 minutes",
            Activities = activities,
            TotalOperations = activities.Sum(a => a.Count)
        };
    }

    private async Task<object> GetSystemHealthMetrics()
    {
        var now = DateTime.UtcNow;
        var oneDayAgo = now.AddDays(-1);

        // Calculate system health metrics
        var totalTenants = await _context.Tenants.CountAsync(t => !t.IsDeleted);
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive && !t.IsDeleted);
        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);

        var recentErrors = await _context.AuditTrails
            .Where(at => at.PerformedAt >= oneDayAgo && !at.WasSuccessful)
            .CountAsync();

        return new
        {
            TenantsHealth = new { Total = totalTenants, Active = activeTenants, HealthPercentage = totalTenants > 0 ? (activeTenants * 100.0 / totalTenants) : 0 },
            UsersHealth = new { Total = totalUsers, Active = activeUsers, HealthPercentage = totalUsers > 0 ? (activeUsers * 100.0 / totalUsers) : 0 },
            ErrorRate = new { Last24Hours = recentErrors, Status = recentErrors < 10 ? "Healthy" : recentErrors < 50 ? "Warning" : "Critical" },
            Timestamp = now
        };
    }

    private async Task<object> GetTenantUsageMetrics()
    {
        var tenantUsage = await _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new
            {
                TenantId = t.Id,
                TenantName = t.Name,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id && !u.IsDeleted),
                ActiveUserCount = _context.Users.Count(u => u.TenantId == t.Id && u.IsActive && !u.IsDeleted),
                MaxUsers = t.MaxUsers,
                LastActivity = _context.AuditTrails
                    .Where(at => at.SourceTenantId == t.Id || at.TargetTenantId == t.Id)
                    .OrderByDescending(at => at.PerformedAt)
                    .Select(at => (DateTime?)at.PerformedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new
        {
            TotalTenants = tenantUsage.Count,
            TenantUsage = tenantUsage.Select(tu => new
            {
                tu.TenantId,
                tu.TenantName,
                tu.UserCount,
                tu.ActiveUserCount,
                tu.MaxUsers,
                UsagePercentage = tu.MaxUsers > 0 ? (tu.UserCount * 100.0 / tu.MaxUsers) : 0,
                tu.LastActivity,
                Status = tu.LastActivity.HasValue && tu.LastActivity > DateTime.UtcNow.AddDays(-7) ? "Active" : "Inactive"
            })
        };
    }

    private async Task<IEnumerable<object>> GetRecentActivities(Guid? tenantId, int limit)
    {
        var query = _context.AuditTrails.AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(at => at.SourceTenantId == tenantId || at.TargetTenantId == tenantId);
        }

        var activities = await query
            .OrderByDescending(at => at.PerformedAt)
            .Take(limit)
            .Select(at => new
            {
                at.Id,
                at.OperationType,
                at.PerformedAt,
                at.PerformedByUserId,
                at.SourceTenantId,
                at.TargetTenantId,
                at.TargetUserId,
                at.WasSuccessful,
                at.Details,
                at.IpAddress,
                UserName = _context.Users
                    .Where(u => u.Id == at.PerformedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault(),
                SourceTenantName = at.SourceTenantId.HasValue ?
                    _context.Tenants
                        .Where(t => t.Id == at.SourceTenantId)
                        .Select(t => t.Name)
                        .FirstOrDefault() : null,
                TargetTenantName = at.TargetTenantId.HasValue ?
                    _context.Tenants
                        .Where(t => t.Id == at.TargetTenantId)
                        .Select(t => t.Name)
                        .FirstOrDefault() : null
            })
            .ToListAsync();

        return activities;
    }

    #endregion

    /// <summary>
    /// Soft delete di un tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="reason">Motivazione della cancellazione</param>
    [HttpDelete("{id}/soft")]
    public async Task<IActionResult> SoftDeleteTenant(Guid id, [FromBody] string reason = "Soft deleted by admin")
    {
        try
        {
            await _tenantService.SoftDeleteTenantAsync(id, reason);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateValidationProblemDetails("Access denied: " + ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Ritorna i tenant disponibili per il login (solo tenant attivi, senza dati sensibili).
    /// Endpoint pubblico per la schermata di login.
    /// </summary>
    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TenantResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetAvailableTenants()
    {
        try
        {
            var tenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .Select(t => new TenantResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Code = t.Code,
                    Description = t.Description,
                    Domain = t.Domain,
                    ContactEmail = t.ContactEmail,
                    MaxUsers = t.MaxUsers,
                    IsActive = t.IsActive,
                    SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                    CreatedAt = t.CreatedAt
                    // Evita di esporre campi sensibili o relazioni
                })
                .ToListAsync();

            return Ok(tenants);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Unable to load tenants");
        }
    }
}