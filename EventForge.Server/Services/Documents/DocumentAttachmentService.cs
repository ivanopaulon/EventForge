using EventForge.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document attachments
/// </summary>
public class DocumentAttachmentService : IDocumentAttachmentService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentAttachmentService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentAttachmentService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="tenantContext">Tenant context service</param>
    /// <param name="logger">Logger instance</param>
    public DocumentAttachmentService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<DocumentAttachmentService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetDocumentHeaderAttachmentsAsync(
        Guid documentHeaderId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentAttachments
                .Where(a => a.DocumentHeaderId == documentHeaderId && !a.IsDeleted);

            if (!includeHistory)
            {
                query = query.Where(a => a.IsCurrentVersion);
            }

            var attachments = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            return attachments.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachments for document header {DocumentHeaderId}", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetDocumentRowAttachmentsAsync(
        Guid documentRowId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentAttachments
                .Where(a => a.DocumentRowId == documentRowId && !a.IsDeleted);

            if (!includeHistory)
            {
                query = query.Where(a => a.IsCurrentVersion);
            }

            var attachments = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            return attachments.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachments for document row {DocumentRowId}", documentRowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> GetAttachmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            return attachment != null ? MapToDto(attachment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachment {AttachmentId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto> CreateAttachmentAsync(
        CreateDocumentAttachmentDto createDto,
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

            var attachment = new DocumentAttachment
            {
                Id = Guid.NewGuid(),
                DocumentHeaderId = createDto.DocumentHeaderId,
                DocumentRowId = createDto.DocumentRowId,
                FileName = createDto.FileName,
                StoragePath = createDto.StoragePath,
                MimeType = createDto.MimeType,
                FileSizeBytes = createDto.FileSizeBytes,
                Title = createDto.Title,
                Notes = createDto.Notes,
                Category = Enum.Parse<DocumentAttachmentCategory>(createDto.Category, true),
                AccessLevel = Enum.Parse<AttachmentAccessLevel>(createDto.AccessLevel, true),
                StorageProvider = createDto.StorageProvider,
                ExternalReference = createDto.ExternalReference,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                TenantId = _tenantContext.CurrentTenantId ?? Guid.Empty
            };

            _context.DocumentAttachments.Add(attachment);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAttachment",
                attachment.Id,
                "CREATE",
                "CREATE",
                null,
                $"Created attachment '{attachment.FileName}' for document",
                currentUser);

            _logger.LogInformation("Created attachment {AttachmentId} for user {User}", attachment.Id, currentUser);

            return MapToDto(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attachment for user {User}", currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> UpdateAttachmentAsync(
        Guid id,
        UpdateDocumentAttachmentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (attachment == null)
                return null;

            var originalValues = new { attachment.Title, attachment.Notes, attachment.Category, attachment.AccessLevel };

            // Update fields
            if (updateDto.Title != null)
                attachment.Title = updateDto.Title;
            if (updateDto.Notes != null)
                attachment.Notes = updateDto.Notes;
            if (!string.IsNullOrEmpty(updateDto.Category))
                attachment.Category = Enum.Parse<DocumentAttachmentCategory>(updateDto.Category, true);
            if (!string.IsNullOrEmpty(updateDto.AccessLevel))
                attachment.AccessLevel = Enum.Parse<AttachmentAccessLevel>(updateDto.AccessLevel, true);

            attachment.ModifiedAt = DateTime.UtcNow;
            attachment.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAttachment",
                attachment.Id,
                "UPDATE",
                "UPDATE",
                null,
                $"Updated attachment '{attachment.FileName}' metadata",
                currentUser);

            _logger.LogInformation("Updated attachment {AttachmentId} for user {User}", attachment.Id, currentUser);

            return MapToDto(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attachment {AttachmentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> CreateAttachmentVersionAsync(
        Guid id,
        AttachmentVersionDto versionDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalAttachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (originalAttachment == null)
                return null;

            // Mark current version as not current
            originalAttachment.IsCurrentVersion = false;

            // Create new version
            var newVersion = new DocumentAttachment
            {
                Id = Guid.NewGuid(),
                DocumentHeaderId = originalAttachment.DocumentHeaderId,
                DocumentRowId = originalAttachment.DocumentRowId,
                FileName = versionDto.FileName,
                StoragePath = versionDto.StoragePath,
                MimeType = versionDto.MimeType,
                FileSizeBytes = versionDto.FileSizeBytes,
                Title = originalAttachment.Title,
                Notes = versionDto.Notes ?? originalAttachment.Notes,
                Category = originalAttachment.Category,
                AccessLevel = originalAttachment.AccessLevel,
                StorageProvider = originalAttachment.StorageProvider,
                ExternalReference = originalAttachment.ExternalReference,
                Version = originalAttachment.Version + 1,
                PreviousVersionId = originalAttachment.Id,
                IsCurrentVersion = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                TenantId = _tenantContext.CurrentTenantId ?? Guid.Empty
            };

            _context.DocumentAttachments.Add(newVersion);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAttachment",
                newVersion.Id,
                "CREATE_VERSION",
                "CREATE_VERSION",
                null,
                $"Created version {newVersion.Version} of attachment '{newVersion.FileName}'",
                currentUser);

            _logger.LogInformation("Created version {Version} of attachment {AttachmentId} for user {User}",
                newVersion.Version, id, currentUser);

            return MapToDto(newVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version of attachment {AttachmentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAttachmentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (attachment == null)
                return false;

            attachment.IsDeleted = true;
            attachment.DeletedAt = DateTime.UtcNow;
            attachment.DeletedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAttachment",
                attachment.Id,
                "DELETE",
                "DELETE",
                null,
                $"Deleted attachment '{attachment.FileName}'",
                currentUser);

            _logger.LogInformation("Deleted attachment {AttachmentId} for user {User}", attachment.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the original attachment or any version to get the base
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (attachment == null)
                return Enumerable.Empty<DocumentAttachmentDto>();

            // Find the root attachment (version 1)
            var rootAttachment = attachment;
            while (rootAttachment.PreviousVersionId.HasValue)
            {
                var previous = await _context.DocumentAttachments
                    .FirstOrDefaultAsync(a => a.Id == rootAttachment.PreviousVersionId.Value && !a.IsDeleted, cancellationToken);
                if (previous != null)
                    rootAttachment = previous;
                else
                    break;
            }

            // Get all versions starting from root
            var versions = await _context.DocumentAttachments
                .Where(a => a.Id == rootAttachment.Id || a.PreviousVersionId == rootAttachment.Id && !a.IsDeleted)
                .OrderBy(a => a.Version)
                .ToListAsync(cancellationToken);

            return versions.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions for attachment {AttachmentId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> SignAttachmentAsync(
        Guid id,
        string signatureInfo,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (attachment == null)
                return null;

            attachment.IsSigned = true;
            attachment.SignatureInfo = signatureInfo;
            attachment.SignedAt = DateTime.UtcNow;
            attachment.SignedBy = currentUser;
            attachment.ModifiedAt = DateTime.UtcNow;
            attachment.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAttachment",
                attachment.Id,
                "SIGN",
                "SIGN",
                null,
                $"Digitally signed attachment '{attachment.FileName}'",
                currentUser);

            _logger.LogInformation("Signed attachment {AttachmentId} for user {User}", attachment.Id, currentUser);

            return MapToDto(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing attachment {AttachmentId} for user {User}", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<DocumentAttachmentCategory>(category, true, out var categoryEnum))
            {
                return Enumerable.Empty<DocumentAttachmentDto>();
            }

            var attachments = await _context.DocumentAttachments
                .Where(a => a.Category == categoryEnum && a.IsCurrentVersion && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            return attachments.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachments by category {Category}", category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> AttachmentExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DocumentAttachments
                .AnyAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if attachment {AttachmentId} exists", id);
            throw;
        }
    }

    /// <summary>
    /// Maps DocumentAttachment entity to DTO
    /// </summary>
    /// <param name="attachment">Document attachment entity</param>
    /// <returns>Document attachment DTO</returns>
    private static DocumentAttachmentDto MapToDto(DocumentAttachment attachment)
    {
        return new DocumentAttachmentDto
        {
            Id = attachment.Id,
            DocumentHeaderId = attachment.DocumentHeaderId,
            DocumentRowId = attachment.DocumentRowId,
            FileName = attachment.FileName,
            MimeType = attachment.MimeType,
            FileSizeBytes = attachment.FileSizeBytes,
            Version = attachment.Version,
            PreviousVersionId = attachment.PreviousVersionId,
            Title = attachment.Title,
            Notes = attachment.Notes,
            IsSigned = attachment.IsSigned,
            SignedAt = attachment.SignedAt,
            SignedBy = attachment.SignedBy,
            IsCurrentVersion = attachment.IsCurrentVersion,
            Category = attachment.Category.ToString(),
            AccessLevel = attachment.AccessLevel.ToString(),
            StorageProvider = attachment.StorageProvider,
            ExternalReference = attachment.ExternalReference,
            CreatedAt = attachment.CreatedAt,
            CreatedBy = attachment.CreatedBy,
            UpdatedAt = attachment.ModifiedAt,
            UpdatedBy = attachment.ModifiedBy
        };
    }
}