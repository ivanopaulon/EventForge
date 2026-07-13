using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    IDocumentCounterService documentCounterService,
    IStockMovementService stockMovementService,
    IUnitConversionService unitConversionService,
    ILogger<DocumentHeaderService> logger) : IDocumentHeaderService
{

    /// <summary>
    /// Processes stock movements for a document based on its type and rows.
    /// </summary>
    private async Task ProcessStockMovementsForDocumentAsync(
        Data.Entities.Documents.DocumentHeader documentHeader,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        if (documentHeader.DocumentType is null)
        {
            logger.LogWarning("Document type not loaded for document {DocumentHeaderId}. Cannot process stock movements.", documentHeader.Id);
            return;
        }

        // Inventory documents record absolute quantity anchors used for stock reconciliation and
        // snapshot calculations.  They are NOT incremental stock deltas, so they must never
        // generate automatic Inbound/Outbound movements.
        if (documentHeader.DocumentType.IsInventoryDocument || !documentHeader.DocumentType.CreatesStockMovements)
        {
            logger.LogInformation(
                "Document {DocumentHeaderId} (type '{Code}') is flagged as inventory or non-movement — stock movements are not generated automatically.",
                documentHeader.Id, documentHeader.DocumentType.Code);
            return;
        }

        // Documents with MovesStockOnRowChange already created movements per-row — skip bulk generation.
        if (documentHeader.DocumentType.MovesStockOnRowChange)
        {
            logger.LogInformation(
                "Document {DocumentHeaderId} (type '{Code}') uses live per-row stock movements — bulk generation skipped.",
                documentHeader.Id, documentHeader.DocumentType.Code);
            return;
        }

        if (documentHeader.Rows is null || !documentHeader.Rows.Any())
        {
            return;
        }

        // Ensure document date is in UTC for stock movements
        var documentDateUtc = NormalizeDateToUtc(documentHeader.Date);

        foreach (var row in documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue))
        {
            // Per-row guard: skip only this row if its movement already exists
            var rowMovementExists = await context.StockMovements
                .AnyAsync(sm => sm.DocumentRowId == row.Id && !sm.IsDeleted, cancellationToken);

            if (rowMovementExists)
            {
                continue;
            }

            // Determine the warehouse location to use
            Guid? warehouseLocationId = null;

            // For stock increase documents (purchases, returns)
            if (documentHeader.DocumentType.IsStockIncrease)
            {
                // Use destination warehouse from row, or document, or document type default
                warehouseLocationId = row.DestinationWarehouseId
                                   ?? documentHeader.DestinationWarehouseId
                                   ?? documentHeader.DocumentType.DefaultWarehouseId;

                if (!warehouseLocationId.HasValue)
                {
                    logger.LogWarning("No destination warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                        row.Id, documentHeader.Id);
                    continue;
                }

                // Get the first storage location in the warehouse
                var storageLocation = await context.StorageLocations
                    .AsNoTracking()
                    .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storageLocation is null)
                {
                    logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                        warehouseLocationId, row.Id);
                    continue;
                }

                // Create inbound movement
                await stockMovementService.ProcessInboundMovementAsync(
                    productId: row.ProductId!.Value,
                    toLocationId: storageLocation.Id,
                    quantity: row.Quantity,
                    unitCost: ComputeNetUnitPrice(row),
                    lotId: null,
                    serialId: null,
                    documentHeaderId: documentHeader.Id,
                    documentRowId: row.Id,
                    notes: $"Auto-generated from document {documentHeader.Number}",
                    currentUser: currentUser,
                    movementDate: documentDateUtc,
                    cancellationToken: cancellationToken);

            }
            // For stock decrease documents (sales, deliveries)
            else
            {
                // Use source warehouse from row, or document, or document type default
                warehouseLocationId = row.SourceWarehouseId
                                   ?? documentHeader.SourceWarehouseId
                                   ?? documentHeader.DocumentType.DefaultWarehouseId;

                if (!warehouseLocationId.HasValue)
                {
                    logger.LogWarning("No source warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                        row.Id, documentHeader.Id);
                    continue;
                }

                // Get the storage location with available stock
                var storageLocation = await context.StorageLocations
                    .AsNoTracking()
                    .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (storageLocation is null)
                {
                    logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                        warehouseLocationId, row.Id);
                    continue;
                }

                // Check if sufficient stock is available
                var availableStock = await context.Stocks
                    .Where(s => s.ProductId == row.ProductId!.Value
                             && s.StorageLocationId == storageLocation.Id
                             && !s.IsDeleted)
                    .SumAsync(s => s.Quantity - s.ReservedQuantity, cancellationToken);

                if (availableStock < row.Quantity)
                {
                    logger.LogWarning("Insufficient stock for product {ProductId} at location {LocationId}. Available: {Available}, Required: {Required}.",
                        row.ProductId!.Value, storageLocation.Id, availableStock, row.Quantity);
                    // Continue processing but log the warning
                }

                // Create outbound movement
                await stockMovementService.ProcessOutboundMovementAsync(
                    productId: row.ProductId!.Value,
                    fromLocationId: storageLocation.Id,
                    quantity: row.Quantity,
                    unitCost: ComputeNetUnitPrice(row),
                    lotId: null,
                    serialId: null,
                    documentHeaderId: documentHeader.Id,
                    documentRowId: row.Id,
                    notes: $"Auto-generated from document {documentHeader.Number}",
                    currentUser: currentUser,
                    movementDate: documentDateUtc,
                    cancellationToken: cancellationToken);

            }
        }

        logger.LogInformation("Completed processing stock movements for document {DocumentHeaderId}.", documentHeader.Id);
    }

    /// <summary>
    /// Synchronizes stock movement dates for a document when the document date changes.
    /// </summary>
    private async Task SyncStockMovementDatesForDocumentAsync(
        Guid documentHeaderId,
        DateTime newDate,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Ensure the date is in UTC
        var newDateUtc = NormalizeDateToUtc(newDate);
        var modifiedAt = DateTime.UtcNow;

        // Try batch SQL update for efficiency (works with SQL Server)
        // Fall back to in-memory update for test databases
        int affected;
        try
        {
            affected = await context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE StockMovements
                       SET MovementDate = {newDateUtc}, 
                           ModifiedAt = {modifiedAt}, 
                           ModifiedBy = {currentUser}
                       WHERE DocumentHeaderId = {documentHeaderId} 
                         AND IsDeleted = 0",
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Fallback for in-memory databases used in tests
            var movements = await context.StockMovements
                .Where(sm => sm.DocumentHeaderId == documentHeaderId && !sm.IsDeleted)
                .ToListAsync(cancellationToken);

            affected = movements.Count;
            foreach (var movement in movements)
            {
                movement.MovementDate = newDateUtc;
                movement.ModifiedAt = modifiedAt;
                movement.ModifiedBy = currentUser;
            }

            if (affected > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        if (affected > 0)
        {
            // Log the sync operation
            await auditLogService.LogEntityChangeAsync(
                "StockMovement",
                documentHeaderId,
                "MovementDate",
                "BulkUpdate",
                null,
                $"Synchronized {affected} stock movement(s) to document date {newDateUtc:yyyy-MM-dd HH:mm:ss} UTC",
                currentUser);

            logger.LogInformation("Synchronized {Count} stock movement dates for document {DocumentHeaderId} to {NewDate}.",
                affected, documentHeaderId, newDateUtc);
        }
        else
        {
        }
    }

    /// <summary>
    /// Ensures a ProductSupplier relationship exists for the given product and supplier.
    /// If it exists, updates the last purchase price and date. If not, creates a new one.
    /// </summary>
    private async Task EnsureProductSupplierAsync(Guid productId, Guid supplierId,
        decimal unitPrice, string currentUser, CancellationToken ct)
    {
        try
        {
            var existing = await context.Set<ProductSupplier>()
                .FirstOrDefaultAsync(ps => ps.ProductId == productId &&
                    ps.SupplierId == supplierId && !ps.IsDeleted, ct);

            if (existing is not null)
            {
                existing.LastPurchasePrice = unitPrice;
                existing.LastPurchaseDate = DateTime.UtcNow;
                existing.ModifiedBy = currentUser;
                existing.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                var tenantId = tenantContext.CurrentTenantId!.Value;
                context.Set<ProductSupplier>().Add(new ProductSupplier
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    SupplierId = supplierId,
                    TenantId = tenantId,
                    UnitCost = unitPrice,
                    LastPurchasePrice = unitPrice,
                    LastPurchaseDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync(ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring ProductSupplier for Product {ProductId} and Supplier {SupplierId}.",
                productId, supplierId);
            // Don't throw - this is a non-critical operation
        }
    }

    /// <summary>
    /// Computes the effective net unit price for a document row, applying any line
    /// discount (percentage) to the base unit price.
    /// <para>
    /// In purchase documents the operator enters a supplier list price and optional
    /// chained trade discounts (e.g. "10+5"). <see cref="DocumentRow.LineDiscount"/>
    /// already holds the cascaded equivalent percentage, so the true cost per unit is
    /// <c>UnitPrice × (1 − LineDiscount / 100)</c>. This value must be recorded as
    /// <see cref="StockMovement.UnitCost"/> to keep price history accurate.
    /// </para>
    /// </summary>
    private static decimal ComputeNetUnitPrice(DocumentRow row)
    {
        if (row.LineDiscount <= 0m)
            return row.UnitPrice;

        var netPrice = row.UnitPrice * (1m - row.LineDiscount / 100m);
        return Math.Round(netPrice, 6, MidpointRounding.AwayFromZero);
    }

}
