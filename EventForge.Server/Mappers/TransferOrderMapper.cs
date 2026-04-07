using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for TransferOrder entities and DTOs.
/// </summary>
public static class TransferOrderMapper
{
    /// <summary>
    /// Maps a TransferOrder entity to a TransferOrderDto.
    /// </summary>
    public static TransferOrderDto ToTransferOrderDto(this TransferOrder transferOrder)
    {
        return new TransferOrderDto
        {
            Id = transferOrder.Id,
            Number = transferOrder.Number,
            Series = transferOrder.Series,
            OrderDate = transferOrder.OrderDate,
            SourceWarehouseId = transferOrder.SourceWarehouseId,
            SourceWarehouseName = transferOrder.SourceWarehouse?.Name,
            DestinationWarehouseId = transferOrder.DestinationWarehouseId,
            DestinationWarehouseName = transferOrder.DestinationWarehouse?.Name,
            Status = transferOrder.Status.ToString(),
            ShipmentDate = transferOrder.ShipmentDate,
            ExpectedArrivalDate = transferOrder.ExpectedArrivalDate,
            ActualArrivalDate = transferOrder.ActualArrivalDate,
            Notes = transferOrder.Notes,
            ShippingReference = transferOrder.ShippingReference,
            Rows = transferOrder.Rows?.Select(r => r.ToTransferOrderRowDto()).ToList() ?? new(),
            CreatedAt = transferOrder.CreatedAt,
            CreatedBy = transferOrder.CreatedBy
        };
    }

    /// <summary>
    /// Maps a TransferOrderRow entity to a TransferOrderRowDto.
    /// </summary>
    public static TransferOrderRowDto ToTransferOrderRowDto(this TransferOrderRow row)
    {
        return new TransferOrderRowDto
        {
            Id = row.Id,
            ProductId = row.ProductId,
            ProductName = row.Product?.Name ?? string.Empty,
            ProductCode = row.Product?.Code ?? string.Empty,
            SourceLocationId = row.SourceLocationId,
            SourceLocationCode = row.SourceLocation?.Code ?? string.Empty,
            DestinationLocationId = row.DestinationLocationId,
            DestinationLocationCode = row.DestinationLocation?.Code,
            QuantityOrdered = row.QuantityOrdered,
            QuantityShipped = row.QuantityShipped,
            QuantityReceived = row.QuantityReceived,
            LotId = row.LotId,
            LotCode = row.Lot?.Code,
            Notes = row.Notes
        };
    }

    /// <summary>
    /// Maps a CreateTransferOrderDto to a TransferOrder entity.
    /// </summary>
    public static TransferOrder ToEntity(this CreateTransferOrderDto createDto, Guid tenantId, string createdBy, string generatedNumber)
    {
        return new TransferOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Number = generatedNumber,
            Series = createDto.Series,
            OrderDate = createDto.OrderDate,
            SourceWarehouseId = createDto.SourceWarehouseId,
            DestinationWarehouseId = createDto.DestinationWarehouseId,
            Status = TransferOrderStatus.Pending,
            Notes = createDto.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Maps a CreateTransferOrderRowDto to a TransferOrderRow entity.
    /// </summary>
    public static TransferOrderRow ToEntity(this CreateTransferOrderRowDto createDto, Guid tenantId, Guid transferOrderId, string createdBy)
    {
        return new TransferOrderRow
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TransferOrderId = transferOrderId,
            ProductId = createDto.ProductId,
            SourceLocationId = createDto.SourceLocationId,
            QuantityOrdered = createDto.Quantity,
            QuantityShipped = 0,
            QuantityReceived = 0,
            LotId = createDto.LotId,
            Notes = createDto.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
