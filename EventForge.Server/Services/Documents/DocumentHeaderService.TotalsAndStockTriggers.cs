using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var documentHeader = await context.DocumentHeaders
            .Include(dh => dh.Rows.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

        if (documentHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found for total calculation.", id);
            return null;
        }

        var netTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)));
        var vatTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)) * (r.VatRate / 100m));

        if (documentHeader.TotalDiscount > 0)
            netTotal -= netTotal * (documentHeader.TotalDiscount / 100m);

        netTotal -= documentHeader.TotalDiscountAmount;

        documentHeader.TotalNetAmount = Math.Max(0, netTotal);
        documentHeader.VatAmount = vatTotal;
        documentHeader.TotalGrossAmount = documentHeader.TotalNetAmount + documentHeader.VatAmount;

        _ = await context.SaveChangesAsync(cancellationToken);


        return documentHeader.ToDto();
    }

    public async Task TriggerStockMovementsForDocumentAsync(
        Guid documentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var documentForStockMovement = await context.DocumentHeaders
            .AsNoTracking()
            .Include(dh => dh.DocumentType)
            .Include(dh => dh.Rows)
            .FirstOrDefaultAsync(dh => dh.Id == documentId && !dh.IsDeleted, cancellationToken);

        if (documentForStockMovement is not null)
        {
            await ProcessStockMovementsForDocumentAsync(documentForStockMovement, currentUser, cancellationToken);
        }
        else
        {
            logger.LogWarning("TriggerStockMovementsForDocumentAsync: document {DocumentId} not found.", documentId);
        }
    }

}
