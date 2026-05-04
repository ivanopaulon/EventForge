namespace Prym.DTOs.Warehouse;

/// <summary>
/// Represents a stock snapshot for a product/location/lot at a specific reference date.
/// Quantities are reconstructed by replaying all stock movements up to the reference date.
/// Prices reflect the purchase cost at that date and the current sale price.
/// </summary>
public class StockSnapshotDto
{
    /// <summary>Product unique identifier.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Product code.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Warehouse unique identifier.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>Warehouse name.</summary>
    public string WarehouseName { get; set; } = string.Empty;

    /// <summary>Warehouse code.</summary>
    public string WarehouseCode { get; set; } = string.Empty;

    /// <summary>Storage location unique identifier.</summary>
    public Guid LocationId { get; set; }

    /// <summary>Storage location code.</summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>Storage location description.</summary>
    public string? LocationDescription { get; set; }

    /// <summary>Lot unique identifier (if applicable).</summary>
    public Guid? LotId { get; set; }

    /// <summary>Lot code (if applicable).</summary>
    public string? LotCode { get; set; }

    /// <summary>Lot expiry date (if applicable).</summary>
    public DateTime? LotExpiry { get; set; }

    /// <summary>
    /// Quantity reconstructed from stock movements up to ReferenceDate.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Purchase unit cost at the reference date.
    /// Taken from the last inbound movement's UnitCost up to ReferenceDate,
    /// or from the current Stock.UnitCost as fallback.
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Current sale price from Product.DefaultPrice.
    /// Note: historical sale prices are not tracked without a time-aware price list.
    /// </summary>
    public decimal? DefaultPrice { get; set; }

    /// <summary>Total purchase value = Quantity * UnitCost.</summary>
    public decimal? TotalCostValue => Quantity > 0 && UnitCost.HasValue ? Quantity * UnitCost.Value : null;

    /// <summary>Total sale value = Quantity * DefaultPrice.</summary>
    public decimal? TotalSaleValue => Quantity > 0 && DefaultPrice.HasValue ? Quantity * DefaultPrice.Value : null;

    /// <summary>Reference date for this snapshot.</summary>
    public DateTime ReferenceDate { get; set; }
}
