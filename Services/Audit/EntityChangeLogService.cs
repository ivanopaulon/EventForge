using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service for managing entity change log operations.
/// </summary>
public class EntityChangeLogService : IEntityChangeLogService
{
    private readonly EventForgeDbContext _context;

    public EntityChangeLogService(EventForgeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<EntityChangeLog>> GetEntityHistoryAsync(
        string entityName,
        Guid entityId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        string? changedBy = null,
        string? propertyName = null)
    {
        var query = _context.EntityChangeLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId);

        if (fromDate.HasValue)
            query = query.Where(x => x.ChangedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.ChangedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(x => x.OperationType == operationType);

        if (!string.IsNullOrEmpty(changedBy))
            query = query.Where(x => x.ChangedBy == changedBy);

        if (!string.IsNullOrEmpty(propertyName))
            query = query.Where(x => x.PropertyName == propertyName);

        return await query
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EntityChangeLog> AddChangeLogAsync(
        string entityName,
        Guid entityId,
        string operationType,
        string propertyName,
        string? oldValue,
        string? newValue,
        string changedBy,
        string? entityDisplayName = null)
    {
        var changeLog = new EntityChangeLog
        {
            EntityName = entityName,
            EntityId = entityId,
            OperationType = operationType,
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            EntityDisplayName = entityDisplayName,
            ChangedAt = DateTime.UtcNow
        };

        _context.EntityChangeLogs.Add(changeLog);
        await _context.SaveChangesAsync();

        return changeLog;
    }

    /// <inheritdoc />
    public async Task<int> DeleteEntityHistoryAsync(string entityName, Guid entityId)
    {
        var logs = await _context.EntityChangeLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .ToListAsync();

        _context.EntityChangeLogs.RemoveRange(logs);
        await _context.SaveChangesAsync();

        return logs.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllHistoryAsync()
    {
        var allLogs = await _context.EntityChangeLogs.ToListAsync();
        _context.EntityChangeLogs.RemoveRange(allLogs);
        await _context.SaveChangesAsync();

        return allLogs.Count;
    }

    /// <inheritdoc />
    public async Task<EntityChangeLog?> GetLastChangeAsync(string entityName, Guid entityId)
    {
        return await _context.EntityChangeLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.ChangedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<List<EntityChangeLog>> GetEntityTypeHistoryAsync(
        string entityName,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        string? changedBy = null)
    {
        var query = _context.EntityChangeLogs
            .Where(x => x.EntityName == entityName);

        if (fromDate.HasValue)
            query = query.Where(x => x.ChangedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.ChangedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(x => x.OperationType == operationType);

        if (!string.IsNullOrEmpty(changedBy))
            query = query.Where(x => x.ChangedBy == changedBy);

        return await query
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EntityChangeLog>> GetUserChangesAsync(
        string changedBy,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? entityName = null,
        string? operationType = null)
    {
        var query = _context.EntityChangeLogs
            .Where(x => x.ChangedBy == changedBy);

        if (fromDate.HasValue)
            query = query.Where(x => x.ChangedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.ChangedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(x => x.EntityName == entityName);

        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(x => x.OperationType == operationType);

        return await query
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EntityChangeLog>> GetPropertyChangesAsync(
        string entityName,
        Guid entityId,
        string propertyName,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? changedBy = null)
    {
        var query = _context.EntityChangeLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId && x.PropertyName == propertyName);

        if (fromDate.HasValue)
            query = query.Where(x => x.ChangedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.ChangedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(changedBy))
            query = query.Where(x => x.ChangedBy == changedBy);

        return await query
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }
}