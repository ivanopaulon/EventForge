using Prym.DTOs.Common;
using Prym.DTOs.SuperAdmin;

namespace Prym.Web.Services
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

        /// <summary>
        /// Gets sanitized application logs for public (authenticated user) viewing.
        /// Sensitive information is masked for security.
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated sanitized application logs</returns>
        Task<PagedResult<SanitizedSystemLogDto>> GetPublicApplicationLogsAsync(
            ApplicationLogQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates an asynchronous log export job on the server.
        /// Restricted to SuperAdmin role.
        /// </summary>
        /// <param name="exportRequest">Export parameters (type, format, date range, filters)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Export result with status and download URL when ready</returns>
        Task<ExportResultDto> ExportLogsAsync(
            ExportRequestDto exportRequest,
            CancellationToken cancellationToken = default);
    }
}
