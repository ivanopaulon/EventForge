namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO representing stock trend data for a product over a period.
    /// </summary>
    public class StockTrendDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Year for which the trend data is provided.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// List of data points representing stock levels over time.
        /// </summary>
        public List<StockTrendDataPoint> DataPoints { get; set; } = new List<StockTrendDataPoint>();

        /// <summary>
        /// List of stock increase movements (carichi).
        /// </summary>
        public List<StockMovementPoint> StockIncreases { get; set; } = new List<StockMovementPoint>();

        /// <summary>
        /// List of stock decrease movements (scarichi).
        /// </summary>
        public List<StockMovementPoint> StockDecreases { get; set; } = new List<StockMovementPoint>();

        /// <summary>
        /// Current stock quantity.
        /// </summary>
        public decimal CurrentStock { get; set; }

        /// <summary>
        /// Minimum stock quantity during the period.
        /// </summary>
        public decimal MinStock { get; set; }

        /// <summary>
        /// Maximum stock quantity during the period.
        /// </summary>
        public decimal MaxStock { get; set; }

        /// <summary>
        /// Average stock quantity during the period.
        /// </summary>
        public decimal AverageStock { get; set; }
    }

    /// <summary>
    /// Data point representing stock quantity at a specific date.
    /// </summary>
    public class StockTrendDataPoint
    {
        /// <summary>
        /// Date of the data point.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Stock quantity at this date.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Type of movement that occurred (e.g., "Inbound", "Outbound", "Adjustment").
        /// </summary>
        public string? MovementType { get; set; }
    }

    /// <summary>
    /// Data point representing a stock movement (increase or decrease) at a specific date.
    /// </summary>
    public class StockMovementPoint
    {
        /// <summary>
        /// Date of the movement.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Quantity moved (always positive).
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Type of movement (e.g., "Inbound", "Outbound").
        /// </summary>
        public string? MovementType { get; set; }
    }
}
