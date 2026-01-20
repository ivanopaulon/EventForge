namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Represents the status of stock based on reorder and safety stock levels.
    /// </summary>
    public enum StockStatus
    {
        /// <summary>
        /// Stock level is above the reorder point.
        /// </summary>
        OK,

        /// <summary>
        /// Stock level is below the reorder point but above safety stock.
        /// </summary>
        LowStock,

        /// <summary>
        /// Stock level is below the safety stock level.
        /// </summary>
        Critical,

        /// <summary>
        /// Stock quantity is zero.
        /// </summary>
        OutOfStock
    }
}
