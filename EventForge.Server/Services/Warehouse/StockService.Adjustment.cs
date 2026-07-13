using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stock = await context.Stocks
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .FirstOrDefaultAsync(s => s.Id == dto.StockId && s.TenantId == currentTenantId.Value, cancellationToken);

        if (stock is null)
        {
            logger.LogWarning("Stock entry not found: {StockId}", dto.StockId);
            return null;
        }

        var previousQuantity = stock.Quantity;
        var difference = dto.NewQuantity - previousQuantity;

        // Guard: no-op when quantity is unchanged — skip movement creation
        if (difference == 0)
        {
            logger.LogDebug("AdjustStockAsync: NewQuantity equals PreviousQuantity ({Qty}) for Stock {StockId} — skipping.", dto.NewQuantity, dto.StockId);
            return stock.ToStockDto();
        }

        // Update stock quantity
        stock.Quantity = dto.NewQuantity;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedBy = currentUser;

        // Create stock movement record
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId.Value,
            ProductId = stock.ProductId,
            // For adjustments, use ToLocation for increases, FromLocation for decreases
            FromLocationId = difference < 0 ? stock.StorageLocationId : null,
            ToLocationId = difference >= 0 ? stock.StorageLocationId : null,
            LotId = stock.LotId,
            Quantity = Math.Abs(difference),
            MovementType = StockMovementType.Adjustment,
            Reason = StockMovementReason.Adjustment,
            MovementDate = DateTime.UtcNow,
            Notes = dto.Notes ?? $"Stock adjustment: {dto.Reason}. Previous: {previousQuantity}, New: {dto.NewQuantity}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            IsActive = true
        };

        context.StockMovements.Add(movement);

        // Create audit log entry if required
        if (dto.RequiresAudit)
        {
            await auditLogService.LogEntityChangeAsync(
                "Stock",
                stock.Id,
                "Quantity",
                "Adjust",
                previousQuantity.ToString(),
                dto.NewQuantity.ToString(),
                currentUser,
                dto.Notes);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stock adjusted: Product {ProductId}, Location {LocationId}, {PreviousQty} → {NewQty}, Reason: {Reason}",
            stock.ProductId, stock.StorageLocationId, previousQuantity, dto.NewQuantity, dto.Reason);

        return stock.ToStockDto();
    }

    /// <inheritdoc />
}
