namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Detailed reliability metrics for a supplier.
    /// </summary>
    public class ReliabilityMetrics
    {
        /// <summary>
        /// Total number of orders placed with this supplier.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// On-time delivery rate (0-100%).
        /// </summary>
        public decimal OnTimeDeliveryRate { get; set; }

        /// <summary>
        /// Order accuracy rate (0-100%).
        /// </summary>
        public decimal OrderAccuracyRate { get; set; }

        /// <summary>
        /// Product defect rate (0-100%).
        /// </summary>
        public decimal DefectRate { get; set; }

        /// <summary>
        /// Average response time in hours.
        /// </summary>
        public int AverageResponseTimeHours { get; set; }
    }
}
