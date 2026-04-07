namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Lightweight DTO for the inventory merge wizard — header only, no rows loaded.
    /// </summary>
    public class InventoryDocumentHeaderDto
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public DateTime InventoryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? WarehouseName { get; set; }
        /// <summary>Row count calculated via SQL COUNT(*) — no rows loaded in memory.</summary>
        public int RowCount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}
