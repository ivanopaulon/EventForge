namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for alert statistics.
    /// </summary>
    public class AlertStatisticsDto
    {
        public int TotalActiveAlerts { get; set; }
        public int TotalAcknowledgedAlerts { get; set; }
        public int TotalResolvedAlerts { get; set; }
        public int TotalDismissedAlerts { get; set; }

        public int CriticalAlerts { get; set; }
        public int ErrorAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int InfoAlerts { get; set; }

        public int LowStockAlerts { get; set; }
        public int OverstockAlerts { get; set; }
        public int ExpiryAlerts { get; set; }
        public int ReorderAlerts { get; set; }
        public int ZeroStockAlerts { get; set; }

        public DateTime? OldestActiveAlert { get; set; }
        public DateTime? NewestActiveAlert { get; set; }

        public int AlertsNeedingNotification { get; set; }
        public int AlertsNotificationsSent { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}