using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<IEnumerable<StockDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stocks = await context.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(s => s.Lot)
            .Where(s => s.TenantId == currentTenantId.Value &&
                       s.MinimumLevel.HasValue &&
                       s.AvailableQuantity <= s.MinimumLevel.Value)
            .OrderBy(s => s.Product!.Name)
            .ToListAsync(cancellationToken);

        return stocks.Select(s => s.ToStockDto());
    }

    public async Task<IEnumerable<StockDto>> GetOverstockAsync(CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stocks = await context.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(s => s.Lot)
            .Where(s => s.TenantId == currentTenantId.Value &&
                       s.MaximumLevel.HasValue &&
                       s.Quantity > s.MaximumLevel.Value)
            .OrderBy(s => s.Product!.Name)
            .ToListAsync(cancellationToken);

        return stocks.Select(s => s.ToStockDto());
    }

    public async Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stock = await context.Stocks
            .FirstOrDefaultAsync(s => s.Id == stockId && s.TenantId == currentTenantId.Value, cancellationToken);

        if (stock is not null)
        {
            stock.LastInventoryDate = inventoryDate;
            stock.ModifiedAt = DateTime.UtcNow;
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> DeleteStockAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock is null)
            {
                return false;
            }

            _ = context.Stocks.Remove(stock);
            _ = await auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Deleted", "Delete", null, "Deleted stock entry", currentUser);

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Stock {StockId}.", id);
                throw new InvalidOperationException("La giacenza è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

}
