using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Stock entities and DTOs.
/// </summary>
public static class StockMapper
{
    /// <summary>
    /// Maps a Stock entity to a StockDto.
    /// </summary>
    public static StockDto ToStockDto(this Stock stock)
    {
        return new StockDto
        {
            Id = stock.Id,
            TenantId = stock.TenantId,
            ProductId = stock.ProductId,
            ProductName = stock.Product?.Name,
            ProductCode = stock.Product?.Code,
            StorageLocationId = stock.StorageLocationId,
            StorageLocationCode = stock.StorageLocation?.Code,
            WarehouseName = stock.StorageLocation?.Warehouse?.Name,
            LotId = stock.LotId,
            LotCode = stock.Lot?.Code,
            Quantity = stock.Quantity,
            ReservedQuantity = stock.ReservedQuantity,
            MinimumLevel = stock.MinimumLevel,
            MaximumLevel = stock.MaximumLevel,
            ReorderPoint = stock.ReorderPoint,
            ReorderQuantity = stock.ReorderQuantity,
            LastMovementDate = stock.LastMovementDate,
            UnitCost = stock.UnitCost,
            LastInventoryDate = stock.LastInventoryDate,
            Notes = stock.Notes,
            CreatedAt = stock.CreatedAt,
            CreatedBy = stock.CreatedBy,
            ModifiedAt = stock.ModifiedAt,
            ModifiedBy = stock.ModifiedBy,
            IsActive = stock.IsActive
        };
    }

    /// <summary>
    /// Maps a CreateStockDto to a Stock entity.
    /// </summary>
    public static Stock ToEntity(this CreateStockDto createDto, Guid tenantId, string createdBy)
    {
        return new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
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
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates a Stock entity from an UpdateStockDto.
    /// </summary>
    public static void UpdateFromDto(this Stock stock, UpdateStockDto updateDto, string modifiedBy)
    {
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

        stock.ModifiedBy = modifiedBy;
        stock.ModifiedAt = DateTime.UtcNow;
    }
}