using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock levels and inventory operations.
/// </summary>
public class StockService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<StockService> logger) : IStockService
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

    public async Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        // Validate GUID is not empty BEFORE database query
        if (createDto.ProductId == Guid.Empty)
        {
            logger.LogWarning("CreateOrUpdateStock called with empty ProductId");
            throw new ArgumentException("ProductId non valido. Seleziona un prodotto valido.");
        }

        if (createDto.StorageLocationId == Guid.Empty)
        {
            logger.LogWarning("CreateOrUpdateStock called with empty StorageLocationId");
            throw new ArgumentException("Storage Location non valida. Seleziona una posizione di magazzino valida.");
        }

        // Validate Product exists
        var productExists = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == createDto.ProductId &&
                          p.TenantId == currentTenantId.Value &&
                          !p.IsDeleted,
                      cancellationToken);

        if (!productExists)
        {
            logger.LogWarning("Product with ID {ProductId} not found for tenant {TenantId}.",
                createDto.ProductId, currentTenantId.Value);
            throw new ArgumentException($"Product with ID {createDto.ProductId} not found.");
        }

        // Validate StorageLocation exists
        var locationExists = await context.StorageLocations
            .AsNoTracking()
            .AnyAsync(sl => sl.Id == createDto.StorageLocationId &&
                           sl.TenantId == currentTenantId.Value &&
                           !sl.IsDeleted,
                      cancellationToken);

        if (!locationExists)
        {
            logger.LogWarning("Storage location with ID {StorageLocationId} not found for tenant {TenantId}.",
                createDto.StorageLocationId, currentTenantId.Value);
            throw new ArgumentException($"Storage location with ID {createDto.StorageLocationId} not found.");
        }

        // Validate Lot if provided
        if (createDto.LotId.HasValue)
        {
            if (createDto.LotId.Value == Guid.Empty)
            {
                logger.LogWarning("CreateOrUpdateStock called with empty LotId");
                throw new ArgumentException("Lot ID non valido.");
            }

            var lotExists = await context.Lots
                .AsNoTracking()
                .AnyAsync(l => l.Id == createDto.LotId.Value &&
                              l.TenantId == currentTenantId.Value &&
                              !l.IsDeleted,
                          cancellationToken);

            if (!lotExists)
            {
                logger.LogWarning("Lot with ID {LotId} not found for tenant {TenantId}.",
                    createDto.LotId.Value, currentTenantId.Value);
                throw new ArgumentException($"Lot with ID {createDto.LotId.Value} not found.");
            }
        }

        // Check if stock entry already exists
        var existingStock = await context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == createDto.ProductId &&
                                     s.StorageLocationId == createDto.StorageLocationId &&
                                     s.LotId == createDto.LotId &&
                                     s.TenantId == currentTenantId.Value, cancellationToken);

        if (existingStock is not null)
        {
            // Update existing stock
            existingStock.Quantity = createDto.Quantity;
            existingStock.ReservedQuantity = createDto.ReservedQuantity;
            existingStock.MinimumLevel = createDto.MinimumLevel;
            existingStock.MaximumLevel = createDto.MaximumLevel;
            existingStock.ReorderPoint = createDto.ReorderPoint;
            existingStock.ReorderQuantity = createDto.ReorderQuantity;
            existingStock.UnitCost = createDto.UnitCost;
            existingStock.Notes = createDto.Notes;
            existingStock.ModifiedBy = currentUser;
            existingStock.ModifiedAt = DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync("Stock", existingStock.Id, "Updated", "Update", null,
                $"Updated stock for product {createDto.ProductId} at location {createDto.StorageLocationId}", currentUser);
        }
        else
        {
            // Create new stock entry
            var newStock = new Stock
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                ProductId = createDto.ProductId,
                StorageLocationId = createDto.StorageLocationId,
                LotId = createDto.LotId,
                Quantity = createDto.Quantity,
                ReservedQuantity = createDto.ReservedQuantity,
                MinimumLevel = createDto.MinimumLevel,
                MaximumLevel = createDto.MaximumLevel,
                ReorderPoint = createDto.ReorderPoint,
                ReorderQuantity = createDto.ReorderQuantity,
                UnitCost = createDto.UnitCost,
                Notes = createDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _ = context.Stocks.Add(newStock);
            existingStock = newStock;

            _ = await auditLogService.LogEntityChangeAsync("Stock", newStock.Id, "Created", "Create", null,
                $"Created stock for product {createDto.ProductId} at location {createDto.StorageLocationId}", currentUser);
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        // Reload with includes for DTO mapping
        var stockForDto = await context.Stocks
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(s => s.Lot)
            .FirstAsync(s => s.Id == existingStock.Id, cancellationToken);

        return stockForDto.ToStockDto();
    }

    /// <summary>
    /// Creates or updates stock entry with enhanced validation.
    /// If dto.StockId is provided, updates existing stock (warehouse/location cannot be changed).
    /// If dto.StockId is null/empty, creates new stock entry.
    /// </summary>
    public async Task<StockDto> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, string currentUser, CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        // Case 1: New stock (insertion)
        if (dto.StockId is null || dto.StockId == Guid.Empty)
        {
            // Validate required fields for new stock
            if (!dto.WarehouseId.HasValue || dto.WarehouseId == Guid.Empty)
                throw new ArgumentException("Warehouse is required for new stock");

            if (dto.StorageLocationId == Guid.Empty)
                throw new ArgumentException("Storage location is required for new stock");

            // Verify warehouse exists
            var warehouseExists = await context.StorageFacilities
                .AsNoTracking()
                .AnyAsync(w => w.Id == dto.WarehouseId.Value &&
                              w.TenantId == currentTenantId.Value &&
                              !w.IsDeleted,
                          cancellationToken);
            if (!warehouseExists)
                throw new ArgumentException($"Warehouse {dto.WarehouseId.Value} not found");

            // Verify location exists and belongs to the warehouse
            var location = await context.StorageLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == dto.StorageLocationId &&
                                         l.TenantId == currentTenantId.Value &&
                                         !l.IsDeleted,
                                    cancellationToken);
            if (location is null)
                throw new ArgumentException($"Storage location {dto.StorageLocationId} not found");

            if (location.WarehouseId != dto.WarehouseId.Value)
                throw new ArgumentException("Storage location does not belong to the selected warehouse");

            // Verify product exists
            var productExists = await context.Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == dto.ProductId &&
                              p.TenantId == currentTenantId.Value &&
                              !p.IsDeleted,
                          cancellationToken);
            if (!productExists)
                throw new ArgumentException($"Product {dto.ProductId} not found");

            // Create new stock
            var newStock = new Stock
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                ProductId = dto.ProductId,
                StorageLocationId = dto.StorageLocationId,
                LotId = dto.LotId,
                Quantity = dto.NewQuantity,
                ReservedQuantity = dto.ReservedQuantity,
                MinimumLevel = dto.MinimumLevel,
                MaximumLevel = dto.MaximumLevel,
                ReorderPoint = dto.ReorderPoint,
                ReorderQuantity = dto.ReorderQuantity,
                UnitCost = dto.UnitCost,
                Notes = dto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _ = context.Stocks.Add(newStock);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("Stock", newStock.Id, "Created", "Create", null,
                $"Created stock for product {dto.ProductId} at location {dto.StorageLocationId}", currentUser);

            // Reload with includes for DTO mapping
            var stockForDto = await context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .FirstAsync(s => s.Id == newStock.Id, cancellationToken);

            return stockForDto.ToStockDto();
        }

        // Case 2: Update existing stock
        else
        {
            var existingStock = await context.Stocks
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .FirstOrDefaultAsync(s => s.Id == dto.StockId &&
                                         s.TenantId == currentTenantId.Value,
                                    cancellationToken);

            if (existingStock is null)
                throw new ArgumentException($"Stock {dto.StockId} not found");

            // ❌ BLOCK: Attempt to change warehouse
            if (dto.WarehouseId.HasValue &&
                dto.WarehouseId != Guid.Empty &&
                existingStock.StorageLocation?.WarehouseId is not null &&
                dto.WarehouseId.Value != existingStock.StorageLocation.WarehouseId)
            {
                throw new InvalidOperationException(
                    "Cannot change the warehouse of existing stock. " +
                    "Delete this stock entry and create a new one in the desired warehouse.");
            }

            // ❌ BLOCK: Attempt to change location
            if (dto.StorageLocationId != Guid.Empty &&
                dto.StorageLocationId != existingStock.StorageLocationId)
            {
                throw new InvalidOperationException(
                    "Cannot change the storage location of existing stock. " +
                    "Use a warehouse movement/transfer to move stock between locations.");
            }

            // ✅ ALLOW: Update quantity and other fields
            existingStock.Quantity = dto.NewQuantity;
            existingStock.ReservedQuantity = dto.ReservedQuantity;
            existingStock.MinimumLevel = dto.MinimumLevel;
            existingStock.MaximumLevel = dto.MaximumLevel;
            existingStock.ReorderPoint = dto.ReorderPoint;
            existingStock.ReorderQuantity = dto.ReorderQuantity;
            existingStock.UnitCost = dto.UnitCost;
            existingStock.Notes = dto.Notes;
            existingStock.ModifiedBy = currentUser;
            existingStock.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync("Stock", existingStock.Id, "Updated", "Update", null,
                $"Updated stock quantity to {dto.NewQuantity}", currentUser);

            return existingStock.ToStockDto();
        }
    }

    public async Task<StockDto?> UpdateStockLevelsAsync(Guid id, UpdateStockDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
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
                .Include(s => s.Lot)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock is null)
            {
                return null;
            }

            // Update only provided fields
            if (updateDto.Quantity.HasValue)
                stock.Quantity = updateDto.Quantity.Value;
            if (updateDto.ReservedQuantity.HasValue)
                stock.ReservedQuantity = updateDto.ReservedQuantity.Value;
            if (updateDto.MinimumLevel.HasValue)
                stock.MinimumLevel = updateDto.MinimumLevel.Value;
            if (updateDto.MaximumLevel.HasValue)
                stock.MaximumLevel = updateDto.MaximumLevel.Value;
            if (updateDto.ReorderPoint.HasValue)
                stock.ReorderPoint = updateDto.ReorderPoint.Value;
            if (updateDto.ReorderQuantity.HasValue)
                stock.ReorderQuantity = updateDto.ReorderQuantity.Value;
            if (updateDto.UnitCost.HasValue)
                stock.UnitCost = updateDto.UnitCost.Value;
            if (!string.IsNullOrEmpty(updateDto.Notes))
                stock.Notes = updateDto.Notes;

            stock.ModifiedBy = currentUser;
            stock.ModifiedAt = DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync("Stock", stock.Id, "StockLevels", "Update", null, "Updated stock levels", currentUser);

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Stock {StockId}.", id);
                throw new InvalidOperationException("La giacenza è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            return stock.ToStockDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

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
    public async Task<IEnumerable<StockSnapshotDto>> GetStockSnapshotAsync(
        DateTime referenceDate,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        // Cover the full reference day in UTC: movements on the reference date are included.
        var cutoff = referenceDate.Date.AddDays(1);

        // ── Step 1: Load movements via projection ────────────────────────────
        // Using a scalar projection avoids loading unused navigation-property columns
        // (AuditableEntity fields, Notes, Reference, …) for every movement row.
        // Only Completed movements contribute to historical stock levels.
        // POS sales (SaleSessionService) and document approval (DocumentHeaderService) both
        // create movements directly with MovementStatus.Completed, so the filter is correct.
        // Planned/InProgress/Cancelled/Failed movements represent uncommitted or voided intent
        // and must NOT influence the historical balance.
        var movementsBase = context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == currentTenantId.Value
                         && !sm.IsDeleted
                         && sm.MovementDate < cutoff
                         && sm.Status == MovementStatus.Completed);

        // Push location/warehouse filters to the database to avoid loading irrelevant rows.
        if (locationId.HasValue)
        {
            movementsBase = movementsBase.Where(sm =>
                sm.ToLocationId == locationId.Value || sm.FromLocationId == locationId.Value);
        }
        else if (warehouseId.HasValue)
        {
            var wid = warehouseId.Value;
            // Navigation-property access in WHERE is translated to a JOIN without loading the entity.
            movementsBase = movementsBase.Where(sm =>
                (sm.ToLocation != null && sm.ToLocation.WarehouseId == wid) ||
                (sm.FromLocation != null && sm.FromLocation.WarehouseId == wid));
        }

        var rawMovements = await movementsBase
            .Select(sm => new MovementProjection(
                sm.ProductId,
                sm.ToLocationId,
                sm.FromLocationId,
                sm.LotId,
                sm.Quantity,
                sm.MovementDate,
                sm.UnitCost))
            .ToListAsync(cancellationToken);

        if (rawMovements.Count == 0)
            return Enumerable.Empty<StockSnapshotDto>();

        // ── Step 2: Collect IDs for bulk-loading reference data ──────────────
        var relevantProductIds = rawMovements.Select(m => m.ProductId).Distinct().ToList();
        var relevantLocationIds = rawMovements
            .SelectMany(m => new[] { m.ToLocationId, m.FromLocationId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var relevantLotIds = rawMovements
            .Where(m => m.LotId.HasValue)
            .Select(m => m.LotId!.Value)
            .Distinct()
            .ToList();

        // ── Step 3: Bulk-load Products, Locations, Lots ──────────────────────
        // DbContext is not thread-safe — queries must be sequential.

        var productLookup = await context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenantId.Value
                        && !p.IsDeleted
                        && relevantProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Location lookup with warehouse navigation so we can fill warehouse name/code in the DTO.
        var locationLookup = await context.StorageLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => l.TenantId == currentTenantId.Value
                        && !l.IsDeleted
                        && relevantLocationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Lot lookup is only needed when at least one movement references a lot.
        var lotLookup = relevantLotIds.Count > 0
            ? await context.Lots
                .AsNoTracking()
                .Where(l => l.TenantId == currentTenantId.Value
                            && !l.IsDeleted
                            && relevantLotIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, cancellationToken)
            : new Dictionary<Guid, Data.Entities.Warehouse.Lot>();

        // ── Step 4: Load inventory-document anchors (projected) ───────────────
        // For each (ProductId, LocationId) pair, the most recent closed inventory document
        // whose date is before the cutoff seeds the accumulator with the counted quantity.
        // Only movements dated *after* the inventory document's date are then applied on top.
        // DocumentRow has no LotId column — inventory counting is done at the (Product, Location)
        // level, so one anchor covers all lot-specific buckets within the same location.
        // Using a scalar projection avoids loading full DocumentRow/DocumentHeader/DocumentType entities.
        var inventoryAnchorLookup = (await context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.TenantId == currentTenantId.Value
                         && !dr.IsDeleted
                         && dr.ProductId.HasValue
                         && relevantProductIds.Contains(dr.ProductId!.Value)
                         && dr.LocationId.HasValue
                         && relevantLocationIds.Contains(dr.LocationId!.Value)
                         && dr.DocumentHeader != null
                         && dr.DocumentHeader.DocumentType != null
                         && dr.DocumentHeader.DocumentType.IsInventoryDocument
                         && (dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived)
                         && dr.DocumentHeader.Date < cutoff)
            .Select(dr => new
            {
                ProductId = dr.ProductId!.Value,
                LocationId = dr.LocationId!.Value,
                dr.Quantity,
                DocumentDate = dr.DocumentHeader!.Date
            })
            .ToListAsync(cancellationToken))
            .GroupBy(r => (r.ProductId, r.LocationId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Most recent inventory document for this (Product, Location) pair.
                    var best = g.OrderByDescending(r => r.DocumentDate).First();
                    // Pre-compute the cutoff so the inner loop does not call AddDays(1) per iteration.
                    return (Quantity: best.Quantity, Cutoff: best.DocumentDate.Date.AddDays(1));
                });

        // ── Step 5: Load Stock.UnitCost fallback ─────────────────────────────
        var stockCostLookup = await context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                        && !s.IsDeleted
                        && relevantProductIds.Contains(s.ProductId)
                        && relevantLocationIds.Contains(s.StorageLocationId))
            .Select(s => new { s.ProductId, s.StorageLocationId, s.LotId, s.UnitCost })
            .ToDictionaryAsync(
                s => (s.ProductId, s.StorageLocationId, s.LotId),
                s => s.UnitCost,
                cancellationToken);

        // ── Step 6: Resolve sale prices ──────────────────────────────────────
        // Mirrors the normal pricing cascade (priority 4 of PriceResolutionService) without
        // business-party or document context (irrelevant for a stock valuation snapshot).
        var referenceDateUtc = referenceDate.Date;
        var salePriceLookup = await BuildSalePriceLookupAsync(
            relevantProductIds, referenceDateUtc, currentTenantId.Value, cancellationToken);

        // ── Step 7: Accumulate movements into per-(Product, Location, Lot) buckets ──
        // Inflows  → ToLocationId  (+Quantity, tracks last UnitCost)
        // Outflows → FromLocationId (-Quantity)
        // Convention: StockMovement.Quantity is always stored as a positive value regardless of
        // direction — the sign is implicit from MovementType / FromLocationId / ToLocationId.
        // Math.Abs is applied defensively so that any legacy rows with a negative Quantity
        // (e.g. written by old code) do not silently invert the sign of the accumulator.
        //
        // When an inventory-document anchor exists for a (ProductId, LocationId) pair, the
        // accumulator starts at the anchored quantity instead of zero, and only movements
        // dated strictly after the inventory document's date are applied.
        var groups = new Dictionary<(Guid ProductId, Guid LocationId, Guid? LotId), SnapshotAccumulator>(
            rawMovements.Count / 2);

        foreach (var mv in rawMovements)
        {
            var absQty = Math.Abs(mv.Quantity);

            // Inflow contribution.
            if (mv.ToLocationId.HasValue)
            {
                var key = (mv.ProductId, mv.ToLocationId.Value, mv.LotId);
                if (!groups.TryGetValue(key, out var acc))
                {
                    acc = BuildAccumulatorWithAnchor(key, inventoryAnchorLookup);
                    groups[key] = acc;
                }

                // Skip movements that pre-date (or are on the same day as) the inventory anchor.
                // InventoryAnchorCutoff is pre-computed (anchorDate.Date + 1 day), so no AddDays() here.
                if (acc.InventoryAnchorCutoff.HasValue && mv.MovementDate < acc.InventoryAnchorCutoff.Value)
                    continue;

                acc.Quantity += absQty;
                // Track the most recent inbound UnitCost for the purchase price.
                if (mv.UnitCost.HasValue && mv.MovementDate > acc.LastInboundDate)
                {
                    acc.LastInboundDate = mv.MovementDate;
                    acc.LastInboundUnitCost = mv.UnitCost;
                }
            }

            // Outflow contribution.
            if (mv.FromLocationId.HasValue)
            {
                var key = (mv.ProductId, mv.FromLocationId.Value, mv.LotId);
                if (!groups.TryGetValue(key, out var acc))
                {
                    acc = BuildAccumulatorWithAnchor(key, inventoryAnchorLookup);
                    groups[key] = acc;
                }

                if (acc.InventoryAnchorCutoff.HasValue && mv.MovementDate < acc.InventoryAnchorCutoff.Value)
                    continue;

                acc.Quantity -= absQty;
            }
        }

        // ── Step 8: Build result DTOs ─────────────────────────────────────────
        // Trim the search term once. StringComparison.OrdinalIgnoreCase is used in Contains.
        var effectiveSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var result = new List<StockSnapshotDto>(groups.Count);

        foreach (var (key, acc) in groups)
        {
            if (!productLookup.TryGetValue(key.ProductId, out var product)) continue;
            if (!locationLookup.TryGetValue(key.LocationId, out var loc)) continue;

            var lot = key.LotId.HasValue && lotLookup.TryGetValue(key.LotId.Value, out var l) ? l : null;

            // Apply optional search filter (in-memory, after aggregation).
            if (effectiveSearch is not null &&
                !product.Name.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase) &&
                !product.Code.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Resolve purchase unit cost: last inbound movement's cost → fallback to Stock.UnitCost.
            decimal? unitCost = acc.LastInboundUnitCost;
            if (!unitCost.HasValue)
                stockCostLookup.TryGetValue((key.ProductId, key.LocationId, key.LotId), out unitCost);

            // Resolve sale price: price-list entry (Output, active at referenceDate) → Product.DefaultPrice.
            salePriceLookup.TryGetValue(key.ProductId, out var salePriceEntry);

            result.Add(new StockSnapshotDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                WarehouseId = loc.Warehouse?.Id ?? Guid.Empty,
                WarehouseName = loc.Warehouse?.Name ?? string.Empty,
                WarehouseCode = loc.Warehouse?.Code ?? string.Empty,
                LocationId = loc.Id,
                LocationCode = loc.Code,
                LocationDescription = loc.Description,
                LotId = lot?.Id,
                LotCode = lot?.Code,
                LotExpiry = lot?.ExpiryDate,
                Quantity = acc.Quantity,
                UnitCost = unitCost,
                SalePrice = salePriceEntry?.Price ?? product.DefaultPrice,
                IsPriceFromList = salePriceEntry is not null,
                PriceListName = salePriceEntry?.PriceListName,
                ReferenceDate = referenceDate.Date
            });
        }

        return result
            .OrderBy(s => s.ProductCode)
            .ThenBy(s => s.LocationCode);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InventorySnapshotDateDto>> GetRecentInventoryDatesAsync(
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            return Array.Empty<InventorySnapshotDateDto>();

        try
        {
            // Project only the scalar fields that are needed — this avoids loading full entity
            // columns and, critically, avoids using CLR-only properties such as DateTime.Kind
            // that EF Core cannot translate to SQL against a real (non-InMemory) database.
            var raw = await context.DocumentHeaders
                .AsNoTracking()
                .Where(dh => dh.TenantId == currentTenantId.Value
                             && !dh.IsDeleted
                             && dh.DocumentType != null
                             && dh.DocumentType.IsInventoryDocument
                             && (dh.Status == DocumentStatus.Archived))
                .OrderByDescending(dh => dh.Date)
                .Take(count)
                .Select(dh => new { dh.Id, dh.Date, dh.Number })
                .ToListAsync(cancellationToken);

            // Map to DTO in-memory (safe to use .Date here, outside the LINQ-to-SQL boundary).
            // SpecifyKind ensures the serialized value always carries a UTC timezone offset,
            // matching the documented contract on InventorySnapshotDateDto.Date.
            return raw
                .Select(r => new InventorySnapshotDateDto
                {
                    DocumentHeaderId = r.Id,
                    Date = DateTime.SpecifyKind(r.Date.Date, DateTimeKind.Utc),
                    DocumentNumber = r.Number
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent inventory dates for tenant {TenantId}.", currentTenantId.Value);
            return Array.Empty<InventorySnapshotDateDto>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StockSnapshotDto>> GetInventoryDocumentQuantitiesAsync(
        Guid documentHeaderId,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
            throw new InvalidOperationException("Current tenant ID is not available.");

        // ── Step 1: Validate and load the inventory document header (scalar projection) ──────
        // Only archived inventory documents are authoritative. An active/draft document
        // represents an in-progress count and must not be used for stock valuation.
        var header = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.Id == documentHeaderId
                         && dh.TenantId == currentTenantId.Value
                         && !dh.IsDeleted
                         && dh.DocumentType != null
                         && dh.DocumentType.IsInventoryDocument
                         && (dh.Status == DocumentStatus.Archived))
            .Select(dh => new { dh.Id, dh.Date })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
            return Enumerable.Empty<StockSnapshotDto>();

        var documentDate = header.Date.Date;
        // The cutoff for movement cost look-up: include all inbounds up to end of document day.
        var movementCutoff = documentDate.AddDays(1);

        // ── Step 2: Load DocumentRows via projection ─────────────────────────────────────────
        // DocumentRows only have ProductId and LocationId — they do not track lots. One row
        // per (Product, Location) is the expected structure; if there are duplicates the
        // quantities are summed in-memory (group step below).
        var rowsQuery = context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.DocumentHeaderId == documentHeaderId
                         && dr.TenantId == currentTenantId.Value
                         && !dr.IsDeleted
                         && dr.ProductId.HasValue
                         && dr.LocationId.HasValue);

        // Push location/warehouse filters to the database to avoid loading irrelevant rows.
        if (locationId.HasValue)
        {
            rowsQuery = rowsQuery.Where(dr => dr.LocationId == locationId.Value);
        }
        else if (warehouseId.HasValue)
        {
            var wid = warehouseId.Value;
            rowsQuery = rowsQuery.Where(dr =>
                dr.Location != null && dr.Location.WarehouseId == wid);
        }

        var rawRows = await rowsQuery
            .Select(dr => new
            {
                ProductId = dr.ProductId!.Value,
                LocationId = dr.LocationId!.Value,
                dr.Quantity
            })
            .ToListAsync(cancellationToken);

        if (rawRows.Count == 0)
            return Enumerable.Empty<StockSnapshotDto>();

        // ── Step 3: Group by (ProductId, LocationId) and sum quantities ──────────────────────
        // Handles the rare case of multiple rows per (Product, Location) in one document.
        var grouped = rawRows
            .GroupBy(r => (r.ProductId, r.LocationId))
            .Select(g => (ProductId: g.Key.ProductId, LocationId: g.Key.LocationId, Quantity: g.Sum(r => r.Quantity)))
            .ToList();

        var relevantProductIds = grouped.Select(r => r.ProductId).Distinct().ToList();
        var relevantLocationIds = grouped.Select(r => r.LocationId).Distinct().ToList();

        // ── Step 4: Bulk-load Products and Locations ─────────────────────────────────────────
        var productLookup = await context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenantId.Value
                        && !p.IsDeleted
                        && relevantProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var locationLookup = await context.StorageLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => l.TenantId == currentTenantId.Value
                        && !l.IsDeleted
                        && relevantLocationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // ── Step 5: Resolve purchase unit cost per (Product, Location) ───────────────────────
        // Strategy: most recent Completed Inbound movement up to end of document day.
        // If no inbound has a cost, fall back to the current Stock.UnitCost.
        var lastCostLookup = (await context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == currentTenantId.Value
                         && !sm.IsDeleted
                         && sm.Status == MovementStatus.Completed
                         && sm.ToLocationId.HasValue
                         && relevantProductIds.Contains(sm.ProductId)
                         && relevantLocationIds.Contains(sm.ToLocationId!.Value)
                         && sm.MovementDate < movementCutoff
                         && sm.UnitCost.HasValue)
            .Select(sm => new
            {
                sm.ProductId,
                LocationId = sm.ToLocationId!.Value,
                sm.MovementDate,
                sm.UnitCost
            })
            .ToListAsync(cancellationToken))
            .GroupBy(sm => (sm.ProductId, sm.LocationId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(sm => sm.MovementDate).First().UnitCost);

        var stockCostLookup = await context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                        && !s.IsDeleted
                        && relevantProductIds.Contains(s.ProductId)
                        && relevantLocationIds.Contains(s.StorageLocationId))
            .Select(s => new { s.ProductId, s.StorageLocationId, s.UnitCost })
            .ToDictionaryAsync(
                s => (s.ProductId, s.StorageLocationId),
                s => s.UnitCost,
                cancellationToken);

        // ── Step 6: Resolve sale prices at the document date ─────────────────────────────────
        var salePriceLookup = await BuildSalePriceLookupAsync(
            relevantProductIds, documentDate, currentTenantId.Value, cancellationToken);

        // ── Step 7: Build result DTOs ─────────────────────────────────────────────────────────
        var effectiveSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var result = new List<StockSnapshotDto>(grouped.Count);

        foreach (var (productId, locId, quantity) in grouped)
        {
            if (!productLookup.TryGetValue(productId, out var product)) continue;
            if (!locationLookup.TryGetValue(locId, out var loc)) continue;

            if (effectiveSearch is not null &&
                !product.Name.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase) &&
                !product.Code.Contains(effectiveSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Resolve unit cost: last inbound before document date → fallback to Stock.UnitCost.
            decimal? unitCost = null;
            lastCostLookup.TryGetValue((productId, locId), out unitCost);
            if (!unitCost.HasValue)
                stockCostLookup.TryGetValue((productId, locId), out unitCost);

            salePriceLookup.TryGetValue(productId, out var salePriceEntry);

            result.Add(new StockSnapshotDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                WarehouseId = loc.Warehouse?.Id ?? Guid.Empty,
                WarehouseName = loc.Warehouse?.Name ?? string.Empty,
                WarehouseCode = loc.Warehouse?.Code ?? string.Empty,
                LocationId = loc.Id,
                LocationCode = loc.Code,
                LocationDescription = loc.Description,
                LotId = null,    // inventory documents do not track at lot level
                LotCode = null,
                LotExpiry = null,
                Quantity = quantity,
                UnitCost = unitCost,
                SalePrice = salePriceEntry?.Price ?? product.DefaultPrice,
                IsPriceFromList = salePriceEntry is not null,
                PriceListName = salePriceEntry?.PriceListName,
                ReferenceDate = documentDate
            });
        }

        return result
            .OrderBy(s => s.ProductCode)
            .ThenBy(s => s.LocationCode);
    }

    // ── Private projection type ───────────────────────────────────────────────

    /// <summary>
    /// Lightweight projection of StockMovement data — only the scalar fields needed
    /// for snapshot accumulation. Using this avoids loading navigation-property columns
    /// (AuditableEntity audit fields, Notes, Reference, etc.) for every movement row.
    /// </summary>
    private sealed record MovementProjection(
        Guid ProductId,
        Guid? ToLocationId,
        Guid? FromLocationId,
        Guid? LotId,
        decimal Quantity,
        DateTime MovementDate,
        decimal? UnitCost);

    /// <summary>Accumulates quantity and purchase cost data for a snapshot group.</summary>
    private sealed class SnapshotAccumulator
    {
        /// <summary>Running net quantity (starts at the inventory anchor quantity when an anchor exists).</summary>
        public decimal Quantity { get; set; }

        public decimal? LastInboundUnitCost { get; set; }
        public DateTime LastInboundDate { get; set; }

        /// <summary>
        /// Pre-computed exclusive cutoff date for the inventory anchor:
        /// <c>anchorDate.Date.AddDays(1)</c>.
        /// Movements with <c>MovementDate &lt; InventoryAnchorCutoff</c> are excluded from the
        /// running total (they are already baked into the anchor quantity).
        /// Null when no inventory anchor is available — the full movement history is used.
        /// </summary>
        public DateTime? InventoryAnchorCutoff { get; set; }
    }

    /// <summary>
    /// Creates a <see cref="SnapshotAccumulator"/> pre-seeded with the inventory-document anchor
    /// quantity for the given key, if one exists in <paramref name="anchorLookup"/>.
    /// When no anchor exists the accumulator starts at zero (original behaviour).
    /// </summary>
    /// <remarks>
    /// The anchor lookup is keyed by <c>(ProductId, LocationId)</c> only — LotId is intentionally
    /// ignored because inventory documents record counts at the (Product, Location) granularity
    /// without lot-level tracking. One anchor therefore covers ALL lot-specific buckets within
    /// the same (Product, Location) pair, which is the correct semantic.
    /// </remarks>
    private static SnapshotAccumulator BuildAccumulatorWithAnchor(
        (Guid ProductId, Guid LocationId, Guid? LotId) key,
        Dictionary<(Guid ProductId, Guid LocationId), (decimal Quantity, DateTime Cutoff)> anchorLookup)
    {
        var acc = new SnapshotAccumulator();

        // Look up with null LotId regardless of the movement's actual LotId.
        // Inventory documents do not track at lot level, so one anchor covers all lots.
        if (anchorLookup.TryGetValue((key.ProductId, key.LocationId), out var anchor))
        {
            acc.Quantity = anchor.Quantity;
            acc.InventoryAnchorCutoff = anchor.Cutoff;
        }

        return acc;
    }

    /// <summary>Resolved sale-price entry for a product from a price list.</summary>
    private sealed record SalePriceEntry(decimal Price, string PriceListName);

    /// <summary>
    /// Builds a lookup of resolved sale prices for the given products at the reference date.
    /// <para>
    /// Resolution logic (mirrors PriceResolutionService priority 4 — general active price list):
    /// <list type="number">
    ///   <item>Find active Output price lists valid at <paramref name="referenceDateUtc"/>
    ///         (Status = Active, ValidFrom ≤ date, ValidTo = null or ≥ date), ordered by Priority.</item>
    ///   <item>For each product, pick the entry from the highest-priority list that contains it.</item>
    ///   <item>Products without a price-list entry are absent from the returned dictionary;
    ///         callers fall back to <c>Product.DefaultPrice</c>.</item>
    /// </list>
    /// A single DB query fetches all matching entries at once to avoid N+1 problems.
    /// </para>
    /// </summary>
    private async Task<Dictionary<Guid, SalePriceEntry>> BuildSalePriceLookupAsync(
        IReadOnlyCollection<Guid> productIds,
        DateTime referenceDateUtc,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Load all active Output price-list entries for the relevant products in one query.
        // We include the price list so we can use Priority and Name.
        var entries = await context.PriceListEntries
            .AsNoTracking()
            .Include(e => e.PriceList)
            .Where(e => productIds.Contains(e.ProductId)
                        && e.TenantId == tenantId
                        && !e.IsDeleted
                        && e.Status == Data.Entities.PriceList.PriceListEntryStatus.Active
                        && e.PriceList != null
                        && e.PriceList.TenantId == tenantId
                        && !e.PriceList.IsDeleted
                        && e.PriceList.Status == Data.Entities.PriceList.PriceListStatus.Active
                        && e.PriceList.Direction == PriceListDirection.Output
                        && (e.PriceList.ValidFrom == null || e.PriceList.ValidFrom.Value.Date <= referenceDateUtc)
                        && (e.PriceList.ValidTo == null || e.PriceList.ValidTo.Value.Date >= referenceDateUtc))
            .Select(e => new
            {
                e.ProductId,
                e.Price,
                PriceListName = e.PriceList!.Name,
                Priority = e.PriceList!.Priority
            })
            .ToListAsync(cancellationToken);

        // For each product keep only the entry from the highest-priority price list (lowest Priority value).
        return entries
            .GroupBy(e => e.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var best = g.OrderBy(e => e.Priority).First();
                    return new SalePriceEntry(best.Price, best.PriceListName);
                });
    }

}
