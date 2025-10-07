using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Implementation of document access logging service.
/// Tracks all document access for security audit and compliance.
/// </summary>
public class DocumentAccessLogService : IDocumentAccessLogService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<DocumentAccessLogService> _logger;

    public DocumentAccessLogService(
        EventForgeDbContext context,
        ILogger<DocumentAccessLogService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> LogAccessAsync(
        Guid documentId,
        string userId,
        string? userName,
        string accessType,
        string? ipAddress,
        string? userAgent,
        string result,
        string? details,
        Guid tenantId,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logEntry = new DocumentAccessLog
            {
                Id = Guid.NewGuid(),
                DocumentHeaderId = documentId,
                UserId = userId,
                UserName = userName,
                AccessType = accessType,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Result = result,
                Details = details,
                TenantId = tenantId,
                SessionId = sessionId
            };

            _ = _context.Set<DocumentAccessLog>().Add(logEntry);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Logged document access: Document={DocumentId}, User={UserId}, Type={AccessType}, Result={Result}",
                documentId, userId, accessType, result);

            return logEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error logging document access for document {DocumentId} by user {UserId}",
                documentId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<DocumentAccessLogDto>> GetDocumentAccessLogsAsync(
        Guid documentId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DocumentAccessLog>()
            .Include(l => l.DocumentHeader)
            .Where(l => l.DocumentHeaderId == documentId);

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt <= toDate.Value);
        }

        var logs = await query
            .OrderByDescending(l => l.AccessedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentAccessLogDto>> GetUserAccessLogsAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DocumentAccessLog>()
            .Include(l => l.DocumentHeader)
            .Where(l => l.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt <= toDate.Value);
        }

        var logs = await query
            .OrderByDescending(l => l.AccessedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToDto);
    }

    public async Task<PagedResult<DocumentAccessLogDto>> GetAccessLogsAsync(
        Guid tenantId,
        Guid? documentId = null,
        string? userId = null,
        string? accessType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DocumentAccessLog>()
            .Include(l => l.DocumentHeader)
            .Where(l => l.TenantId == tenantId);

        if (documentId.HasValue)
        {
            query = query.Where(l => l.DocumentHeaderId == documentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(accessType))
        {
            query = query.Where(l => l.AccessType == accessType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.AccessedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentAccessLogDto>
        {
            Items = logs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> DeleteOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldLogs = await _context.Set<DocumentAccessLog>()
            .Where(l => l.AccessedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldLogs.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation(
            "Deleting {Count} access log entries older than {Date}",
            oldLogs.Count, cutoffDate);

        _context.Set<DocumentAccessLog>().RemoveRange(oldLogs);
        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted {Count} old access log entries",
            oldLogs.Count);

        return oldLogs.Count;
    }

    private DocumentAccessLogDto MapToDto(DocumentAccessLog log)
    {
        return new DocumentAccessLogDto
        {
            Id = log.Id,
            DocumentHeaderId = log.DocumentHeaderId,
            DocumentNumber = log.DocumentHeader?.Number,
            UserId = log.UserId,
            UserName = log.UserName,
            AccessType = log.AccessType,
            AccessedAt = log.AccessedAt,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Result = log.Result,
            Details = log.Details,
            TenantId = log.TenantId,
            SessionId = log.SessionId
        };
    }
}
