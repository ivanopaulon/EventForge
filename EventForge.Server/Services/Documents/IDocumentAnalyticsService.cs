using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for document analytics and KPI tracking
/// </summary>
public interface IDocumentAnalyticsService
{
    /// <summary>
    /// Creates or updates analytics for a document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics DTO</returns>
    Task<DocumentAnalyticsDto> CreateOrUpdateAnalyticsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics DTO or null if not found</returns>
    Task<DocumentAnalyticsDto?> GetDocumentAnalyticsAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics summary with grouping and filtering
    /// </summary>
    /// <param name="from">Start date filter</param>
    /// <param name="to">End date filter</param>
    /// <param name="groupBy">Group by option (time, documentType, department)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary data</returns>
    Task<DocumentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates analytics based on workflow events
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="eventType">Workflow event type</param>
    /// <param name="eventData">Additional event data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics</returns>
    Task<DocumentAnalyticsDto> HandleWorkflowEventAsync(
        Guid documentHeaderId,
        string eventType,
        object? eventData,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates KPIs for documents in date range
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>KPI summary</returns>
    Task<DocumentKpiSummaryDto> CalculateKpiSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}