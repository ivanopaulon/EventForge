using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<PagedResult<StockDto>> GetStockAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var query = context.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(s => s.Lot)
            .Where(s => s.TenantId == currentTenantId.Value);

        // Apply filters
        if (productId.HasValue)
        {
            query = query.Where(s => s.ProductId == productId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(s => s.StorageLocationId == locationId.Value);
        }

        if (lotId.HasValue)
        {
            query = query.Where(s => s.LotId == lotId.Value);
        }

        if (lowStock.HasValue && lowStock.Value)
        {
            query = query.Where(s => s.MinimumLevel.HasValue && s.AvailableQuantity <= s.MinimumLevel.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var stocks = await query
            .OrderBy(s => s.Product!.Name)
            .ThenBy(s => s.StorageLocation!.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var stockDtos = stocks.Select(s => s.ToStockDto()).ToList();

        return new PagedResult<StockDto>
        {
            Items = stockDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var stock = await context.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(s => s.Lot)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

        return stock?.ToStockDto();
    }

    public async Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
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
            .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value)
            .OrderBy(s => s.StorageLocation!.Code)
            .ToListAsync(cancellationToken);

        return stocks.Select(s => s.ToStockDto());
    }

    public async Task<IEnumerable<StockDto>> GetStockByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
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
            .Where(s => s.StorageLocationId == locationId && s.TenantId == currentTenantId.Value)
            .OrderBy(s => s.Product!.Name)
            .ToListAsync(cancellationToken);

        return stocks.Select(s => s.ToStockDto());
    }

    public async Task<decimal> GetAvailableQuantityAsync(Guid productId, Guid? lotId = null, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var query = context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value);

        if (lotId.HasValue)
        {
            query = query.Where(s => s.LotId == lotId.Value);
        }

        return await query.SumAsync(s => s.AvailableQuantity, cancellationToken);
    }

    public async Task<decimal> GetAvailableQuantityAtLocationAsync(Guid productId, Guid locationId, Guid? lotId = null, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var query = context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == productId &&
                       s.StorageLocationId == locationId &&
                       s.TenantId == currentTenantId.Value);

        if (lotId.HasValue)
        {
            query = query.Where(s => s.LotId == lotId.Value);
        }

        var stock = await query.FirstOrDefaultAsync(cancellationToken);
        return stock?.AvailableQuantity ?? 0;
    }

}
