using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
    public async Task<PagedResult<StockLocationDetail>> GetStockOverviewAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null,
        bool? criticalStock = null,
        bool? outOfStock = null,
        bool? inStockOnly = null,
        bool? showAllProducts = null,
        bool detailedView = false,
        CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        IQueryable<StockLocationDetail> query;

        if (showAllProducts.HasValue && showAllProducts.Value)
        {
            // LEFT JOIN: Show all products + stock (if present)
            // We use a different approach: query products and optionally include their stock data
            query = from p in context.Products
                        .AsNoTracking()
                        .Where(p => p.TenantId == currentTenantId.Value && p.IsActive)
                    from s in context.Stocks
                        .Include(s => s.StorageLocation)
                            .ThenInclude(sl => sl!.Warehouse)
                        .Include(s => s.Lot)
                        .Where(s => s.ProductId == p.Id && s.TenantId == currentTenantId.Value)
                        .DefaultIfEmpty()
                    select new StockLocationDetail
                    {
                        StockId = s != null ? s.Id : Guid.Empty,
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        WarehouseId = s != null && s.StorageLocation != null && s.StorageLocation.Warehouse != null ? s.StorageLocation.Warehouse.Id : Guid.Empty,
                        WarehouseName = s != null && s.StorageLocation != null && s.StorageLocation.Warehouse != null ? s.StorageLocation.Warehouse.Name : "N/A",
                        WarehouseCode = s != null && s.StorageLocation != null && s.StorageLocation.Warehouse != null ? s.StorageLocation.Warehouse.Code : string.Empty,
                        LocationId = s != null && s.StorageLocation != null ? s.StorageLocation.Id : Guid.Empty,
                        LocationCode = s != null && s.StorageLocation != null ? s.StorageLocation.Code : "N/A",
                        LocationDescription = s != null && s.StorageLocation != null ? s.StorageLocation.Description : null,
                        LotId = s != null ? s.LotId : null,
                        LotCode = s != null && s.Lot != null ? s.Lot.Code : null,
                        LotExpiry = s != null && s.Lot != null ? s.Lot.ExpiryDate : null,
                        Quantity = s != null ? s.Quantity : 0,
                        Reserved = s != null ? s.ReservedQuantity : 0,
                        LastMovementDate = s != null ? s.LastMovementDate : null,
                        ReorderPoint = s != null && s.ReorderPoint.HasValue ? s.ReorderPoint : p.ReorderPoint,
                        SafetyStock = s != null && s.MinimumLevel.HasValue ? s.MinimumLevel : p.SafetyStock
                    };
        }
        else
        {
            // EXISTING QUERY: Only products with stock
            var stockQuery = context.Stocks
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .Where(s => s.TenantId == currentTenantId.Value);

            query = stockQuery.Select(s => new StockLocationDetail
            {
                StockId = s.Id,
                ProductId = s.ProductId,
                ProductCode = s.Product!.Code,
                ProductName = s.Product.Name,
                WarehouseId = s.StorageLocation!.WarehouseId,
                WarehouseName = s.StorageLocation.Warehouse!.Name,
                WarehouseCode = s.StorageLocation.Warehouse.Code,
                LocationId = s.StorageLocationId,
                LocationCode = s.StorageLocation.Code,
                LocationDescription = s.StorageLocation.Description,
                LotId = s.LotId,
                LotCode = s.Lot != null ? s.Lot.Code : null,
                LotExpiry = s.Lot != null ? s.Lot.ExpiryDate : null,
                Quantity = s.Quantity,
                Reserved = s.ReservedQuantity,
                LastMovementDate = s.LastMovementDate,
                ReorderPoint = s.ReorderPoint,
                SafetyStock = s.MinimumLevel
            });
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s =>
                s.ProductName.ToLower().Contains(searchLower) ||
                s.ProductCode.ToLower().Contains(searchLower));
        }

        // Apply warehouse filter
        // When showAllProducts is enabled, only filter products with stock to avoid excluding products without stock entries
        if (warehouseId.HasValue)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || s.WarehouseId == warehouseId.Value);
            }
            else
            {
                query = query.Where(s => s.WarehouseId == warehouseId.Value);
            }
        }

        // Apply location filter
        // When showAllProducts is enabled, only filter products with stock to avoid excluding products without stock entries
        if (locationId.HasValue)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || s.LocationId == locationId.Value);
            }
            else
            {
                query = query.Where(s => s.LocationId == locationId.Value);
            }
        }

        // Apply lot filter
        // When showAllProducts is enabled, only filter products with stock to avoid excluding products without stock entries
        if (lotId.HasValue)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || s.LotId == lotId.Value);
            }
            else
            {
                query = query.Where(s => s.LotId == lotId.Value);
            }
        }

        // Apply stock status filters
        // When showAllProducts is enabled, only apply filters to products with stock
        if (lowStock.HasValue && lowStock.Value)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || (s.ReorderPoint.HasValue && s.Quantity <= s.ReorderPoint.Value));
            }
            else
            {
                query = query.Where(s => s.ReorderPoint.HasValue && s.Quantity <= s.ReorderPoint.Value);
            }
        }

        if (criticalStock.HasValue && criticalStock.Value)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || (s.SafetyStock.HasValue && s.Quantity <= s.SafetyStock.Value));
            }
            else
            {
                query = query.Where(s => s.SafetyStock.HasValue && s.Quantity <= s.SafetyStock.Value);
            }
        }

        if (outOfStock.HasValue && outOfStock.Value)
        {
            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                query = query.Where(s => s.StockId == Guid.Empty || s.Quantity == 0);
            }
            else
            {
                query = query.Where(s => s.Quantity == 0);
            }
        }

        if (inStockOnly.HasValue && inStockOnly.Value)
        {
            // When showAllProducts is enabled and inStockOnly is true, exclude products without stock entries
            query = query.Where(s => s.StockId != Guid.Empty && s.Quantity > 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ProductCode)
            .ThenBy(s => s.LocationCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLocationDetail>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

}
