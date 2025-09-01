using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document analytics and KPI management with multi-tenant support.
/// Provides analytics, reporting, and KPI tracking for document workflows.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentAnalyticsController : BaseApiController
{
    private readonly IDocumentAnalyticsService _analyticsService;
    private readonly ITenantContext _tenantContext;

    public DocumentAnalyticsController(
        IDocumentAnalyticsService analyticsService,
        ITenantContext tenantContext)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets analytics for a specific document.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document analytics</returns>
    /// <response code="200">Returns the document analytics</response>
    /// <response code="404">If the document analytics are not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document/{documentHeaderId:guid}")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> GetDocumentAnalytics(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var analytics = await _analyticsService.GetDocumentAnalyticsAsync(documentHeaderId, cancellationToken);

            if (analytics == null)
                return CreateNotFoundProblem($"Analytics for document {documentHeaderId} not found.");

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document analytics.", ex);
        }
    }

    /// <summary>
    /// Gets analytics summary with grouping and filtering.
    /// </summary>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="groupBy">Group by option: time, documentType, department</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    /// <response code="200">Returns the analytics summary</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DocumentAnalyticsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsSummaryDto>> GetAnalyticsSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? groupBy = "documentType",
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        // Validate groupBy parameter
        if (!string.IsNullOrEmpty(groupBy) &&
            !new[] { "time", "documentType", "department" }.Contains(groupBy, StringComparer.OrdinalIgnoreCase))
        {
            return CreateValidationProblemDetails("Group by must be one of: time, documentType, department");
        }

        // Validate date range
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return CreateValidationProblemDetails("Start date cannot be after end date");
        }

        try
        {
            var summary = await _analyticsService.GetAnalyticsSummaryAsync(from, to, groupBy, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving analytics summary.", ex);
        }
    }

    /// <summary>
    /// Gets KPI summary for documents in date range.
    /// </summary>
    /// <param name="from">Start date (required)</param>
    /// <param name="to">End date (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>KPI summary</returns>
    /// <response code="200">Returns the KPI summary</response>
    /// <response code="400">If the date parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("kpi")]
    [ProducesResponseType(typeof(DocumentKpiSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentKpiSummaryDto>> GetKpiSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        // Validate date range
        if (from > to)
        {
            return CreateValidationProblemDetails("Start date cannot be after end date");
        }

        if (from == default || to == default)
        {
            return CreateValidationProblemDetails("Both start and end dates are required");
        }

        // Limit date range to prevent performance issues
        if ((to - from).TotalDays > 365)
        {
            return CreateValidationProblemDetails("Date range cannot exceed 365 days");
        }

        try
        {
            var kpiSummary = await _analyticsService.CalculateKpiSummaryAsync(from, to, cancellationToken);
            return Ok(kpiSummary);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating KPI summary.", ex);
        }
    }

    /// <summary>
    /// Creates or updates analytics for a specific document.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics</returns>
    /// <response code="200">Returns the updated analytics</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("document/{documentHeaderId:guid}/refresh")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> RefreshDocumentAnalytics(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await _analyticsService.CreateOrUpdateAnalyticsAsync(documentHeaderId, currentUser, cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while refreshing document analytics.", ex);
        }
    }

    /// <summary>
    /// Handles workflow events for analytics tracking.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="eventType">Workflow event type</param>
    /// <param name="eventData">Additional event data (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics</returns>
    /// <response code="200">Returns the updated analytics</response>
    /// <response code="400">If the event type is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("document/{documentHeaderId:guid}/events")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> HandleWorkflowEvent(
        Guid documentHeaderId,
        [FromQuery] string eventType,
        [FromBody] object? eventData = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return CreateValidationProblemDetails("Event type is required");
        }

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await _analyticsService.HandleWorkflowEventAsync(
                documentHeaderId, eventType, eventData, currentUser, cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while handling workflow event.", ex);
        }
    }
}