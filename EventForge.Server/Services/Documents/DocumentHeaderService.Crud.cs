using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot create document header without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var documentHeader = createDto.ToEntity();
        documentHeader.TenantId = tenantId.Value;
        documentHeader.CreatedBy = currentUser;
        documentHeader.CreatedAt = DateTime.UtcNow;

        // Auto-generate document number if not provided
        if (string.IsNullOrWhiteSpace(documentHeader.Number))
        {
            var series = documentHeader.Series ?? string.Empty;
            documentHeader.Number = await documentCounterService.GenerateDocumentNumberAsync(
                documentHeader.DocumentTypeId,
                series,
                currentUser,
                cancellationToken);

            logger.LogInformation("Auto-generated document number '{Number}' for document type {DocumentTypeId}, series '{Series}'.",
                documentHeader.Number, documentHeader.DocumentTypeId, series);
        }

        _ = context.DocumentHeaders.Add(documentHeader);

        if (createDto.Rows?.Any() == true)
        {
            foreach (var rowDto in createDto.Rows)
            {
                var row = rowDto.ToEntity();
                row.DocumentHeaderId = documentHeader.Id;
                row.TenantId = tenantId.Value;
                row.CreatedBy = currentUser;
                row.CreatedAt = DateTime.UtcNow;
                documentHeader.Rows.Add(row);
            }

            // Calculate and persist totals from the provided rows
            var netTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)));
            var vatTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)) * (r.VatRate / 100m));

            if (documentHeader.TotalDiscount > 0)
                netTotal -= netTotal * (documentHeader.TotalDiscount / 100m);

            netTotal -= documentHeader.TotalDiscountAmount;

            documentHeader.TotalNetAmount = Math.Max(0, netTotal);
            documentHeader.VatAmount = vatTotal;
            documentHeader.TotalGrossAmount = documentHeader.TotalNetAmount + documentHeader.VatAmount;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Document header {DocumentHeaderId} created by {User}.", documentHeader.Id, currentUser);

        return documentHeader.ToDto();
    }

    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for this operation.");

            var originalHeader = await context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && dh.TenantId == currentTenantId && !dh.IsDeleted, cancellationToken);

            if (originalHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for update.", id);
                return null;
            }

            var documentHeader = await context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && dh.TenantId == currentTenantId && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for update.", id);
                return null;
            }

            // Detect if Date changed to sync stock movements
            // Normalize both dates to UTC for proper comparison
            var originalDateUtc = NormalizeDateToUtc(originalHeader.Date);
            var newDateUtc = NormalizeDateToUtc(updateDto.Date);
            var dateChanged = originalDateUtc != newDateUtc;

            documentHeader.UpdateFromDto(updateDto);
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating document header {DocumentHeaderId}.", id);
                throw new InvalidOperationException("Il documento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Update", currentUser, originalHeader, cancellationToken);

            // Sync stock movement dates if document date changed
            if (dateChanged)
            {
                await SyncStockMovementDatesForDocumentAsync(id, newDateUtc, currentUser, cancellationToken);
            }

            // Process stock movements on every content save so that movements are always
            // up-to-date with the current rows, regardless of the document status.
            var documentForStockMovement = await context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentForStockMovement is not null)
            {
                await ProcessStockMovementsForDocumentAsync(documentForStockMovement, currentUser, cancellationToken);
            }

            logger.LogInformation("Document header {DocumentHeaderId} updated by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for this operation.");

        var originalHeader = await context.DocumentHeaders
            .AsNoTracking()
            .Include(dh => dh.Rows)
            .FirstOrDefaultAsync(dh => dh.Id == id && dh.TenantId == currentTenantId && !dh.IsDeleted, cancellationToken);

        if (originalHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
            return false;
        }

        var documentHeader = await context.DocumentHeaders
            .Include(dh => dh.Rows)
            .Include(dh => dh.DocumentType)
            .FirstOrDefaultAsync(dh => dh.Id == id && dh.TenantId == currentTenantId && !dh.IsDeleted, cancellationToken);

        if (documentHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
            return false;
        }

        // If the document is archived and DocumentType is available, generate compensating movements BEFORE delete
        if (documentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived
            && documentHeader.DocumentType is not null)
        {
            var compensatingCount = 0;
            var documentDateUtc = NormalizeDateToUtc(documentHeader.Date);

            foreach (var row in documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue))
            {
                Guid? warehouseLocationId = documentHeader.DocumentType.IsStockIncrease
                    ? row.DestinationWarehouseId ?? documentHeader.DestinationWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId
                    : row.SourceWarehouseId ?? documentHeader.SourceWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId;

                if (!warehouseLocationId.HasValue)
                {
                    logger.LogWarning("No warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping compensating movement.", row.Id, id);
                    continue;
                }

                var storageLocation = await context.StorageLocations
                    .AsNoTracking()
                    .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storageLocation is null)
                {
                    logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping compensating movement.", warehouseLocationId, row.Id);
                    continue;
                }

                var quantity = row.BaseQuantity ?? row.Quantity;
                var notes = $"Compensating movement: document {documentHeader.Id} deleted by {currentUser}";

                if (documentHeader.DocumentType.IsStockIncrease)
                {
                    // Original was Inbound → compensating is Outbound
                    await stockMovementService.ProcessOutboundMovementAsync(
                        productId: row.ProductId!.Value,
                        fromLocationId: storageLocation.Id,
                        quantity: quantity,
                        unitCost: ComputeNetUnitPrice(row),
                        documentHeaderId: documentHeader.Id,
                        documentRowId: row.Id,
                        notes: notes,
                        currentUser: currentUser,
                        movementDate: documentDateUtc,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // Original was Outbound → compensating is Inbound
                    await stockMovementService.ProcessInboundMovementAsync(
                        productId: row.ProductId!.Value,
                        toLocationId: storageLocation.Id,
                        quantity: quantity,
                        unitCost: ComputeNetUnitPrice(row),
                        documentHeaderId: documentHeader.Id,
                        documentRowId: row.Id,
                        notes: notes,
                        currentUser: currentUser,
                        movementDate: documentDateUtc,
                        cancellationToken: cancellationToken);
                }

                compensatingCount++;
            }

            logger.LogInformation("Created {Count} compensating stock movements before deleting document {DocumentHeaderId}.", compensatingCount, id);
        }

        documentHeader.IsDeleted = true;
        documentHeader.ModifiedBy = currentUser;
        documentHeader.ModifiedAt = DateTime.UtcNow;

        foreach (var row in documentHeader.Rows)
        {
            row.IsDeleted = true;
            row.ModifiedBy = currentUser;
            row.ModifiedAt = DateTime.UtcNow;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Delete", currentUser, originalHeader, cancellationToken);

        logger.LogInformation("Document header {DocumentHeaderId} deleted by {User}.", id, currentUser);

        return true;
    }

}
