namespace EventForge.DTOs.Warehouse;

/// <summary>
/// DTO for transfer order data.
/// </summary>
public class TransferOrderDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? Series { get; set; }
    public DateTime OrderDate { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ShipmentDate { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ActualArrivalDate { get; set; }
    public string? Notes { get; set; }
    public string? ShippingReference { get; set; }
    public List<TransferOrderRowDto> Rows { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO for transfer order row data.
/// </summary>
public class TransferOrderRowDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public Guid SourceLocationId { get; set; }
    public string SourceLocationCode { get; set; } = string.Empty;
    public Guid? DestinationLocationId { get; set; }
    public string? DestinationLocationCode { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal QuantityReceived { get; set; }
    public Guid? LotId { get; set; }
    public string? LotCode { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a new transfer order.
/// </summary>
public class CreateTransferOrderDto
{
    public string? Number { get; set; }
    public string? Series { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public Guid SourceWarehouseId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public string? Notes { get; set; }
    public List<CreateTransferOrderRowDto> Rows { get; set; } = new();
}

/// <summary>
/// DTO for creating a transfer order row.
/// </summary>
public class CreateTransferOrderRowDto
{
    public Guid ProductId { get; set; }
    public Guid SourceLocationId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? LotId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for shipping a transfer order.
/// </summary>
public class ShipTransferOrderDto
{
    public DateTime ShipmentDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedArrivalDate { get; set; }
    public string? ShippingReference { get; set; }
}

/// <summary>
/// DTO for receiving a transfer order.
/// </summary>
public class ReceiveTransferOrderDto
{
    public DateTime ActualArrivalDate { get; set; } = DateTime.UtcNow;
    public List<ReceiveTransferOrderRowDto> Rows { get; set; } = new();
}

/// <summary>
/// DTO for receiving a transfer order row.
/// </summary>
public class ReceiveTransferOrderRowDto
{
    public Guid RowId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public decimal QuantityReceived { get; set; }
}
