namespace EventForge.Server.Services.Logs;

/// <summary>
/// Unified interface for all log management operations.
/// Consolidates application logs, audit logs, and client logs management.
/// </summary>
public interface ILogManagementService
{
    #region Application Logs

    /// <summary>
    /// Gets paginated application logs with filtering and sorting.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of application logs</returns>
    Task<PagedResult<SystemLogDto>> GetApplicationLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific application log entry by ID.
    /// </summary>
    /// <param name="id">The log entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The log entry or null if not found</returns>
    Task<SystemLogDto?> GetApplicationLogByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent error logs (last 24 hours).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recent error logs</returns>
    Task<IEnumerable<SystemLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log statistics for a date range.
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Log statistics grouped by level</returns>
    Task<Dictionary<string, int>> GetLogStatisticsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Client Logs

    /// <summary>
    /// Processes a client log entry and integrates it with the server logging system.
    /// </summary>
    /// <param name="clientLog">Client log to process</param>
    /// <param name="userContext">Current user context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessClientLogAsync(ClientLogDto clientLog, string? userContext = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes multiple client log entries in a batch.
    /// </summary>
    /// <param name="clientLogs">Client logs to process</param>
    /// <param name="userContext">Current user context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch processing result with success/error counts</returns>
    Task<BatchProcessingResult> ProcessClientLogBatchAsync(
        IEnumerable<ClientLogDto> clientLogs,
        string? userContext = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Audit Logs (Integration)

    /// <summary>
    /// Gets paginated audit logs with filtering.
    /// </summary>
    /// <param name="searchDto">Search and filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit logs</returns>
    Task<PagedResult<AuditTrailResponseDto>> GetAuditLogsAsync(
        AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit trail statistics</returns>
    Task<AuditTrailStatisticsDto> GetAuditStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Export and Monitoring

    /// <summary>
    /// Exports logs with the specified parameters.
    /// </summary>
    /// <param name="exportRequest">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result information</returns>
    Task<ExportResultDto> ExportLogsAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current log monitoring configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current monitoring configuration</returns>
    Task<LogMonitoringConfigDto> GetMonitoringConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the log monitoring configuration.
    /// </summary>
    /// <param name="config">Updated monitoring configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated monitoring configuration</returns>
    Task<LogMonitoringConfigDto> UpdateMonitoringConfigurationAsync(
        LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available log levels from the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available log levels</returns>
    Task<IEnumerable<string>> GetAvailableLogLevelsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Performance and Maintenance

    /// <summary>
    /// Clears log cache to ensure fresh data retrieval.
    /// </summary>
    Task ClearCacheAsync();

    /// <summary>
    /// Gets system health status related to logging.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information</returns>
    Task<LogSystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of batch processing operation.
/// </summary>
public class BatchProcessingResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BatchItemResult> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of individual item in batch processing.
/// </summary>
public class BatchItemResult
{
    public int Index { get; set; }
    public string Status { get; set; } = string.Empty; // "success" or "error"
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Log system health status.
/// </summary>
public class LogSystemHealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, object> Details { get; set; } = new();
}