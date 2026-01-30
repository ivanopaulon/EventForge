using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for creating a stock alert.
    /// </summary>
    public class CreateStockAlertDto
    {
        [Required(ErrorMessage = "Stock entry is required.")]
        public Guid StockId { get; set; }

        [Required(ErrorMessage = "Alert type is required.")]
        public string AlertType { get; set; } = string.Empty;

        public string Severity { get; set; } = "Warning";

        public decimal CurrentLevel { get; set; }

        public decimal Threshold { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;

        public bool SendEmailNotifications { get; set; } = true;

        [StringLength(500, ErrorMessage = "Notification emails cannot exceed 500 characters.")]
        public string? NotificationEmails { get; set; }
    }
}