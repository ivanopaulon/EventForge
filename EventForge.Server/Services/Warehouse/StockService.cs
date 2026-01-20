using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock levels and inventory operations.
/// </summary>
public class StockService : IStockService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StockService> _logger;

    public StockService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<StockService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StockDto>> GetStockAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Stocks
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock with filters - ProductId: {ProductId}, LocationId: {LocationId}, LotId: {LotId}",
                productId, locationId, lotId);
            throw;
        }
    }

    public async Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .FirstOrDefaultAsync(s => s.ProductId == id && s.TenantId == currentTenantId.Value, cancellationToken);

            return stock?.ToStockDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock by ID: {StockId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.StorageLocation!.Code)
                .ToListAsync(cancellationToken);

            return stocks.Select(s => s.ToStockDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock by product ID: {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<StockDto>> GetStockByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stocks = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .Where(s => s.StorageLocationId == locationId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.Product!.Name)
                .ToListAsync(cancellationToken);

            return stocks.Select(s => s.ToStockDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock by location ID: {LocationId}", locationId);
            throw;
        }
    }

    public async Task<decimal> GetAvailableQuantityAsync(Guid productId, Guid? lotId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Stocks
                .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value);

            if (lotId.HasValue)
            {
                query = query.Where(s => s.LotId == lotId.Value);
            }

            return await query.SumAsync(s => s.AvailableQuantity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available quantity for product: {ProductId}, lot: {LotId}", productId, lotId);
            throw;
        }
    }

    public async Task<decimal> GetAvailableQuantityAtLocationAsync(Guid productId, Guid locationId, Guid? lotId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Stocks
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available quantity at location - Product: {ProductId}, Location: {LocationId}, Lot: {LotId}",
                productId, locationId, lotId);
            throw;
        }
    }

    public async Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Check if stock entry already exists
            var existingStock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == createDto.ProductId &&
                                         s.StorageLocationId == createDto.StorageLocationId &&
                                         s.LotId == createDto.LotId &&
                                         s.TenantId == currentTenantId.Value, cancellationToken);

            if (existingStock != null)
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

                _ = await _auditLogService.LogEntityChangeAsync("Stock", existingStock.Id, "Updated", "Update", null,
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

                _ = _context.Stocks.Add(newStock);
                existingStock = newStock;

                _ = await _auditLogService.LogEntityChangeAsync("Stock", newStock.Id, "Created", "Create", null,
                    $"Created stock for product {createDto.ProductId} at location {createDto.StorageLocationId}", currentUser);
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Reload with includes for DTO mapping
            var stockForDto = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .FirstAsync(s => s.Id == existingStock.Id, cancellationToken);

            return stockForDto.ToStockDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating stock for product: {ProductId} at location: {LocationId}",
                createDto.ProductId, createDto.StorageLocationId);
            throw;
        }
    }

    public async Task<StockDto?> UpdateStockLevelsAsync(Guid id, UpdateStockDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Include(s => s.Lot)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock == null)
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

            _ = await _auditLogService.LogEntityChangeAsync("Stock", stock.Id, "StockLevels", "Update", null, "Updated stock levels", currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return stock.ToStockDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock levels for ID: {StockId}", id);
            throw;
        }
    }

    public async Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId &&
                                         s.StorageLocationId == locationId &&
                                         s.LotId == lotId &&
                                         s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock == null || stock.AvailableQuantity < quantity)
            {
                return false; // Insufficient stock available
            }

            stock.ReservedQuantity += quantity;
            stock.ModifiedBy = currentUser;
            stock.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Reserved", "Reserve", null,
                $"Reserved {quantity} units", currentUser ?? "System");
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock - Product: {ProductId}, Location: {LocationId}, Quantity: {Quantity}",
                productId, locationId, quantity);
            throw;
        }
    }

    public async Task<bool> ReleaseReservedStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId &&
                                         s.StorageLocationId == locationId &&
                                         s.LotId == lotId &&
                                         s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock == null || stock.ReservedQuantity < quantity)
            {
                return false; // Not enough reserved quantity
            }

            stock.ReservedQuantity -= quantity;
            stock.ModifiedBy = currentUser;
            stock.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Released", "Release", null,
                $"Released {quantity} reserved units", currentUser ?? "System");
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reserved stock - Product: {ProductId}, Location: {LocationId}, Quantity: {Quantity}",
                productId, locationId, quantity);
            throw;
        }
    }

    public async Task<IEnumerable<StockDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stocks = await _context.Stocks
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock entries");
            throw;
        }
    }

    public async Task<IEnumerable<StockDto>> GetOverstockAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stocks = await _context.Stocks
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overstock entries");
            throw;
        }
    }

    public async Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == stockId && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock != null)
            {
                stock.LastInventoryDate = inventoryDate;
                stock.ModifiedAt = DateTime.UtcNow;
                _ = await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last inventory date for stock: {StockId}", stockId);
            throw;
        }
    }

    public async Task<bool> DeleteStockAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock == null)
            {
                return false;
            }

            _ = _context.Stocks.Remove(stock);
            _ = await _auditLogService.LogEntityChangeAsync("Stock", stock.Id, "Deleted", "Delete", null, "Deleted stock entry", currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stock: {StockId}", id);
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
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            IQueryable<StockLocationDetail> query;

            if (showAllProducts.HasValue && showAllProducts.Value)
            {
                // LEFT JOIN: Show all products + stock (if present)
                // We use a different approach: query products and optionally include their stock data
                query = from p in _context.Products
                            .Where(p => p.TenantId == currentTenantId.Value && p.IsActive)
                        from s in _context.Stocks
                            .Include(s => s.StorageLocation)
                                .ThenInclude(sl => sl!.Warehouse)
                            .Include(s => s.Lot)
                            .Where(s => s.ProductId == p.Id && s.TenantId == currentTenantId.Value && s.IsActive)
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
                var stockQuery = _context.Stocks
                    .Include(s => s.Product)
                    .Include(s => s.StorageLocation)
                        .ThenInclude(sl => sl!.Warehouse)
                    .Include(s => s.Lot)
                    .Where(s => s.TenantId == currentTenantId.Value && s.IsActive);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock overview");
            throw;
        }
    }

    public async Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var stock = await _context.Stocks
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == dto.StockId && s.TenantId == currentTenantId.Value, cancellationToken);

            if (stock == null)
            {
                _logger.LogWarning("Stock entry not found: {StockId}", dto.StockId);
                return null;
            }

            var previousQuantity = stock.Quantity;
            var difference = dto.NewQuantity - previousQuantity;

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

            _context.StockMovements.Add(movement);

            // Create audit log entry if required
            if (dto.RequiresAudit)
            {
                await _auditLogService.LogEntityChangeAsync(
                    "Stock",
                    stock.Id,
                    "Quantity",
                    "Adjust",
                    previousQuantity.ToString(),
                    dto.NewQuantity.ToString(),
                    currentUser,
                    dto.Notes);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Stock adjusted: Product {ProductId}, Location {LocationId}, {PreviousQty} → {NewQty}, Reason: {Reason}",
                stock.ProductId, stock.StorageLocationId, previousQuantity, dto.NewQuantity, dto.Reason);

            return stock.ToStockDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock: {StockId}", dto.StockId);
            throw;
        }
    }
}