using EventForge.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document summary links.
/// </summary>
public class DocumentSummaryLinkService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<DocumentSummaryLinkService> logger) : IDocumentSummaryLinkService
{
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentSummaryLinkDto>> GetLinksBySummaryDocumentIdAsync(
        Guid summaryDocumentId,
        CancellationToken cancellationToken = default)
    {
        var links = await context.DocumentSummaryLinks
            .AsNoTracking()
            .Include(l => l.SummaryDocument)
            .Include(l => l.DetailedDocument)
            .Where(l => l.SummaryDocumentId == summaryDocumentId && !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentSummaryLinkDto>> GetLinksByDetailedDocumentIdAsync(
        Guid detailedDocumentId,
        CancellationToken cancellationToken = default)
    {
        // DetailedDocumentId is a shadow property; access via EF shadow property query
        var links = await context.DocumentSummaryLinks
            .AsNoTracking()
            .Include(l => l.SummaryDocument)
            .Include(l => l.DetailedDocument)
            .Where(l => EF.Property<Guid?>(l, "DetailedDocumentId") == detailedDocumentId && !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentSummaryLinkDto?> GetDocumentSummaryLinkByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var link = await context.DocumentSummaryLinks
            .AsNoTracking()
            .Include(l => l.SummaryDocument)
            .Include(l => l.DetailedDocument)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

        return link is not null ? MapToDto(link) : null;
    }

    /// <inheritdoc />
    public async Task<DocumentSummaryLinkDto> CreateDocumentSummaryLinkAsync(
        CreateDocumentSummaryLinkDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var link = new DocumentSummaryLink
        {
            Id = Guid.NewGuid(),
            SummaryDocumentId = createDto.SummaryDocumentId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            TenantId = tenantContext.CurrentTenantId ?? Guid.Empty
        };

        // Set shadow property DetailedDocumentId
        var entry = context.DocumentSummaryLinks.Add(link);
        if (createDto.DetailedDocumentId.HasValue)
            context.Entry(link).Property("DetailedDocumentId").CurrentValue = createDto.DetailedDocumentId.Value;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentSummaryLink", link.Id, "CREATE", "CREATE",
            null, $"Created link between summary {createDto.SummaryDocumentId} and detailed {createDto.DetailedDocumentId}", currentUser);

        logger.LogInformation("Created DocumentSummaryLink {LinkId} for user {User}", link.Id, currentUser);

        // Re-load with navigation properties for the DTO
        return MapToDto(link, createDto.SummaryDocumentId, createDto.DetailedDocumentId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentSummaryLinkAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var link = await context.DocumentSummaryLinks
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

        if (link is null)
            return false;

        link.IsDeleted = true;
        link.ModifiedAt = DateTime.UtcNow;
        link.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentSummaryLink", link.Id, "DELETE", "DELETE",
            null, $"Deleted summary link {link.Id}", currentUser);

        logger.LogInformation("Deleted DocumentSummaryLink {LinkId} for user {User}", link.Id, currentUser);
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentSummaryLinkDto>> BulkCreateDocumentSummaryLinksAsync(
        Guid summaryDocumentId,
        IEnumerable<Guid> detailedDocumentIds,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var idList = detailedDocumentIds.ToList();
        var now = DateTime.UtcNow;
        var tenantId = tenantContext.CurrentTenantId ?? Guid.Empty;

        var links = idList.Select(detailedId =>
        {
            var link = new DocumentSummaryLink
            {
                Id = Guid.NewGuid(),
                SummaryDocumentId = summaryDocumentId,
                CreatedAt = now,
                CreatedBy = currentUser,
                TenantId = tenantId
            };
            return (link, detailedId);
        }).ToList();

        foreach (var (link, detailedId) in links)
        {
            context.DocumentSummaryLinks.Add(link);
            context.Entry(link).Property("DetailedDocumentId").CurrentValue = detailedId;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentSummaryLink", summaryDocumentId, "BULK_CREATE", "BULK_CREATE",
            null, $"Bulk created {links.Count} summary links for document {summaryDocumentId}", currentUser);

        logger.LogInformation("Bulk created {Count} DocumentSummaryLinks for document {DocumentId}", links.Count, summaryDocumentId);
        return links.Select(pair => MapToDto(pair.link, summaryDocumentId, pair.detailedId));
    }

    /// <inheritdoc />
    public async Task<bool> DocumentSummaryLinkExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.DocumentSummaryLinks
            .AsNoTracking()
            .AnyAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> LinkExistsAsync(
        Guid summaryDocumentId,
        Guid? detailedDocumentId,
        CancellationToken cancellationToken = default)
    {
        return await context.DocumentSummaryLinks
            .AsNoTracking()
            .AnyAsync(l =>
                l.SummaryDocumentId == summaryDocumentId &&
                EF.Property<Guid?>(l, "DetailedDocumentId") == detailedDocumentId &&
                !l.IsDeleted, cancellationToken);
    }

    private static DocumentSummaryLinkDto MapToDto(DocumentSummaryLink link)
    {
        var detailedDocId = link.DetailedDocument?.Id;
        return new DocumentSummaryLinkDto
        {
            Id = link.Id,
            SummaryDocumentId = link.SummaryDocumentId,
            SummaryDocumentNumber = link.SummaryDocument?.Number,
            SummaryDocumentDate = link.SummaryDocument?.Date,
            DetailedDocumentId = detailedDocId,
            DetailedDocumentNumber = link.DetailedDocument?.Number,
            DetailedDocumentDate = link.DetailedDocument?.Date,
            CreatedAt = link.CreatedAt,
            CreatedBy = link.CreatedBy,
            ModifiedAt = link.ModifiedAt,
            ModifiedBy = link.ModifiedBy
        };
    }

    private static DocumentSummaryLinkDto MapToDto(DocumentSummaryLink link, Guid summaryDocumentId, Guid? detailedDocumentId)
    {
        return new DocumentSummaryLinkDto
        {
            Id = link.Id,
            SummaryDocumentId = summaryDocumentId,
            DetailedDocumentId = detailedDocumentId,
            CreatedAt = link.CreatedAt,
            CreatedBy = link.CreatedBy,
            ModifiedAt = link.ModifiedAt,
            ModifiedBy = link.ModifiedBy
        };
    }
}
