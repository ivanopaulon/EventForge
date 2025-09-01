using System;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for alert check summary results.
    /// </summary>
    public class AlertCheckSummaryDto
    {
        public DateTime CheckDateTime { get; set; } = DateTime.UtcNow;

        public int LowStockAlertsCreated { get; set; }
        public int OverstockAlertsCreated { get; set; }
        public int ExpiryAlertsCreated { get; set; }
        public int TotalAlertsCreated => LowStockAlertsCreated + OverstockAlertsCreated + ExpiryAlertsCreated;

        public int ProductsChecked { get; set; }
        public int LocationsChecked { get; set; }
        public int LotsChecked { get; set; }

        public TimeSpan CheckDuration { get; set; }
        public bool CheckCompleted { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}