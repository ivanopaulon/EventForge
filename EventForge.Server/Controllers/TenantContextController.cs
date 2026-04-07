using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant context operations (tenant switching and user impersonation).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class TenantContextController(ITenantContext tenantContext, ITenantService tenantService) : BaseApiController
{

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
    public async Task<ActionResult<object>> GetCurrentContext(CancellationToken cancellationToken = default)
    {
        var result = new
        {
            CurrentTenantId = tenantContext.CurrentTenantId,
            CurrentUserId = tenantContext.CurrentUserId,
            IsSuperAdmin = tenantContext.IsSuperAdmin,
            IsImpersonating = tenantContext.IsImpersonating,
            ManageableTenants = tenantContext.IsSuperAdmin ? await tenantContext.GetManageableTenantsAsync(cancellationToken) : null
        };

        return Ok(result);
    }

    /// <summary>
    /// Switches to a different tenant context (super admin only).
    /// </summary>
    /// <param name="request">Tenant switch request</param>
    /// <param name="cancellationToken">Cancellation token</param>
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
    public async Task<IActionResult> SwitchTenant([FromBody] SwitchTenantRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await tenantContext.SetTenantContextAsync(request.TenantId, request.Reason, cancellationToken);
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
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("start-impersonation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartImpersonation([FromBody] StartImpersonationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await tenantContext.StartImpersonationAsync(request.UserId, request.Reason, cancellationToken);
            return Ok(new { Message = "User impersonation started successfully", UserId = request.UserId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateForbiddenProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
    }

    /// <summary>
    /// Ends impersonation and returns to original super admin context.
    /// </summary>
    /// <param name="request">End impersonation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("end-impersonation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EndImpersonation([FromBody] EndImpersonationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await tenantContext.EndImpersonationAsync(request.Reason, cancellationToken);
            return Ok(new { Message = "User impersonation ended successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Gets audit trail for tenant operations (super admin/auditor only).
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit trail entries</returns>
    [HttpGet("audit-trail")]
    [ProducesResponseType(typeof(PagedResult<EventForge.DTOs.SuperAdmin.AuditTrailResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<EventForge.DTOs.SuperAdmin.AuditTrailResponseDto>>> GetAuditTrail(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] AuditOperationType? operationType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await tenantService.GetAuditTrailAsync(tenantId, operationType, pageNumber, pageSize);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateForbiddenProblem(ex.Message);
        }
    }

    /// <summary>
    /// Validates if the current user can access a specific tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("validate-access/{tenantId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> ValidateAccess(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var canAccess = await tenantContext.CanAccessTenantAsync(tenantId, cancellationToken);
        return Ok(new { TenantId = tenantId, CanAccess = canAccess });
    }
}