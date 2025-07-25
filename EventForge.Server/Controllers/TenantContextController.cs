using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant context operations (tenant switching and user impersonation).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class TenantContextController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantService _tenantService;

    public TenantContextController(ITenantContext tenantContext, ITenantService tenantService)
    {
        _tenantContext = tenantContext;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets the current tenant context information.
    /// </summary>
    /// <returns>Current tenant context details</returns>
    [HttpGet("current")]
    public async Task<ActionResult<object>> GetCurrentContext()
    {
        var result = new
        {
            CurrentTenantId = _tenantContext.CurrentTenantId,
            CurrentUserId = _tenantContext.CurrentUserId,
            IsSuperAdmin = _tenantContext.IsSuperAdmin,
            IsImpersonating = _tenantContext.IsImpersonating,
            ManageableTenants = _tenantContext.IsSuperAdmin ? await _tenantContext.GetManageableTenantsAsync() : null
        };

        return Ok(result);
    }

    /// <summary>
    /// Switches to a different tenant context (super admin only).
    /// </summary>
    /// <param name="request">Tenant switch request</param>
    [HttpPost("switch-tenant")]
    public async Task<IActionResult> SwitchTenant([FromBody] SwitchTenantRequest request)
    {
        try
        {
            await _tenantContext.SetTenantContextAsync(request.TenantId, request.Reason);
            return Ok(new { Message = "Tenant context switched successfully", TenantId = request.TenantId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Starts impersonating a user (super admin only).
    /// </summary>
    /// <param name="request">Impersonation request</param>
    [HttpPost("start-impersonation")]
    public async Task<IActionResult> StartImpersonation([FromBody] StartImpersonationRequest request)
    {
        try
        {
            await _tenantContext.StartImpersonationAsync(request.UserId, request.Reason);
            return Ok(new { Message = "User impersonation started successfully", UserId = request.UserId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Ends impersonation and returns to original super admin context.
    /// </summary>
    /// <param name="request">End impersonation request</param>
    [HttpPost("end-impersonation")]
    public async Task<IActionResult> EndImpersonation([FromBody] EndImpersonationRequest request)
    {
        try
        {
            await _tenantContext.EndImpersonationAsync(request.Reason);
            return Ok(new { Message = "User impersonation ended successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets audit trail for tenant operations (super admin/auditor only).
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>Paginated audit trail entries</returns>
    [HttpGet("audit-trail")]
    public async Task<ActionResult<PaginatedResponse<AuditTrailResponseDto>>> GetAuditTrail(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] AuditOperationType? operationType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _tenantService.GetAuditTrailAsync(tenantId, operationType, pageNumber, pageSize);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Validates if the current user can access a specific tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID to validate</param>
    [HttpGet("validate-access/{tenantId}")]
    public async Task<ActionResult<object>> ValidateAccess(Guid tenantId)
    {
        var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
        return Ok(new { TenantId = tenantId, CanAccess = canAccess });
    }
}

/// <summary>
/// Request DTO for tenant switching.
/// </summary>
public class SwitchTenantRequest
{
    /// <summary>
    /// Target tenant ID to switch to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Reason for the tenant switch.
    /// </summary>
    public string Reason { get; set; } = "Tenant switch by admin";
}

/// <summary>
/// Request DTO for starting user impersonation.
/// </summary>
public class StartImpersonationRequest
{
    /// <summary>
    /// User ID to impersonate.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reason for impersonation.
    /// </summary>
    public string Reason { get; set; } = "User impersonation by admin";
}

/// <summary>
/// Request DTO for ending user impersonation.
/// </summary>
public class EndImpersonationRequest
{
    /// <summary>
    /// Reason for ending impersonation.
    /// </summary>
    public string Reason { get; set; } = "Impersonation ended by admin";
}