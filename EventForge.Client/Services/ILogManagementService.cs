using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Client-side service for log management operations.
    /// Provides access to application logs with filtering, sorting, and pagination.
    /// </summary>
    public interface ILogManagementService
    {
        /// <summary>
        /// Gets paginated application logs with optional filtering and sorting.
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated application logs</returns>
        Task<PagedResult<SystemLogDto>> GetApplicationLogsAsync(
            ApplicationLogQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific application log entry by ID.
        /// </summary>
        /// <param name="id">The log entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The log entry or null if not found</returns>
        Task<SystemLogDto?> GetApplicationLogByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available log levels from the system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available log levels</returns>
        Task<IEnumerable<string>> GetAvailableLogLevelsAsync(
            CancellationToken cancellationToken = default);
    }
}
