using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace EventForge.Server.Services.Audit;

/// <summary>
/// Service implementation for managing audit logs and entity change tracking.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        EventForgeDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates an audit log entry for an entity change.
    /// </summary>
    public async Task<EntityChangeLog> LogEntityChangeAsync(
        string entityName,
        Guid entityId,
        string propertyName,
        string operationType,
        string? oldValue,
        string? newValue,
        string changedBy,
        string? entityDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);

        try
        {
            var changeLog = new EntityChangeLog
            {
                EntityName = entityName,
                EntityDisplayName = entityDisplayName,
                EntityId = entityId,
                PropertyName = propertyName,
                OperationType = operationType,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow
            };

            _ = _context.EntityChangeLogs.Add(changeLog);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: {OperationType} on {EntityName} [{EntityId}] property {PropertyName} by {ChangedBy}",
                operationType, entityName, entityId, propertyName, changedBy);

            return changeLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating audit log for {OperationType} on {EntityName} [{EntityId}] property {PropertyName}",
                operationType, entityName, entityId, propertyName);
            throw;
        }
    }

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> GetEntityLogsAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityChangeLogs
            .Where(log => log.EntityId == entityId)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs for a specific entity type.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> GetEntityTypeLogsAsync(
        string entityName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return await _context.EntityChangeLogs
            .Where(log => log.EntityName == entityName)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs within a date range.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityChangeLogs
            .Where(log => log.ChangedAt >= fromDate && log.ChangedAt <= toDate)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs for changes made by a specific user.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> GetUserLogsAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _context.EntityChangeLogs
            .Where(log => log.ChangedBy == username)
            .OrderByDescending(log => log.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> GetLogsAsync(
        Expression<Func<EntityChangeLog, bool>>? filter = null,
        Expression<Func<EntityChangeLog, object>>? orderBy = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EntityChangeLogs.AsQueryable();

        if (filter != null)
            query = query.Where(filter);

        if (orderBy != null)
            query = query.OrderBy(orderBy);
        else
            query = query.OrderByDescending(log => log.ChangedAt);

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tracks changes for an auditable entity during save operations.
    /// </summary>
    public async Task<IEnumerable<EntityChangeLog>> TrackEntityChangesAsync<TEntity>(
        TEntity entity,
        string operationType,
        string changedBy,
        TEntity? originalValues = null,
        CancellationToken cancellationToken = default)
        where TEntity : AuditableEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);

        var entityType = typeof(TEntity);
        var entityName = entityType.Name;
        var changeLogs = new List<EntityChangeLog>();

        // For Insert and Delete operations, track all properties
        if (operationType is "Insert" or "Delete")
        {
            var properties = GetTrackableProperties(entityType);

            foreach (var property in properties)
            {
                var value = property.GetValue(entity)?.ToString();
                var changeLog = new EntityChangeLog
                {
                    EntityName = entityName,
                    EntityId = entity.Id,
                    PropertyName = property.Name,
                    OperationType = operationType,
                    OldValue = operationType == "Delete" ? value : null,
                    NewValue = operationType == "Insert" ? value : null,
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.UtcNow
                };

                changeLogs.Add(changeLog);
            }
        }
        // For Update operations, compare with original values
        else if (operationType == "Update" && originalValues != null)
        {
            var properties = GetTrackableProperties(entityType);

            foreach (var property in properties)
            {
                var oldValue = property.GetValue(originalValues)?.ToString();
                var newValue = property.GetValue(entity)?.ToString();

                // Only log if values are different
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    var changeLog = new EntityChangeLog
                    {
                        EntityName = entityName,
                        EntityId = entity.Id,
                        PropertyName = property.Name,
                        OperationType = operationType,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangedBy = changedBy,
                        ChangedAt = DateTime.UtcNow
                    };

                    changeLogs.Add(changeLog);
                }
            }
        }

        if (changeLogs.Count > 0)
        {
            _context.EntityChangeLogs.AddRange(changeLogs);
            _ = await _context.SaveChangesAsync(cancellationToken);
        }

        return changeLogs;
    }

    /// <summary>
    /// Gets paginated audit logs with filtering and sorting.
    /// </summary>
    public async Task<PagedResult<EntityChangeLog>> GetPagedLogsAsync(
        AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var query = _context.EntityChangeLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryParameters.EntityName))
        {
            query = query.Where(log => log.EntityName.Contains(queryParameters.EntityName));
        }

        if (queryParameters.EntityId.HasValue)
        {
            query = query.Where(log => log.EntityId == queryParameters.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.ChangedBy))
        {
            query = query.Where(log => log.ChangedBy.Contains(queryParameters.ChangedBy));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.OperationType))
        {
            query = query.Where(log => log.OperationType == queryParameters.OperationType);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.PropertyName))
        {
            query = query.Where(log => log.PropertyName.Contains(queryParameters.PropertyName));
        }

        if (queryParameters.FromDate.HasValue)
        {
            query = query.Where(log => log.ChangedAt >= queryParameters.FromDate.Value);
        }

        if (queryParameters.ToDate.HasValue)
        {
            query = query.Where(log => log.ChangedAt <= queryParameters.ToDate.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        // Apply pagination
        var items = await query
            .Skip(queryParameters.Skip)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EntityChangeLog>
        {
            Items = items,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gets a single audit log by ID.
    /// </summary>
    public async Task<EntityChangeLog?> GetLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityChangeLogs
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    /// <summary>
    /// Applies sorting to the query based on sort field and direction.
    /// </summary>
    private static IQueryable<EntityChangeLog> ApplySorting(
        IQueryable<EntityChangeLog> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "entityname" => isDescending
                ? query.OrderByDescending(x => x.EntityName)
                : query.OrderBy(x => x.EntityName),
            "entityid" => isDescending
                ? query.OrderByDescending(x => x.EntityId)
                : query.OrderBy(x => x.EntityId),
            "propertyname" => isDescending
                ? query.OrderByDescending(x => x.PropertyName)
                : query.OrderBy(x => x.PropertyName),
            "operationtype" => isDescending
                ? query.OrderByDescending(x => x.OperationType)
                : query.OrderBy(x => x.OperationType),
            "changedby" => isDescending
                ? query.OrderByDescending(x => x.ChangedBy)
                : query.OrderBy(x => x.ChangedBy),
            "changedat" or _ => isDescending
                ? query.OrderByDescending(x => x.ChangedAt)
                : query.OrderBy(x => x.ChangedAt)
        };
    }

    /// <summary>
    /// Gets trackable properties for an entity type (excludes audit fields to avoid recursion).
    /// </summary>
    private static PropertyInfo[] GetTrackableProperties(Type entityType)
    {
        var auditProperties = new[]
        {
            nameof(AuditableEntity.CreatedAt),
            nameof(AuditableEntity.CreatedBy),
            nameof(AuditableEntity.ModifiedAt),
            nameof(AuditableEntity.ModifiedBy),
            nameof(AuditableEntity.DeletedAt),
            nameof(AuditableEntity.DeletedBy),
            nameof(AuditableEntity.RowVersion)
        };

        return entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => (p.CanRead &&
                       !auditProperties.Contains(p.Name) &&
                       !p.PropertyType.IsSubclassOf(typeof(AuditableEntity)) &&
                       !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)) ||
                       p.PropertyType == typeof(string))
            .ToArray();
    }

    /// <summary>
    /// Searches audit trail with advanced filtering.
    /// </summary>
    public async Task<PagedResult<AuditTrailResponseDto>> SearchAuditTrailAsync(
        AuditTrailSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate async operation

        // In a real implementation, this would search through audit logs with the specified criteria
        var results = new PagedResult<AuditTrailResponseDto>
        {
            Items = new List<AuditTrailResponseDto>(),
            TotalCount = 0,
            Page = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };

        return results;
    }

    /// <summary>
    /// Gets audit trail statistics.
    /// </summary>
    public async Task<AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate async operation

        // In a real implementation, this would calculate statistics from audit logs
        var statistics = new AuditTrailStatisticsDto
        {
            TotalOperations = 0,
            SuccessfulOperations = 0,
            FailedOperations = 0,
            CriticalOperations = 0,
            OperationsToday = 0,
            OperationsThisWeek = 0,
            OperationsThisMonth = 0,
            OperationsByType = new Dictionary<string, int>(),
            OperationsByUser = new Dictionary<string, int>(),
            OperationsByTenant = new Dictionary<string, int>(),
            RecentTrends = new List<AuditTrendDto>(),
            LastUpdated = DateTime.UtcNow
        };

        return statistics;
    }

    /// <summary>
    /// Exports audit data in the specified format.
    /// </summary>
    public async Task<ExportResultDto> ExportAdvancedAsync(
        ExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportRequest);

        // Validate export request
        if (!new[] { "JSON", "CSV", "EXCEL" }.Contains(exportRequest.Format.ToUpper()))
        {
            throw new ArgumentException("Invalid format. Supported formats: JSON, CSV, EXCEL");
        }

        if (!new[] { "audit", "systemlogs", "users", "tenants" }.Contains(exportRequest.Type.ToLower()))
        {
            throw new ArgumentException("Invalid type. Supported types: audit, systemlogs, users, tenants");
        }

        await Task.Delay(100, cancellationToken); // Simulate async processing

        // In a real implementation, this would queue the export job for background processing
        var exportResult = new ExportResultDto
        {
            Id = Guid.NewGuid(),
            Type = exportRequest.Type,
            Format = exportRequest.Format,
            Status = "Processing",
            RequestedAt = DateTime.UtcNow,
            RequestedBy = "Current User" // Should get from current user context
        };

        return exportResult;
    }

    /// <summary>
    /// Gets the status of an export operation.
    /// </summary>
    public async Task<ExportResultDto?> GetExportStatusAsync(
        Guid exportId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation

        // In a real implementation, this would check the status of the export job from storage
        var exportResult = new ExportResultDto
        {
            Id = exportId,
            Type = "audit",
            Format = "JSON",
            Status = "Completed",
            TotalRecords = 150,
            FileName = $"audit_export_{exportId:N}.json",
            DownloadUrl = $"/api/v1/auditlog/export/{exportId}/download",
            FileSizeBytes = 1024 * 50, // 50KB
            RequestedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-2),
            RequestedBy = "Current User",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        return exportResult;
    }

    /// <summary>
    /// Gets all audit logs with pagination.
    /// </summary>
    public async Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = _context.EntityChangeLogs.AsQueryable();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(log => log.ChangedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(log => new EntityChangeLogDto
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityDisplayName = log.EntityDisplayName,
                EntityId = log.EntityId,
                PropertyName = log.PropertyName,
                OperationType = log.OperationType,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            })
            .ToListAsync(ct);

        return new PagedResult<EntityChangeLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets audit logs for a specific entity type with pagination.
    /// </summary>
    public async Task<PagedResult<EntityChangeLogDto>> GetLogsByEntityAsync(
        string entityType,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = _context.EntityChangeLogs
            .Where(log => log.EntityName == entityType);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(log => log.ChangedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(log => new EntityChangeLogDto
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityDisplayName = log.EntityDisplayName,
                EntityId = log.EntityId,
                PropertyName = log.PropertyName,
                OperationType = log.OperationType,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            })
            .ToListAsync(ct);

        return new PagedResult<EntityChangeLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets audit logs for a specific user with pagination.
    /// </summary>
    public async Task<PagedResult<EntityChangeLogDto>> GetLogsByUserAsync(
        Guid userId,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        // Note: EntityChangeLog stores ChangedBy as string (username), not Guid
        // We need to join with Users table to filter by userId
        var query = from log in _context.EntityChangeLogs
                    join user in _context.Users on log.ChangedBy equals user.Username
                    where user.Id == userId
                    select log;

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(log => log.ChangedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(log => new EntityChangeLogDto
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityDisplayName = log.EntityDisplayName,
                EntityId = log.EntityId,
                PropertyName = log.PropertyName,
                OperationType = log.OperationType,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            })
            .ToListAsync(ct);

        return new PagedResult<EntityChangeLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets audit logs within a date range with pagination.
    /// </summary>
    public async Task<PagedResult<EntityChangeLogDto>> GetLogsByDateRangeAsync(
        DateTime startDate,
        DateTime? endDate,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var end = endDate ?? DateTime.UtcNow;

        var query = _context.EntityChangeLogs
            .Where(log => log.ChangedAt >= startDate && log.ChangedAt <= end);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(log => log.ChangedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(log => new EntityChangeLogDto
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityDisplayName = log.EntityDisplayName,
                EntityId = log.EntityId,
                PropertyName = log.PropertyName,
                OperationType = log.OperationType,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            })
            .ToListAsync(ct);

        return new PagedResult<EntityChangeLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}