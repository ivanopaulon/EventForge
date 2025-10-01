using EventForge.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for document access logging.
/// Tracks all document access for security audit and compliance.
/// </summary>
public interface IDocumentAccessLogService
{
    /// <summary>
    /// Logs a document access event.
    /// </summary>
    /// <param name="documentId">Document that was accessed</param>
    /// <param name="userId">User who accessed the document</param>
    /// <param name="userName">User's display name</param>
    /// <param name="accessType">Type of access (View, Download, Edit, etc.)</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="result">Result of the access attempt</param>
    /// <param name="details">Additional details</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="sessionId">Session ID if available</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created log entry ID</returns>
    Task<Guid> LogAccessAsync(
        Guid documentId,
        string userId,
        string? userName,
        string accessType,
        string? ipAddress,
        string? userAgent,
        string result,
        string? details,
        Guid tenantId,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets access logs for a specific document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of access log entries</returns>
    Task<IEnumerable<DocumentAccessLogDto>> GetDocumentAccessLogsAsync(
        Guid documentId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets access logs for a specific user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of access log entries</returns>
    Task<IEnumerable<DocumentAccessLogDto>> GetUserAccessLogsAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets access logs with filtering and pagination.
    /// </summary>
    /// <param name="tenantId">Tenant ID filter</param>
    /// <param name="documentId">Optional document ID filter</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="accessType">Optional access type filter</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of access log entries</returns>
    Task<PagedResult<DocumentAccessLogDto>> GetAccessLogsAsync(
        Guid tenantId,
        Guid? documentId = null,
        string? userId = null,
        string? accessType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old access logs based on retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain logs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs deleted</returns>
    Task<int> DeleteOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for document access log entries.
/// </summary>
public class DocumentAccessLogDto
{
    public Guid Id { get; set; }
    public Guid DocumentHeaderId { get; set; }
    public string? DocumentNumber { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Guid TenantId { get; set; }
    public string? SessionId { get; set; }
}
