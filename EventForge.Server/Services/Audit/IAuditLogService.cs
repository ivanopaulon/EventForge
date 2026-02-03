using System.Linq.Expressions;

namespace EventForge.Server.Services.Audit;

/// <summary>
/// Service interface for managing audit logs and entity change tracking.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Creates an audit log entry for an entity change.
    /// </summary>
    /// <param name="entityName">The name of the entity that was changed</param>
    /// <param name="entityId">The ID of the changed entity</param>
    /// <param name="propertyName">The name of the changed property</param>
    /// <param name="operationType">The type of operation (Insert, Update, Delete)</param>
    /// <param name="oldValue">The previous value</param>
    /// <param name="newValue">The new value</param>
    /// <param name="changedBy">The user who made the change</param>
    /// <param name="entityDisplayName">Optional display name for the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created EntityChangeLog entry</returns>
    Task<EntityChangeLog> LogEntityChangeAsync(
        string entityName,
        Guid entityId,
        string propertyName,
        string operationType,
        string? oldValue,
        string? newValue,
        string changedBy,
        string? entityDisplayName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID to get logs for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the entity</returns>
    Task<IEnumerable<EntityChangeLog>> GetEntityLogsAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity type.
    /// </summary>
    /// <param name="entityName">The entity type name to get logs for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the entity type</returns>
    Task<IEnumerable<EntityChangeLog>> GetEntityTypeLogsAsync(
        string entityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs within the date range</returns>
    Task<IEnumerable<EntityChangeLog>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for changes made by a specific user.
    /// </summary>
    /// <param name="username">The username to get logs for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the user</returns>
    Task<IEnumerable<EntityChangeLog>> GetUserLogsAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="orderBy">Optional ordering expression</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered and paginated audit logs</returns>
    Task<IEnumerable<EntityChangeLog>> GetLogsAsync(
        Expression<Func<EntityChangeLog, bool>>? filter = null,
        Expression<Func<EntityChangeLog, object>>? orderBy = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks changes for an auditable entity during save operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that inherits from AuditableEntity</typeparam>
    /// <param name="entity">The entity being tracked</param>
    /// <param name="operationType">The operation type (Insert, Update, Delete)</param>
    /// <param name="changedBy">The user making the change</param>
    /// <param name="originalValues">Original values for comparison during updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of created audit log entries</returns>
    Task<IEnumerable<EntityChangeLog>> TrackEntityChangesAsync<TEntity>(
        TEntity entity,
        string operationType,
        string changedBy,
        TEntity? originalValues = null,
        CancellationToken cancellationToken = default)
        where TEntity : AuditableEntity;

    /// <summary>
    /// Gets paginated audit logs with filtering and sorting.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit logs with total count</returns>
    Task<PagedResult<EntityChangeLog>> GetPagedLogsAsync(
        AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single audit log by ID.
    /// </summary>
    /// <param name="id">The audit log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audit log entry or null if not found</returns>
    Task<EntityChangeLog?> GetLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit trail with advanced filtering.
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit trail results</returns>
    Task<PagedResult<DTOs.Audit.AuditTrailResponseDto>> SearchAuditTrailAsync(
        DTOs.Audit.AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit trail statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit trail statistics</returns>
    Task<DTOs.Audit.AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit data in the specified format.
    /// </summary>
    /// <param name="exportRequest">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result information</returns>
    Task<ExportResultDto> ExportAdvancedAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of an export operation.
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export status information</returns>
    Task<ExportResultDto?> GetExportStatusAsync(
        Guid exportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit logs</returns>
    Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets audit logs for a specific entity type with pagination.
    /// </summary>
    /// <param name="entityType">Entity type name</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit logs for the entity type</returns>
    Task<PagedResult<EntityChangeLogDto>> GetLogsByEntityAsync(
        string entityType,
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets audit logs for a specific user with pagination.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit logs for the user</returns>
    Task<PagedResult<EntityChangeLogDto>> GetLogsByUserAsync(
        Guid userId,
        PaginationParameters pagination,
        CancellationToken ct = default);

    /// <summary>
    /// Gets audit logs within a date range with pagination.
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date (optional, defaults to now)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit logs within the date range</returns>
    Task<PagedResult<EntityChangeLogDto>> GetLogsByDateRangeAsync(
        DateTime startDate,
        DateTime? endDate,
        PaginationParameters pagination,
        CancellationToken ct = default);
}