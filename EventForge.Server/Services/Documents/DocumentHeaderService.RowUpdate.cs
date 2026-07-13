using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for this operation.");

        var row = await context.DocumentRows
            .Include(r => r.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .FirstOrDefaultAsync(r => r.Id == rowId && r.TenantId == currentTenantId && !r.IsDeleted, cancellationToken);

        if (row is null)
        {
            logger.LogWarning("Document row {RowId} not found for update.", rowId);
            return null;
        }

        // Store old base quantity for compensating movement calculation
        var oldBaseQuantity = row.BaseQuantity ?? row.Quantity;
        var oldProductId = row.ProductId;

        // Update row properties
        row.RowType = (Data.Entities.Documents.DocumentRowType)updateDto.RowType;
        row.ParentRowId = updateDto.ParentRowId;
        row.ProductCode = updateDto.ProductCode;
        row.Description = updateDto.Description;
        row.UnitOfMeasure = updateDto.UnitOfMeasure;
        row.UnitOfMeasureId = updateDto.UnitOfMeasureId;
        row.UnitPrice = updateDto.UnitPrice;
        row.Quantity = updateDto.Quantity;
        row.LineDiscount = updateDto.LineDiscount;
        row.LineDiscountString = updateDto.LineDiscountString;
        row.LineDiscountValue = updateDto.LineDiscountValue;
        row.DiscountType = (Prym.DTOs.Common.DiscountType)updateDto.DiscountType;
        row.VatRate = updateDto.VatRate;
        row.VatDescription = updateDto.VatDescription;
        row.IsGift = updateDto.IsGift;
        row.IsManual = updateDto.IsManual;
        row.SourceWarehouseId = updateDto.SourceWarehouseId;
        row.DestinationWarehouseId = updateDto.DestinationWarehouseId;
        row.Notes = updateDto.Notes;
        row.SortOrder = updateDto.SortOrder;
        row.StationId = updateDto.StationId;
        row.BaseQuantity = updateDto.BaseQuantity;
        row.BaseUnitPrice = updateDto.BaseUnitPrice;
        row.BaseUnitOfMeasureId = updateDto.BaseUnitOfMeasureId;
        row.IsPriceManual = updateDto.IsPriceManual;
        row.AppliedPriceListId = updateDto.AppliedPriceListId;
        row.OriginalPriceFromPriceList = updateDto.OriginalPriceFromPriceList;
        row.PriceNotes = updateDto.PriceNotes;
        row.AppliedPromotionsJSON = updateDto.AppliedPromotionsJSON;
        row.SupplierGrossPrice = updateDto.SupplierGrossPrice;
        row.ModifiedBy = currentUser;
        row.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(row, "Update", currentUser, null, cancellationToken);

        logger.LogInformation("Document row {RowId} updated by {User}.", rowId, currentUser);

        // If document is archived and quantity changed, create compensating movement
        if (row.DocumentHeader is not null &&
            row.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived &&
            row.ProductId.HasValue &&
            row.ProductId == oldProductId)
        {
            var newBaseQuantity = row.BaseQuantity ?? row.Quantity;
            var delta = newBaseQuantity - oldBaseQuantity;
            if (delta != 0)
            {
                var documentDateUtc = NormalizeDateToUtc(row.DocumentHeader.Date);

                // Determine warehouse location
                Guid? warehouseLocationId = null;
                if (row.DocumentHeader.DocumentType is not null)
                {
                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                    {
                        warehouseLocationId = row.DestinationWarehouseId
                                           ?? row.DocumentHeader.DestinationWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }
                    else
                    {
                        warehouseLocationId = row.SourceWarehouseId
                                           ?? row.DocumentHeader.SourceWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }

                    if (warehouseLocationId.HasValue)
                    {
                        var storageLocation = await context.StorageLocations
                            .AsNoTracking()
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation is not null)
                        {
                            try
                            {
                                if (delta > 0)
                                {
                                    // Positive delta: add more stock
                                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: delta,
                                            unitCost: ComputeNetUnitPrice(row),
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: delta,
                                            unitCost: ComputeNetUnitPrice(row),
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                }
                                else
                                {
                                    // Negative delta: remove stock
                                    var absDelta = Math.Abs(delta);
                                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            unitCost: ComputeNetUnitPrice(row),
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            unitCost: ComputeNetUnitPrice(row),
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                }

                                logger.LogInformation("Created compensating stock movement for updated row {RowId} in archived document. Delta: {Delta}",
                                    rowId, delta);
                            }
                            catch (Exception ex)
                            {
                                // Row is already saved; log the movement failure without re-throwing.
                                logger.LogError(ex, "Failed to create compensating stock movement for row {RowId}; the row was already persisted and the response will succeed.", rowId);
                            }
                        }
                    }
                }
            }
        }
        // If document type uses live "MovesStockOnRowChange" mode, replace movement for this row
        else if (row.DocumentHeader is not null &&
                 (row.DocumentHeader.DocumentType?.MovesStockOnRowChange ?? false) &&
                 row.ProductId.HasValue)
        {
            try
            {
                // Delete all existing movements for this row, then create a fresh one
                await stockMovementService.DeleteMovementsForRowAsync(rowId, currentUser, cancellationToken);

                if (row.DocumentHeader.DocumentType is not null)
                {
                    var documentDateUtc = NormalizeDateToUtc(row.DocumentHeader.Date);

                    Guid? warehouseLocationId = null;
                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                    {
                        warehouseLocationId = row.DestinationWarehouseId
                                           ?? row.DocumentHeader.DestinationWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }
                    else
                    {
                        warehouseLocationId = row.SourceWarehouseId
                                           ?? row.DocumentHeader.SourceWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }

                    if (warehouseLocationId.HasValue)
                    {
                        var storageLocation = await context.StorageLocations
                            .AsNoTracking()
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation is not null)
                        {
                            var currentQuantity = row.BaseQuantity ?? row.Quantity;
                            if (row.DocumentHeader.DocumentType.IsStockIncrease)
                            {
                                await stockMovementService.ProcessInboundMovementAsync(
                                    productId: row.ProductId!.Value,
                                    toLocationId: storageLocation.Id,
                                    quantity: currentQuantity,
                                    unitCost: ComputeNetUnitPrice(row),
                                    documentHeaderId: row.DocumentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Live replacement movement from document {row.DocumentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await stockMovementService.ProcessOutboundMovementAsync(
                                    productId: row.ProductId!.Value,
                                    fromLocationId: storageLocation.Id,
                                    quantity: currentQuantity,
                                    unitCost: ComputeNetUnitPrice(row),
                                    documentHeaderId: row.DocumentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Live replacement movement from document {row.DocumentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);
                            }

                            logger.LogInformation("Replaced stock movement for live-mode row {RowId} with new quantity {Quantity}.", rowId, currentQuantity);
                        }
                        else
                        {
                            logger.LogWarning("No storage location found in warehouse {WarehouseId} for live-mode row {RowId}. Stock movement not replaced.",
                                warehouseLocationId, rowId);
                        }
                    }
                    else
                    {
                        logger.LogWarning("No warehouse found for live-mode row {RowId}. Stock movement not replaced.", rowId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Row is already saved; log the movement failure without re-throwing.
                logger.LogError(ex, "Failed to replace stock movement for live-mode row {RowId}; the row was already persisted and the response will succeed.", rowId);
            }
        }

        return row.ToDto();
    }

}
