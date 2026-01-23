using System;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Summary statistics for stock reconciliation results
    /// </summary>
    public class StockReconciliationSummaryDto
    {
        /// <summary>
        /// Total number of products analyzed
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// Number of products with correct stock (no discrepancy)
        /// </summary>
        public int CorrectCount { get; set; }

        /// <summary>
        /// Number of products with minor discrepancies (< 10%)
        /// </summary>
        public int MinorDiscrepancyCount { get; set; }

        /// <summary>
        /// Number of products with major discrepancies (> 10%)
        /// </summary>
        public int MajorDiscrepancyCount { get; set; }

        /// <summary>
        /// Number of products with missing stock (current = 0, calculated > 0)
        /// </summary>
        public int MissingCount { get; set; }

        /// <summary>
        /// Total absolute value of all differences
        /// </summary>
        public decimal TotalDifferenceValue { get; set; }
    }
}
