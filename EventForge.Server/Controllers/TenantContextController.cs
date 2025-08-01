using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant context operations (tenant switching and user impersonation).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class TenantContextController : BaseApiController
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
    /// <response code="200">Returns current tenant context information</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user does not have admin privileges</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    /// <returns>Success message with new tenant ID</returns>
    /// <response code="200">Tenant context switched successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user does not have permission to switch tenants</response>
    [HttpPost("switch-tenant")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchTenant([FromBody] SwitchTenantRequest request)
    {
        try
        {
            await _tenantContext.SetTenantContextAsync(request.TenantId, request.Reason);
            return Ok(new { Message = "Tenant context switched successfully", TenantId = request.TenantId });
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
    public async Task<ActionResult<PagedResult<AuditTrailResponseDto>>> GetAuditTrail(
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