using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for StockMovement entities and DTOs.
/// </summary>
public static class StockMovementMapper
{
    /// <summary>
    /// Maps a StockMovement entity to a StockMovementDto.
    /// </summary>
    public static StockMovementDto ToStockMovementDto(this StockMovement stockMovement)
    {
        return new StockMovementDto
        {
            Id = stockMovement.Id,
            TenantId = stockMovement.TenantId,
            MovementType = stockMovement.MovementType.ToString(),
            ProductId = stockMovement.ProductId,
            ProductName = stockMovement.Product?.Name,
            ProductCode = stockMovement.Product?.Code,
            LotId = stockMovement.LotId,
            LotCode = stockMovement.Lot?.Code,
            SerialId = stockMovement.SerialId,
            SerialNumber = stockMovement.Serial?.SerialNumber,
            FromLocationId = stockMovement.FromLocationId,
            FromLocationCode = stockMovement.FromLocation?.Code,
            FromWarehouseName = stockMovement.FromLocation?.Warehouse?.Name,
            ToLocationId = stockMovement.ToLocationId,
            ToLocationCode = stockMovement.ToLocation?.Code,
            ToWarehouseName = stockMovement.ToLocation?.Warehouse?.Name,
            Quantity = stockMovement.Quantity,
            UnitCost = stockMovement.UnitCost,
            MovementDate = stockMovement.MovementDate,
            DocumentHeaderId = stockMovement.DocumentHeaderId,
            DocumentReference = stockMovement.DocumentHeader?.Number,
            DocumentRowId = stockMovement.DocumentRowId,
            Reason = stockMovement.Reason.ToString(),
            Status = stockMovement.Status.ToString(),
            Notes = stockMovement.Notes,
            UserId = stockMovement.UserId,
            Reference = stockMovement.Reference,
            MovementPlanId = stockMovement.MovementPlanId,
            CreatedAt = stockMovement.CreatedAt,
            CreatedBy = stockMovement.CreatedBy
        };
    }

    /// <summary>
    /// Maps a CreateStockMovementDto to a StockMovement entity.
    /// </summary>
    public static StockMovement ToEntity(this CreateStockMovementDto createDto, Guid tenantId, string createdBy)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementType = Enum.Parse<StockMovementType>(createDto.MovementType),
            ProductId = createDto.ProductId,
            LotId = createDto.LotId,
            SerialId = createDto.SerialId,
            FromLocationId = createDto.FromLocationId,
            ToLocationId = createDto.ToLocationId,
            Quantity = createDto.Quantity,
            UnitCost = createDto.UnitCost,
            MovementDate = createDto.MovementDate,
            DocumentHeaderId = createDto.DocumentHeaderId,
            DocumentRowId = createDto.DocumentRowId,
            Reason = Enum.Parse<StockMovementReason>(createDto.Reason),
            Notes = createDto.Notes,
            UserId = createDto.UserId,
            Reference = createDto.Reference,
            MovementPlanId = createDto.MovementPlanId,
            Status = MovementStatus.Completed,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}