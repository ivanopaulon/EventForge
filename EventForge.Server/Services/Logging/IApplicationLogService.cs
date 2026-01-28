using EventForge.DTOs.Common;
using EventForge.DTOs.Logging;

namespace EventForge.Server.Services.Logging;

/// <summary>
/// Service interface for managing application logs.
/// </summary>
public interface IApplicationLogService
{
    /// <summary>
    /// Gets all application logs with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated application logs</returns>
    Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetApplicationLogsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets application logs for a specific log level with pagination.
    /// </summary>
    /// <param name="level">Log level (e.g., "Information", "Warning", "Error")</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated application logs for the log level</returns>
    Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetLogsByLevelAsync(
        string level,
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets application logs within a date range with pagination.
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date (optional, defaults to now)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated application logs within the date range</returns>
    Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetLogsByDateRangeAsync(
        DateTime startDate,
        DateTime? endDate,
        PaginationParameters pagination,
        CancellationToken ct = default);
}
