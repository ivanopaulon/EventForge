using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<DocumentHeaderDto?> ArchiveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var originalHeader = await context.DocumentHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

        if (originalHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found for archiving.", id);
            return null;
        }

        if (originalHeader.Status != Prym.DTOs.Common.DocumentStatus.Active)
        {
            throw new InvalidOperationException("Solo i documenti nello stato Attivo possono essere archiviati.");
        }

        var documentHeader = await context.DocumentHeaders
            .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

        if (documentHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found for archiving.", id);
            return null;
        }

        documentHeader.Status = Prym.DTOs.Common.DocumentStatus.Archived;
        documentHeader.ArchivedAt = DateTime.UtcNow;
        documentHeader.ModifiedBy = currentUser;
        documentHeader.ModifiedAt = DateTime.UtcNow;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict archiving document {DocumentHeaderId}.", id);
            throw new InvalidOperationException("Il documento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Archive", currentUser, originalHeader, cancellationToken);

        logger.LogInformation("Document header {DocumentHeaderId} archived by {User}.", id, currentUser);

        return documentHeader.ToDto();
    }

    public async Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.DocumentHeaders
            .AsNoTracking()
            .AnyAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Returns <paramref name="date"/> with <see cref="DateTimeKind.Utc"/> ensured.
    /// SQL Server stores datetime values as UTC but EF Core reads them back with
    /// <see cref="DateTimeKind.Unspecified"/>.  This helper relabels the value without
    /// changing the numeric ticks, which is correct when the DB always stores UTC.
    /// If the value is already <see cref="DateTimeKind.Utc"/> it is returned unchanged.
    /// </summary>
    private static DateTime NormalizeDateToUtc(DateTime date) =>
        date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);

    private IQueryable<DocumentHeader> BuildDocumentHeaderQuery(DocumentHeaderQueryParameters parameters)
    {
        var query = context.DocumentHeaders.AsNoTracking().Where(dh => !dh.IsDeleted);

        // Restrict to the current tenant to prevent cross-tenant data leakage
        var currentTenantId = tenantContext.CurrentTenantId;
        if (currentTenantId.HasValue)
            query = query.Where(dh => dh.TenantId == currentTenantId.Value);

        // Exclude archived documents by default unless explicitly requested or a specific status is filtered
        if (!parameters.Status.HasValue && !parameters.IncludeArchived)
            query = query.Where(dh => dh.Status != Prym.DTOs.Common.DocumentStatus.Archived);

        if (parameters.DocumentTypeId.HasValue)
            query = query.Where(dh => dh.DocumentTypeId == parameters.DocumentTypeId.Value);

        if (!string.IsNullOrEmpty(parameters.Number))
            query = query.Where(dh => dh.Number.Contains(parameters.Number));

        if (!string.IsNullOrEmpty(parameters.Series))
            query = query.Where(dh => dh.Series == parameters.Series);

        if (parameters.FromDate.HasValue)
            query = query.Where(dh => dh.Date >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(dh => dh.Date <= parameters.ToDate.Value);

        if (parameters.BusinessPartyId.HasValue)
            query = query.Where(dh => dh.BusinessPartyId == parameters.BusinessPartyId.Value);

        if (!string.IsNullOrEmpty(parameters.CustomerName))
            query = query.Where(dh => dh.CustomerName != null && dh.CustomerName.Contains(parameters.CustomerName));

        if (parameters.Status.HasValue)
            query = query.Where(dh => dh.Status == (Prym.DTOs.Common.DocumentStatus)parameters.Status.Value);

        if (parameters.PaymentStatus.HasValue)
            query = query.Where(dh => dh.PaymentStatus == (EventForge.Server.Data.Entities.Documents.PaymentStatus)parameters.PaymentStatus.Value);

        if (parameters.TeamId.HasValue)
            query = query.Where(dh => dh.TeamId == parameters.TeamId.Value);

        if (parameters.EventId.HasValue)
            query = query.Where(dh => dh.EventId == parameters.EventId.Value);

        if (parameters.SourceWarehouseId.HasValue)
            query = query.Where(dh => dh.SourceWarehouseId == parameters.SourceWarehouseId.Value);

        if (parameters.DestinationWarehouseId.HasValue)
            query = query.Where(dh => dh.DestinationWarehouseId == parameters.DestinationWarehouseId.Value);

        if (parameters.IsFiscal.HasValue)
            query = query.Where(dh => dh.IsFiscal == parameters.IsFiscal.Value);

        if (parameters.IsProforma.HasValue)
            query = query.Where(dh => dh.IsProforma == parameters.IsProforma.Value);

        if (parameters.ProductId.HasValue)
            query = query.Where(dh => dh.Rows.Any(r => !r.IsDeleted && r.ProductId == parameters.ProductId.Value));

        return query;
    }

}
