using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    /// <summary>
    /// Deletes a document row.
    /// </summary>
    public async Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
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
            logger.LogWarning("Document row {RowId} not found for deletion.", rowId);
            return false;
        }

        // If document is archived, create compensating movement before deleting row
        if (row.DocumentHeader is not null &&
            row.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived &&
            row.ProductId.HasValue &&
            !(row.DocumentHeader.DocumentType?.MovesStockOnRowChange ?? false))
        {
            // Find ALL existing movements for this row (there can be more than one after merges)
            var existingMovements = await context.StockMovements
                .Where(sm => sm.DocumentRowId == rowId && !sm.IsDeleted)
                .ToListAsync(cancellationToken);

            if (existingMovements.Count > 0 && row.DocumentHeader.DocumentType is not null)
            {
                var documentDateUtc = NormalizeDateToUtc(row.DocumentHeader.Date);

                // Determine warehouse location
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
                        foreach (var existingMovement in existingMovements)
                        {
                            try
                            {
                                // Create reverse movement to compensate for each existing movement
                                if (existingMovement.MovementType == StockMovementType.Inbound)
                                {
                                    await stockMovementService.ProcessOutboundMovementAsync(
                                        productId: existingMovement.ProductId,
                                        fromLocationId: existingMovement.ToLocationId ?? storageLocation.Id,
                                        quantity: existingMovement.Quantity,
                                        unitCost: ComputeNetUnitPrice(row),
                                        documentHeaderId: row.DocumentHeader.Id,
                                        documentRowId: rowId,
                                        notes: $"Compensating movement: document row deleted",
                                        currentUser: currentUser,
                                        movementDate: documentDateUtc,
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await stockMovementService.ProcessInboundMovementAsync(
                                        productId: existingMovement.ProductId,
                                        toLocationId: existingMovement.FromLocationId ?? storageLocation.Id,
                                        quantity: existingMovement.Quantity,
                                        documentHeaderId: row.DocumentHeader.Id,
                                        documentRowId: rowId,
                                        notes: $"Compensating movement: document row deleted",
                                        currentUser: currentUser,
                                        movementDate: documentDateUtc,
                                        cancellationToken: cancellationToken);
                                }

                                logger.LogInformation("Created compensating stock movement for deleted row {RowId} in archived document (movement {MovementId}).", rowId, existingMovement.Id);
                            }
                            catch (Exception ex)
                            {
                                // Log the movement failure but do not re-throw; the row deletion will still proceed.
                                logger.LogError(ex, "Failed to create compensating stock movement for deleted row {RowId} (movement {MovementId}); deletion will still proceed.", rowId, existingMovement.Id);
                            }
                        }
                    }
                }
            }
        }
        // If document type uses live "MovesStockOnRowChange" mode, soft-delete the movement directly
        else if (row.DocumentHeader is not null &&
                 (row.DocumentHeader.DocumentType?.MovesStockOnRowChange ?? false) &&
                 row.ProductId.HasValue)
        {
            try
            {
                await stockMovementService.DeleteMovementsForRowAsync(rowId, currentUser, cancellationToken);
                logger.LogInformation("Soft-deleted stock movements for live-mode row {RowId} being deleted.", rowId);
            }
            catch (Exception ex)
            {
                // Log the movement failure but do not re-throw; the row deletion will still proceed.
                logger.LogError(ex, "Failed to soft-delete stock movements for live-mode row {RowId}; deletion will still proceed.", rowId);
            }
        }

        // Soft delete
        row.IsDeleted = true;
        row.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(row, "Delete", currentUser, null, cancellationToken);

        logger.LogInformation("Document row {RowId} deleted.", rowId);

        return true;
    }

}
