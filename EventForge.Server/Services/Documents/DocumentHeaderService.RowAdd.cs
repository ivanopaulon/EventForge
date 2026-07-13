using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Verify document header exists
        var documentHeader = await context.DocumentHeaders
            .Include(dh => dh.DocumentType)
            .FirstOrDefaultAsync(dh => dh.Id == createDto.DocumentHeaderId && !dh.IsDeleted, cancellationToken);

        if (documentHeader is null)
        {
            throw new InvalidOperationException($"Document header with ID {createDto.DocumentHeaderId} not found.");
        }

        // Compute base quantity and base unit price if UnitOfMeasureId is provided
        decimal? baseQuantity = createDto.BaseQuantity;
        decimal? baseUnitPrice = createDto.BaseUnitPrice;
        Guid? baseUnitOfMeasureId = createDto.BaseUnitOfMeasureId;

        if (createDto.UnitOfMeasureId.HasValue && createDto.ProductId.HasValue)
        {
            // Load the ProductUnit to get the conversion factor and base unit
            var productUnit = await context.ProductUnits
                .FirstOrDefaultAsync(pu =>
                    pu.ProductId == createDto.ProductId.Value &&
                    pu.UnitOfMeasureId == createDto.UnitOfMeasureId.Value &&
                    !pu.IsDeleted,
                    cancellationToken);

            if (productUnit is not null)
            {
                // Find the base unit for this product (ConversionFactor = 1.0 and UnitType = "Base")
                var baseUnit = await context.ProductUnits
                    .FirstOrDefaultAsync(pu =>
                        pu.ProductId == createDto.ProductId.Value &&
                        pu.ConversionFactor == 1m &&
                        pu.UnitType == "Base" &&
                        !pu.IsDeleted,
                        cancellationToken);

                if (baseUnit is not null)
                {
                    baseUnitOfMeasureId = baseUnit.UnitOfMeasureId;

                    // Compute base quantity using conversion factor
                    baseQuantity = unitConversionService.ConvertToBaseUnit(
                        createDto.Quantity,
                        productUnit.ConversionFactor,
                        decimalPlaces: 4);

                    // Compute base unit price (inverse conversion for price)
                    if (createDto.UnitPrice > 0)
                    {
                        baseUnitPrice = unitConversionService.ConvertPrice(
                            createDto.UnitPrice,
                            fromConversionFactor: productUnit.ConversionFactor,
                            toConversionFactor: 1m,
                            decimalPlaces: 4);
                    }
                }
            }
        }

        // Check if we should merge with an existing IDENTICAL row
        if (createDto.MergeDuplicateProducts && createDto.ProductId.HasValue)
        {
            // Merge rules:
            // - Different UOM rows always merge (unit conversion case; quantities are summed in base units).
            // - Same UOM rows merge only when UnitPrice, VatRate, LineDiscount, and DiscountType all match.
            var existingRow = await context.DocumentRows
                .FirstOrDefaultAsync(r =>
                    r.DocumentHeaderId == createDto.DocumentHeaderId &&
                    r.ProductId == createDto.ProductId &&
                    !r.IsDeleted &&
                    (r.UnitOfMeasureId != createDto.UnitOfMeasureId || (
                        r.UnitPrice == createDto.UnitPrice &&
                        r.VatRate == createDto.VatRate &&
                        r.LineDiscount == createDto.LineDiscount &&
                        r.DiscountType == createDto.DiscountType
                    )),
                    cancellationToken);

            if (existingRow is not null)
            {

                // Merge: sum base quantities and recalculate display quantity if units differ
                if (baseQuantity.HasValue && existingRow.BaseQuantity.HasValue)
                {
                    existingRow.BaseQuantity += baseQuantity.Value;

                    // Recalculate the display quantity if the existing row has a unit
                    if (existingRow.UnitOfMeasureId.HasValue && createDto.ProductId.HasValue)
                    {
                        var existingProductUnit = await context.ProductUnits
                            .FirstOrDefaultAsync(pu =>
                                pu.ProductId == createDto.ProductId.Value &&
                                pu.UnitOfMeasureId == existingRow.UnitOfMeasureId.Value &&
                                !pu.IsDeleted,
                                cancellationToken);

                        if (existingProductUnit is not null)
                        {
                            existingRow.Quantity = unitConversionService.ConvertFromBaseUnit(
                                existingRow.BaseQuantity.Value,
                                existingProductUnit.ConversionFactor,
                                decimalPlaces: 4);
                        }
                        else
                        {
                            existingRow.Quantity += createDto.Quantity;
                        }
                    }
                    else
                    {
                        existingRow.Quantity += createDto.Quantity;
                    }
                }
                else
                {
                    // Fallback: just add quantities if base quantities not available
                    existingRow.Quantity += createDto.Quantity;
                }

                existingRow.ModifiedBy = currentUser;
                existingRow.ModifiedAt = DateTime.UtcNow;

                _ = await context.SaveChangesAsync(cancellationToken);

                _ = await auditLogService.TrackEntityChangesAsync(
                    existingRow,
                    "MergeUpdate",
                    currentUser,
                    null,
                    cancellationToken);

                logger.LogInformation(
                    "Row merged successfully: RowId={RowId}, NewQty={NewQty}, NewBaseQty={NewBaseQty}",
                    existingRow.Id,
                    existingRow.Quantity,
                    existingRow.BaseQuantity);

                // Create or update a stock movement for the merged quantity delta when the
                // document type requires it (same conditions as for a newly added row).
                if (documentHeader.DocumentType is not null &&
                    existingRow.ProductId.HasValue &&
                    (documentHeader.DocumentType.MovesStockOnRowChange || documentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived))
                {
                    var deltaQuantity = baseQuantity ?? createDto.Quantity;
                    var documentDateUtc = NormalizeDateToUtc(documentHeader.Date);

                    Guid? warehouseLocationId;
                    if (documentHeader.DocumentType.IsStockIncrease)
                    {
                        warehouseLocationId = existingRow.DestinationWarehouseId
                                           ?? documentHeader.DestinationWarehouseId
                                           ?? documentHeader.DocumentType.DefaultWarehouseId;
                    }
                    else
                    {
                        warehouseLocationId = existingRow.SourceWarehouseId
                                           ?? documentHeader.SourceWarehouseId
                                           ?? documentHeader.DocumentType.DefaultWarehouseId;
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
                                if (documentHeader.DocumentType.MovesStockOnRowChange)
                                {
                                    // Live mode: replace the existing movement with a new one for the merged total.
                                    await stockMovementService.DeleteMovementsForRowAsync(existingRow.Id, currentUser, cancellationToken);
                                    var mergedQuantity = existingRow.BaseQuantity ?? existingRow.Quantity;
                                    var notes = $"Live replacement movement after merge on document {documentHeader.Number}";
                                    if (documentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: existingRow.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: mergedQuantity,
                                            unitCost: ComputeNetUnitPrice(existingRow),
                                            documentHeaderId: documentHeader.Id,
                                            documentRowId: existingRow.Id,
                                            notes: notes,
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: existingRow.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: mergedQuantity,
                                            unitCost: ComputeNetUnitPrice(existingRow),
                                            documentHeaderId: documentHeader.Id,
                                            documentRowId: existingRow.Id,
                                            notes: notes,
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    logger.LogInformation("Replaced stock movement after merge for live-mode row {RowId}. New total: {Total}", existingRow.Id, mergedQuantity);
                                }
                                else
                                {
                                    // Archived doc: create a compensating movement for the added delta only.
                                    var notes = $"Auto-generated from document {documentHeader.Number}";
                                    if (documentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: existingRow.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: deltaQuantity,
                                            unitCost: ComputeNetUnitPrice(existingRow),
                                            documentHeaderId: documentHeader.Id,
                                            documentRowId: existingRow.Id,
                                            notes: notes,
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: existingRow.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: deltaQuantity,
                                            unitCost: ComputeNetUnitPrice(existingRow),
                                            documentHeaderId: documentHeader.Id,
                                            documentRowId: existingRow.Id,
                                            notes: notes,
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    logger.LogInformation("Created compensating stock movement for merged row {RowId} (archived). Delta: {Delta}", existingRow.Id, deltaQuantity);
                                }
                            }
                            catch (Exception ex)
                            {
                                // The row has already been saved; log the movement failure but do not
                                // re-throw so the caller receives the saved row DTO and the dialog closes.
                                logger.LogError(ex, "Failed to update stock movement for merged row {RowId}; the row was already persisted and the response will succeed.", existingRow.Id);
                            }
                        }
                        else
                        {
                            logger.LogWarning("No storage location found in warehouse {WarehouseId} for merged row {RowId}. Stock movement not updated.", warehouseLocationId, existingRow.Id);
                        }
                    }
                    else
                    {
                        logger.LogWarning("No warehouse found for merged row {RowId}. Stock movement not updated.", existingRow.Id);
                    }
                }

                return existingRow.ToDto();
            }
        }

        // Create new row (default behavior)
        var row = createDto.ToEntity();
        row.TenantId = documentHeader.TenantId; // Set TenantId from document header
        row.BaseQuantity = baseQuantity;
        row.BaseUnitPrice = baseUnitPrice;
        row.BaseUnitOfMeasureId = baseUnitOfMeasureId;
        row.CreatedBy = currentUser;
        row.CreatedAt = DateTime.UtcNow;

        _ = context.DocumentRows.Add(row);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(row, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Document row {RowId} added to document {DocumentHeaderId} by {User}.",
            row.Id, createDto.DocumentHeaderId, currentUser);

        // Auto-create or update ProductSupplier for purchase documents
        if (row.ProductId.HasValue && documentHeader.BusinessPartyId != Guid.Empty)
        {
            if (documentHeader.DocumentType?.IsStockIncrease == true)
            {
                await EnsureProductSupplierAsync(
                    row.ProductId!.Value,
                    documentHeader.BusinessPartyId,
                    row.UnitPrice,
                    currentUser,
                    cancellationToken);
            }
        }

        // Create a stock movement immediately when:
        // - the document is Active and the document type uses live "MovesStockOnRowChange" mode, OR
        // - the document is already Archived (compensate on add for already-posted documents)
        if (documentHeader.DocumentType is not null &&
            row.ProductId.HasValue &&
            (documentHeader.DocumentType.MovesStockOnRowChange || documentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived))
        {
            var isLiveMode = documentHeader.DocumentType.MovesStockOnRowChange;
            var documentDateUtc = NormalizeDateToUtc(documentHeader.Date);

            // Determine the warehouse location to use (same logic as ProcessStockMovementsForDocumentAsync)
            Guid? warehouseLocationId = null;
            if (documentHeader.DocumentType.IsStockIncrease)
            {
                warehouseLocationId = row.DestinationWarehouseId
                                   ?? documentHeader.DestinationWarehouseId
                                   ?? documentHeader.DocumentType.DefaultWarehouseId;
            }
            else
            {
                warehouseLocationId = row.SourceWarehouseId
                                   ?? documentHeader.SourceWarehouseId
                                   ?? documentHeader.DocumentType.DefaultWarehouseId;
            }

            if (warehouseLocationId.HasValue)
            {
                var storageLocation = await context.StorageLocations
                    .AsNoTracking()
                    .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storageLocation is not null)
                {
                    var notes = isLiveMode
                        ? $"Auto-generated live from document {documentHeader.Number}"
                        : $"Auto-generated from document {documentHeader.Number}";

                    try
                    {
                        if (documentHeader.DocumentType.IsStockIncrease)
                        {
                            await stockMovementService.ProcessInboundMovementAsync(
                                productId: row.ProductId!.Value,
                                toLocationId: storageLocation.Id,
                                quantity: row.Quantity,
                                unitCost: ComputeNetUnitPrice(row),
                                documentHeaderId: documentHeader.Id,
                                documentRowId: row.Id,
                                notes: notes,
                                currentUser: currentUser,
                                movementDate: documentDateUtc,
                                cancellationToken: cancellationToken);

                            logger.LogInformation("Created immediate inbound stock movement for document row {RowId} (liveMode={LiveMode}).", row.Id, isLiveMode);
                        }
                        else
                        {
                            await stockMovementService.ProcessOutboundMovementAsync(
                                productId: row.ProductId!.Value,
                                fromLocationId: storageLocation.Id,
                                quantity: row.Quantity,
                                unitCost: ComputeNetUnitPrice(row),
                                documentHeaderId: documentHeader.Id,
                                documentRowId: row.Id,
                                notes: notes,
                                currentUser: currentUser,
                                movementDate: documentDateUtc,
                                cancellationToken: cancellationToken);

                            logger.LogInformation("Created immediate outbound stock movement for document row {RowId} (liveMode={LiveMode}).", row.Id, isLiveMode);
                        }
                    }
                    catch (Exception ex)
                    {
                        // The row has already been saved; log the stock movement failure but do not
                        // re-throw so the caller receives the saved row DTO and the dialog closes.
                        logger.LogError(ex, "Failed to create stock movement for document row {RowId}; the row was already persisted and the response will succeed.", row.Id);
                    }
                }
                else
                {
                    logger.LogWarning("No storage location found in warehouse {WarehouseId} for document row {RowId}. Stock movement not created.",
                        warehouseLocationId, row.Id);
                }
            }
            else
            {
                logger.LogWarning("No warehouse found for document row {RowId}. Stock movement not created.", row.Id);
            }
        }

        return row.ToDto();
    }

}
