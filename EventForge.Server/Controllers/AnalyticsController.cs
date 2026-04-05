using EventForge.DTOs.Analytics;
using EventForge.Server.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for analytics and reporting (Fase 4 - Reportistica).
/// Provides aggregated analytics data for promotions, pricing and sales.
/// </summary>
[Route("api/v1/analytics")]
[Authorize]
[ApiController]
public class AnalyticsController(
    IAnalyticsService analyticsService,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Returns promotion analytics dashboard data.
    /// </summary>
    /// <param name="filter">Date range and grouping filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated promotion analytics dashboard.</returns>
    /// <response code="200">Returns the promotion analytics dashboard.</response>
    /// <response code="400">If the tenant context is missing.</response>
    /// <response code="500">If an internal error occurs.</response>
    [HttpGet("promotions")]
    [ProducesResponseType(typeof(PromotionAnalyticsDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PromotionAnalyticsDashboardDto>> GetPromotionAnalytics(
        [FromQuery] AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await analyticsService.GetPromotionAnalyticsAsync(filter, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving promotion analytics.", ex);
        }
    }

    /// <summary>
    /// Returns pricing analytics dashboard data.
    /// </summary>
    /// <param name="filter">Date range and grouping filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated pricing analytics dashboard.</returns>
    /// <response code="200">Returns the pricing analytics dashboard.</response>
    /// <response code="400">If the tenant context is missing.</response>
    /// <response code="500">If an internal error occurs.</response>
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(PricingAnalyticsDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PricingAnalyticsDashboardDto>> GetPricingAnalytics(
        [FromQuery] AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await analyticsService.GetPricingAnalyticsAsync(filter, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving pricing analytics.", ex);
        }
    }

    /// <summary>
    /// Returns sales analytics dashboard data.
    /// </summary>
    /// <param name="filter">Date range and grouping filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated sales analytics dashboard.</returns>
    /// <response code="200">Returns the sales analytics dashboard.</response>
    /// <response code="400">If the tenant context is missing.</response>
    /// <response code="500">If an internal error occurs.</response>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(SalesAnalyticsDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SalesAnalyticsDashboardDto>> GetSalesAnalytics(
        [FromQuery] AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await analyticsService.GetSalesAnalyticsAsync(filter, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving sales analytics.", ex);
        }
    }
}
