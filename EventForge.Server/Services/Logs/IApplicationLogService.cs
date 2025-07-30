using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Services.Logs;

/// <summary>
/// Interface for application log service operations.
/// Provides read-only access to application logs for monitoring and debugging.
/// </summary>
public interface IApplicationLogService
{
    /// <summary>
    /// Gets a paginated list of application logs with optional filtering and sorting.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of application logs</returns>
    Task<PagedResult<SystemLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific application log entry by ID.
    /// </summary>
    /// <param name="id">The log entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The log entry or null if not found</returns>
    Task<SystemLogDto?> GetLogByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets application logs filtered by log level.
    /// </summary>
    /// <param name="level">The log level to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs for the specified level</returns>
    Task<IEnumerable<SystemLogDto>> GetLogsByLevelAsync(
        string level,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets application logs within a specific date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs within the date range</returns>
    Task<IEnumerable<SystemLogDto>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log statistics grouped by level for a specific date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with log level as key and count as value</returns>
    Task<Dictionary<string, int>> GetLogStatisticsByLevelAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent error logs (last 24 hours).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recent error logs</returns>
    Task<IEnumerable<SystemLogDto>> GetRecentErrorLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a specific correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of logs with the specified correlation ID</returns>
    Task<IEnumerable<SystemLogDto>> GetLogsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports system logs with the specified parameters.
    /// </summary>
    /// <param name="exportRequest">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result information</returns>
    Task<ExportResultDto> ExportSystemLogsAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current monitoring configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current monitoring configuration</returns>
    Task<LogMonitoringConfigDto> GetMonitoringConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the monitoring configuration.
    /// </summary>
    /// <param name="config">Updated monitoring configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated monitoring configuration</returns>
    Task<LogMonitoringConfigDto> UpdateMonitoringConfigAsync(
        LogMonitoringConfigDto config,
        CancellationToken cancellationToken = default);
}