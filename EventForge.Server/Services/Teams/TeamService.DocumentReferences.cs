using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<IEnumerable<DocumentReferenceDto>> GetDocumentsByOwnerAsync(Guid ownerId, string ownerType, CancellationToken cancellationToken = default)
    {
        var documents = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.OwnerId == ownerId && d.OwnerType == ownerType && !d.IsDeleted)
            .OrderBy(d => d.Type)
            .ThenBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(MapToDocumentReferenceDto);
    }

    public async Task<DocumentReferenceDto?> GetDocumentReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var document = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.Id == id && d.TenantId == currentTenantId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return document is not null ? MapToDocumentReferenceDto(document) : null;
    }

    public async Task<DocumentReferenceDto> CreateDocumentReferenceAsync(CreateDocumentReferenceDto createDocumentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDocumentDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var document = new DocumentReference
        {
            OwnerId = createDocumentDto.OwnerId,
            OwnerType = createDocumentDto.OwnerType,
            FileName = createDocumentDto.FileName,
            Type = createDocumentDto.Type,
            SubType = createDocumentDto.SubType,
            MimeType = createDocumentDto.MimeType,
            StorageKey = createDocumentDto.StorageKey,
            Url = createDocumentDto.Url,
            ThumbnailStorageKey = createDocumentDto.ThumbnailStorageKey,
            Expiry = createDocumentDto.Expiry,
            FileSizeBytes = createDocumentDto.FileSizeBytes,
            Title = createDocumentDto.Title,
            Notes = createDocumentDto.Notes,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
        };

        _ = context.DocumentReferences.Add(document);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(document, "Insert", currentUser, null, cancellationToken);

        return MapToDocumentReferenceDto(document);
    }

    public async Task<DocumentReferenceDto?> UpdateDocumentReferenceAsync(Guid id, UpdateDocumentReferenceDto updateDocumentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDocumentDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var document = await context.DocumentReferences
            .Where(d => d.Id == id && d.TenantId == currentTenantId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Document reference {DocumentId} not found for update", id);
            return null;
        }

        var originalDocument = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.Id == id && d.TenantId == currentTenantId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        document.FileName = updateDocumentDto.FileName ?? document.FileName;
        document.Type = updateDocumentDto.Type ?? document.Type;
        document.SubType = updateDocumentDto.SubType ?? document.SubType;
        document.Url = updateDocumentDto.Url;
        document.ThumbnailStorageKey = updateDocumentDto.ThumbnailStorageKey;
        document.Expiry = updateDocumentDto.Expiry;
        document.Title = updateDocumentDto.Title;
        document.Notes = updateDocumentDto.Notes;
        document.ModifiedBy = currentUser;
        document.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(document, "Update", currentUser, originalDocument, cancellationToken);

        return MapToDocumentReferenceDto(document);
    }

    public async Task<bool> DeleteDocumentReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var document = await context.DocumentReferences
            .Where(d => d.Id == id && d.TenantId == currentTenantId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Document reference {DocumentId} not found for deletion", id);
            return false;
        }

        var originalDocument = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.Id == id && d.TenantId == currentTenantId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        document.IsDeleted = true;
        document.DeletedBy = currentUser;
        document.DeletedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(document, "Delete", currentUser, originalDocument, cancellationToken);

        return true;
    }

    // Membership Card operations

}
