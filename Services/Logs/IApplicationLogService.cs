namespace EventForge.Services.Logs;

/// <summary>
/// Service interface for reading application logs from Serilog database.
/// </summary>
public interface IApplicationLogService
{
    /// <summary>
    /// Gets paginated application logs with filtering and sorting.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated application logs with total count</returns>
    Task<PagedResult<ApplicationLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single application log by ID.
    /// </summary>
    /// <param name="id">The log entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The log entry or null if not found</returns>
    Task<ApplicationLogDto?> GetLogByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets application logs within a date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs within the date range</returns>
    Task<IEnumerable<ApplicationLogDto>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets application logs by log level.
    /// </summary>
    /// <param name="level">The log level to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of application logs for the specified level</returns>
    Task<IEnumerable<ApplicationLogDto>> GetLogsByLevelAsync(
        string level,
        CancellationToken cancellationToken = default);
}