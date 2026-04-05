using EventForge.DTOs.Monitoring;
using EventForge.Server.Services.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for the monitoring dashboard (Sprint 4 — Fase 6 Optimization).
/// Accessible only by Admin and SuperAdmin roles.
/// </summary>
[Route("api/v1/monitoring")]
[Authorize(Roles = "Admin,SuperAdmin")]
[ApiController]
public class MonitoringController(
    IMonitoringService monitoringService,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Returns a monitoring dashboard snapshot for the current tenant.
    /// </summary>
    /// <param name="topN">Number of top promotions to include (default 10, max 50).</param>
    /// <param name="recentErrorCount">Number of recent error entries to include (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Monitoring dashboard snapshot.</returns>
    /// <response code="200">Returns the monitoring dashboard snapshot.</response>
    /// <response code="400">If the tenant context is missing.</response>
    /// <response code="403">If the caller does not have Admin or SuperAdmin role.</response>
    /// <response code="500">If an internal error occurs.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(MonitoringDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MonitoringDashboardDto>> GetDashboard(
        [FromQuery] int topN = 10,
        [FromQuery] int recentErrorCount = 20,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        topN = Math.Clamp(topN, 1, 50);
        recentErrorCount = Math.Clamp(recentErrorCount, 1, 100);

        try
        {
            var result = await monitoringService.GetDashboardAsync(topN, recentErrorCount, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the monitoring dashboard.", ex);
        }
    }
}
