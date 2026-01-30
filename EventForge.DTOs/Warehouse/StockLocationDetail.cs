namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Detailed stock information for a specific location.
    /// Used in the detailed view of the stock overview.
    /// </summary>
    public class StockLocationDetail
    {
        public Guid StockId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public Guid LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string? LocationDescription { get; set; }
        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }
        public DateTime? LotExpiry { get; set; }
        public decimal Quantity { get; set; }
        public decimal Reserved { get; set; }
        public decimal Available => Quantity - Reserved;
        public DateTime? LastMovementDate { get; set; }
        public decimal? ReorderPoint { get; set; }
        public decimal? SafetyStock { get; set; }
    }
}
