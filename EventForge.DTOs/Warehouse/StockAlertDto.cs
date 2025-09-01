using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

    /// <summary>
    /// DTO for stock alert information.
    /// </summary>
    public class StockAlertDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public Guid StockId { get; set; }

        // Stock information for display
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? LocationCode { get; set; }
        public string? WarehouseName { get; set; }
        public string? LotCode { get; set; }

        [Required]
        public string AlertType { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public decimal CurrentLevel { get; set; }
        public decimal Threshold { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime TriggeredDate { get; set; }

        public DateTime? AcknowledgedDate { get; set; }

        [StringLength(100)]
        public string? AcknowledgedBy { get; set; }

        public DateTime? ResolvedDate { get; set; }

        [StringLength(100)]
        public string? ResolvedBy { get; set; }

        [StringLength(500)]
        public string? ResolutionNotes { get; set; }

        public bool SendEmailNotifications { get; set; } = true;

        [StringLength(500)]
        public string? NotificationEmails { get; set; }

        public DateTime? LastNotificationDate { get; set; }
        public int NotificationCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}