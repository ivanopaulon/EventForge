using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace EventForge.Services.Audit;

/// <summary>
/// Service implementation for managing audit logs and entity change tracking.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly EventForgeDbContext _context;

    public AuditLogService(EventForgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

        _context.EntityChangeLogs.Add(changeLog);
        await _context.SaveChangesAsync(cancellationToken);

        return changeLog;
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
            await _context.SaveChangesAsync(cancellationToken);
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
            .Where(p => p.CanRead &&
                       !auditProperties.Contains(p.Name) &&
                       !p.PropertyType.IsSubclassOf(typeof(AuditableEntity)) &&
                       !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) ||
                       p.PropertyType == typeof(string))
            .ToArray();
    }
}