using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{
    /// <summary>
    /// Gets analytics for a specific document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document analytics</returns>
    /// <response code="200">Returns the document analytics</response>
    /// <response code="404">If analytics are not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/analytics")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> GetDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var analytics = await documentFacade.GetAnalyticsAsync(documentId, cancellationToken);

            if (analytics == null)
                return CreateNotFoundProblem($"Analytics for document with ID {documentId} not found.");

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
    [HttpGet("analytics/summary")]
    [ProducesResponseType(typeof(DocumentAnalyticsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsSummaryDto>> GetAnalyticsSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? groupBy = "documentType",
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
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
            var summary = await documentFacade.GetAnalyticsSummaryAsync(from, to, groupBy, cancellationToken);
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
    [HttpGet("analytics/kpi")]
    [ProducesResponseType(typeof(DocumentKpiSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentKpiSummaryDto>> GetKpiSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
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
            var kpiSummary = await documentFacade.CalculateKpiSummaryAsync(from, to, cancellationToken);
            return Ok(kpiSummary);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating KPI summary.", ex);
        }
    }

    /// <summary>
    /// Refreshes (creates or updates) analytics for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document analytics</returns>
    /// <response code="200">Returns the updated document analytics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/analytics/refresh")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> RefreshDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await documentFacade.RefreshAnalyticsAsync(documentId, currentUser, cancellationToken);
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
    /// <param name="documentId">Document header ID</param>
    /// <param name="eventType">Workflow event type</param>
    /// <param name="eventData">Additional event data (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics</returns>
    /// <response code="200">Returns the updated analytics</response>
    /// <response code="400">If the event type is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/analytics/events")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> HandleWorkflowEvent(
        Guid documentId,
        [FromQuery] string eventType,
        [FromBody] object? eventData = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return CreateValidationProblemDetails("Event type is required");
        }

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await documentFacade.HandleWorkflowEventAsync(
                documentId, eventType, eventData, currentUser, cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while handling workflow event.", ex);
        }
    }

}
