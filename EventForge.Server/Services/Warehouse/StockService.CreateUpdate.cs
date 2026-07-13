using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Services.Warehouse;

public partial class StockService
{
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

}
