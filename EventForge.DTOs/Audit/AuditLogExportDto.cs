using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Audit
{
    /// <summary>
    /// DTO for audit log export request.
    /// </summary>
    public class AuditLogExportDto
    {
        /// <summary>
        /// Export format (JSON, CSV, TXT).
        /// </summary>
        [Required]
        public string Format { get; set; } = "JSON";

        /// <summary>
        /// Start date for the export range.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// End date for the export range.
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by specific operation types.
        /// </summary>
        public List<string>? OperationTypes { get; set; }

        /// <summary>
        /// Filter by specific user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Filter by specific tenant ID.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Filter by success status.
        /// </summary>
        public bool? WasSuccessful { get; set; }
    }

    /// <summary>
    /// DTO for audit log statistics.
    /// </summary>
    public class AuditLogStatisticsDto
    {
        /// <summary>
        /// Total number of audit logs.
        /// </summary>
        public int TotalLogs { get; set; }

        /// <summary>
        /// Number of logs created today.
        /// </summary>
        public int LogsToday { get; set; }

        /// <summary>
        /// Number of logs created this week.
        /// </summary>
        public int LogsThisWeek { get; set; }

        /// <summary>
        /// Number of logs created this month.
        /// </summary>
        public int LogsThisMonth { get; set; }

        /// <summary>
        /// Number of critical operations logged.
        /// </summary>
        public int CriticalOperations { get; set; }

        /// <summary>
        /// Last time statistics were updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}