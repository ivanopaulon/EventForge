using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Printing
{

    /// <summary>
    /// Data transfer object for print job information
    /// </summary>
    public class PrintJobDto
    {
        /// <summary>
        /// Unique identifier for the print job
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID of the printer to use
        /// </summary>
        [Required]
        public string PrinterId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the printer
        /// </summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>
        /// Title/description of the print job
        /// </summary>
        [Required]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of content being printed
        /// </summary>
        public PrintContentType ContentType { get; set; } = PrintContentType.Raw;

        /// <summary>
        /// Content to be printed
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Optional configuration for the print job
        /// </summary>
        public PrintJobConfigurationDto? Configuration { get; set; }

        /// <summary>
        /// Current status of the print job
        /// </summary>
        public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;

        /// <summary>
        /// When the print job was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the print job was submitted to the printer
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// When the print job was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if the print job failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of copies to print
        /// </summary>
        public int Copies { get; set; } = 1;

        /// <summary>
        /// Priority of the print job
        /// </summary>
        public PrintJobPriority Priority { get; set; } = PrintJobPriority.Normal;

        /// <summary>
        /// User who initiated the print job
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Username who initiated the print job
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Tenant ID associated with the print job
        /// </summary>
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Type of content being printed
    /// </summary>
    public enum PrintContentType
    {
        Raw = 0,
        Html = 1,
        Pdf = 2,
        Image = 3,
        Receipt = 4,
        Label = 5
    }

    /// <summary>
    /// Status of a print job
    /// </summary>
    public enum PrintJobStatus
    {
        Pending = 0,
        Queued = 1,
        Printing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Paused = 6
    }

    /// <summary>
    /// Priority of a print job
    /// </summary>
    public enum PrintJobPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    /// <summary>
    /// Configuration options for a print job
    /// </summary>
    public class PrintJobConfigurationDto
    {
        /// <summary>
        /// Paper size to use
        /// </summary>
        public string? PaperSize { get; set; }

        /// <summary>
        /// Print orientation
        /// </summary>
        public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;

        /// <summary>
        /// Print quality
        /// </summary>
        public PrintQuality Quality { get; set; } = PrintQuality.Normal;

        /// <summary>
        /// Whether to print in color
        /// </summary>
        public bool ColorMode { get; set; } = false;

        /// <summary>
        /// Margins for the print job
        /// </summary>
        public PrintMarginsDto? Margins { get; set; }

        /// <summary>
        /// Custom options specific to the printer or content type
        /// </summary>
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Print orientation enumeration
    /// </summary>
    public enum PrintOrientation
    {
        Portrait = 0,
        Landscape = 1
    }

    /// <summary>
    /// Print quality enumeration
    /// </summary>
    public enum PrintQuality
    {
        Draft = 0,
        Normal = 1,
        High = 2,
        Best = 3
    }

    /// <summary>
    /// Print margins configuration
    /// </summary>
    public class PrintMarginsDto
    {
        /// <summary>
        /// Top margin
        /// </summary>
        public double Top { get; set; } = 0;

        /// <summary>
        /// Bottom margin
        /// </summary>
        public double Bottom { get; set; } = 0;

        /// <summary>
        /// Left margin
        /// </summary>
        public double Left { get; set; } = 0;

        /// <summary>
        /// Right margin
        /// </summary>
        public double Right { get; set; } = 0;

        /// <summary>
        /// Unit of measurement for margins
        /// </summary>
        public MarginUnit Unit { get; set; } = MarginUnit.Millimeters;
    }

    /// <summary>
    /// Unit of measurement for margins
    /// </summary>
    public enum MarginUnit
    {
        Millimeters = 0,
        Inches = 1,
        Points = 2
    }
}