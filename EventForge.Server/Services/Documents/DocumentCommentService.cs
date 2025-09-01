using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document comments and collaboration
/// </summary>
public class DocumentCommentService : IDocumentCommentService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentCommentService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentCommentService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="tenantContext">Tenant context service</param>
    /// <param name="logger">Logger instance</param>
    public DocumentCommentService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<DocumentCommentService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetDocumentHeaderCommentsAsync(
        Guid documentHeaderId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentComments
                .Where(c => c.DocumentHeaderId == documentHeaderId && !c.IsDeleted && c.ParentCommentId == null);

            if (includeReplies)
            {
                query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
            }

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return comments.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for document header {DocumentHeaderId}", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetDocumentRowCommentsAsync(
        Guid documentRowId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentComments
                .Where(c => c.DocumentRowId == documentRowId && !c.IsDeleted && c.ParentCommentId == null);

            if (includeReplies)
            {
                query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
            }

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return comments.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for document row {DocumentRowId}", documentRowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> GetCommentByIdAsync(
        Guid id,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentComments.Where(c => c.Id == id && !c.IsDeleted);

            if (includeReplies)
            {
                query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
            }

            var comment = await query.FirstOrDefaultAsync(cancellationToken);
            return comment != null ? MapToDto(comment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comment {CommentId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto> CreateCommentAsync(
        CreateDocumentCommentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate that either document header or row is specified
            if (!createDto.DocumentHeaderId.HasValue && !createDto.DocumentRowId.HasValue)
            {
                throw new ArgumentException("Either DocumentHeaderId or DocumentRowId must be specified.");
            }

            var comment = new DocumentComment
            {
                Id = Guid.NewGuid(),
                DocumentHeaderId = createDto.DocumentHeaderId,
                DocumentRowId = createDto.DocumentRowId,
                Content = createDto.Content,
                CommentType = Enum.Parse<DocumentCommentType>(createDto.CommentType, true),
                Priority = Enum.Parse<CommentPriority>(createDto.Priority, true),
                ParentCommentId = createDto.ParentCommentId,
                AssignedTo = createDto.AssignedTo,
                DueDate = createDto.DueDate,
                MentionedUsers = createDto.MentionedUsers,
                IsPrivate = createDto.IsPrivate,
                IsPinned = createDto.IsPinned,
                Visibility = Enum.Parse<CommentVisibility>(createDto.Visibility, true),
                Tags = createDto.Tags,
                Metadata = createDto.Metadata,
                Status = CommentStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                TenantId = _tenantContext.CurrentTenantId ?? Guid.Empty
            };

            _context.DocumentComments.Add(comment);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentComment",
                comment.Id,
                "CREATE",
                "CREATE",
                null,
                $"Created comment on document",
                currentUser);

            _logger.LogInformation("Created comment {CommentId} for user {User}", comment.Id, currentUser);

            return MapToDto(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for user {User}", currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> UpdateCommentAsync(
        Guid id,
        UpdateDocumentCommentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _context.DocumentComments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (comment == null)
                return null;

            // Update fields
            if (!string.IsNullOrEmpty(updateDto.Content))
                comment.Content = updateDto.Content;
            if (!string.IsNullOrEmpty(updateDto.Priority))
                comment.Priority = Enum.Parse<CommentPriority>(updateDto.Priority, true);
            if (!string.IsNullOrEmpty(updateDto.Status))
                comment.Status = Enum.Parse<CommentStatus>(updateDto.Status, true);
            if (updateDto.AssignedTo != null)
                comment.AssignedTo = updateDto.AssignedTo;
            if (updateDto.DueDate.HasValue)
                comment.DueDate = updateDto.DueDate;
            if (updateDto.IsPinned.HasValue)
                comment.IsPinned = updateDto.IsPinned.Value;
            if (!string.IsNullOrEmpty(updateDto.Visibility))
                comment.Visibility = Enum.Parse<CommentVisibility>(updateDto.Visibility, true);
            if (updateDto.Tags != null)
                comment.Tags = updateDto.Tags;
            if (updateDto.Metadata != null)
                comment.Metadata = updateDto.Metadata;

            comment.ModifiedAt = DateTime.UtcNow;
            comment.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentComment",
                comment.Id,
                "UPDATE",
                "UPDATE",
                null,
                $"Updated comment",
                currentUser);

            _logger.LogInformation("Updated comment {CommentId} for user {User}", comment.Id, currentUser);

            return MapToDto(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCommentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _context.DocumentComments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (comment == null)
                return false;

            comment.IsDeleted = true;
            comment.DeletedAt = DateTime.UtcNow;
            comment.DeletedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentComment",
                comment.Id,
                "DELETE",
                "DELETE",
                null,
                $"Deleted comment",
                currentUser);

            _logger.LogInformation("Deleted comment {CommentId} for user {User}", comment.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ResolveCommentAsync(
        Guid id,
        ResolveCommentDto resolveDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _context.DocumentComments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (comment == null)
                return null;

            comment.Status = CommentStatus.Resolved;
            comment.ResolvedAt = DateTime.UtcNow;
            comment.ResolvedBy = currentUser;
            comment.ModifiedAt = DateTime.UtcNow;
            comment.ModifiedBy = currentUser;

            if (!string.IsNullOrEmpty(resolveDto.ResolutionNotes))
            {
                comment.Metadata = string.IsNullOrEmpty(comment.Metadata) 
                    ? $"{{\"resolutionNotes\":\"{resolveDto.ResolutionNotes}\"}}"
                    : comment.Metadata;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentComment",
                comment.Id,
                "RESOLVE",
                "RESOLVE",
                null,
                $"Resolved comment",
                currentUser);

            _logger.LogInformation("Resolved comment {CommentId} for user {User}", comment.Id, currentUser);

            return MapToDto(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving comment {CommentId} for user {User}", id, currentUser);
            throw;
        }
    }

    // Simplified implementations for other methods to keep this manageable

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ReopenCommentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.DocumentComments
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (comment == null) return null;

        comment.Status = CommentStatus.Open;
        comment.ResolvedAt = null;
        comment.ResolvedBy = null;
        comment.ModifiedAt = DateTime.UtcNow;
        comment.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(comment);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetAssignedCommentsAsync(
        string username,
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentComments
            .Where(c => c.AssignedTo == username && !c.IsDeleted);

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<CommentStatus>(statusFilter, out var status))
        {
            query = query.Where(c => c.Status == status);
        }

        var comments = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
        return comments.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentsByPriorityAsync(
        string priority,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CommentPriority>(priority, out var priorityEnum))
            return Enumerable.Empty<DocumentCommentDto>();

        var comments = await _context.DocumentComments
            .Where(c => c.Priority == priorityEnum && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentsByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CommentStatus>(status, out var statusEnum))
            return Enumerable.Empty<DocumentCommentDto>();

        var comments = await _context.DocumentComments
            .Where(c => c.Status == statusEnum && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentsByTypeAsync(
        string commentType,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<DocumentCommentType>(commentType, out var typeEnum))
            return Enumerable.Empty<DocumentCommentDto>();

        var comments = await _context.DocumentComments
            .Where(c => c.CommentType == typeEnum && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentStatsDto> GetDocumentCommentStatsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var comments = await _context.DocumentComments
            .Where(c => c.DocumentHeaderId == documentHeaderId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        return new DocumentCommentStatsDto
        {
            TotalComments = comments.Count,
            OpenComments = comments.Count(c => c.Status == CommentStatus.Open),
            ResolvedComments = comments.Count(c => c.Status == CommentStatus.Resolved),
            TaskComments = comments.Count(c => c.CommentType == DocumentCommentType.Task),
            HighPriorityComments = comments.Count(c => c.Priority == CommentPriority.High || c.Priority == CommentPriority.Critical),
            AssignedToMe = comments.Count(c => c.AssignedTo == currentUser)
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentRepliesAsync(
        Guid parentCommentId,
        CancellationToken cancellationToken = default)
    {
        var replies = await _context.DocumentComments
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return replies.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> AssignCommentAsync(
        Guid id,
        string assignToUsername,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.DocumentComments
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (comment == null) return null;

        comment.AssignedTo = assignToUsername;
        comment.ModifiedAt = DateTime.UtcNow;
        comment.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(comment);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> SearchCommentsAsync(
        string searchText,
        Guid? documentHeaderId = null,
        string? status = null,
        string? priority = null,
        string? assignedTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentComments.Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(searchText))
            query = query.Where(c => c.Content.Contains(searchText));

        if (documentHeaderId.HasValue)
            query = query.Where(c => c.DocumentHeaderId == documentHeaderId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CommentStatus>(status, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<CommentPriority>(priority, out var priorityEnum))
            query = query.Where(c => c.Priority == priorityEnum);

        if (!string.IsNullOrEmpty(assignedTo))
            query = query.Where(c => c.AssignedTo == assignedTo);

        var comments = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
        return comments.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<bool> CommentExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DocumentComments
                .AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if comment {CommentId} exists", id);
            throw;
        }
    }

    /// <summary>
    /// Maps DocumentComment entity to DTO
    /// </summary>
    /// <param name="comment">Document comment entity</param>
    /// <returns>Document comment DTO</returns>
    private static DocumentCommentDto MapToDto(DocumentComment comment)
    {
        return new DocumentCommentDto
        {
            Id = comment.Id,
            DocumentHeaderId = comment.DocumentHeaderId,
            DocumentRowId = comment.DocumentRowId,
            Content = comment.Content,
            CommentType = comment.CommentType.ToString(),
            Priority = comment.Priority.ToString(),
            Status = comment.Status.ToString(),
            ParentCommentId = comment.ParentCommentId,
            AssignedTo = comment.AssignedTo,
            DueDate = comment.DueDate,
            ResolvedAt = comment.ResolvedAt,
            ResolvedBy = comment.ResolvedBy,
            MentionedUsers = comment.MentionedUsers,
            IsPrivate = comment.IsPrivate,
            IsPinned = comment.IsPinned,
            Visibility = comment.Visibility.ToString(),
            Tags = comment.Tags,
            Metadata = comment.Metadata,
            CreatedAt = comment.CreatedAt,
            CreatedBy = comment.CreatedBy ?? string.Empty,
            ModifiedAt = comment.ModifiedAt,
            ModifiedBy = comment.ModifiedBy,
            Replies = comment.Replies?.Where(r => !r.IsDeleted).Select(MapToDto).ToList() ?? new List<DocumentCommentDto>()
        };
    }
}