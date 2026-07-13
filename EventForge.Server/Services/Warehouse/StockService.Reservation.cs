using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stock = await context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId &&
                                     s.StorageLocationId == locationId &&
                                     s.LotId == lotId &&
                                     s.TenantId == currentTenantId.Value, cancellationToken);

        if (stock is null || stock.AvailableQuantity < quantity)
        {
            return false; // Insufficient stock available
        }

        stock.ReservedQuantity += quantity;
        stock.ModifiedBy = currentUser;
        stock.ModifiedAt = DateTime.UtcNow;

        _ = await auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Reserved", "Reserve", null,
            $"Reserved {quantity} units", currentUser ?? "System");
        _ = await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ReleaseReservedStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stock = await context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId &&
                                     s.StorageLocationId == locationId &&
                                     s.LotId == lotId &&
                                     s.TenantId == currentTenantId.Value, cancellationToken);

        if (stock is null || stock.ReservedQuantity < quantity)
        {
            return false; // Not enough reserved quantity
        }

        stock.ReservedQuantity -= quantity;
        stock.ModifiedBy = currentUser;
        stock.ModifiedAt = DateTime.UtcNow;

        _ = await auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Released", "Release", null,
            $"Released {quantity} reserved units", currentUser ?? "System");
        _ = await context.SaveChangesAsync(cancellationToken);

        return true;
    }

}
