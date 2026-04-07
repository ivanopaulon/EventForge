namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Represents a source of stock movement for reconciliation tracking
    /// </summary>
    public class StockMovementSourceDto
    {
        /// <summary>
        /// Type of movement source: "Document", "Inventory", "Manual"
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Reference identifier (e.g., document number, inventory code)
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the movement (positive for increase, negative for decrease)
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Date of the movement
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// True for inventory movements that replace the stock quantity entirely
        /// </summary>
        public bool IsReplacement { get; set; }
    }
}
