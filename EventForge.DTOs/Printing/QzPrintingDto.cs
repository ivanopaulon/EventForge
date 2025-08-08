using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Printing
{

/// <summary>
/// Request DTO for discovering printers through QZ Tray
/// </summary>
public class PrinterDiscoveryRequestDto
{
    /// <summary>
    /// QZ Tray instance URL to connect to
    /// </summary>
    public string? QzUrl { get; set; }

    /// <summary>
    /// Whether to include detailed printer information
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Whether to check printer status
    /// </summary>
    public bool CheckStatus { get; set; } = true;

    /// <summary>
    /// Timeout for the discovery operation in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Response DTO for printer discovery operation
/// </summary>
public class PrinterDiscoveryResponseDto
{
    /// <summary>
    /// Whether the discovery operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of discovered printers
    /// </summary>
    public List<PrinterDto> Printers { get; set; } = new List<PrinterDto>();

    /// <summary>
    /// Error message if discovery failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the discovery was performed
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// QZ Tray version information
    /// </summary>
    public string? QzVersion { get; set; }

    /// <summary>
    /// Connection status to QZ Tray
    /// </summary>
    public QzConnectionStatus ConnectionStatus { get; set; }
}

/// <summary>
/// QZ Tray connection status
/// </summary>
public enum QzConnectionStatus
{
    Unknown = 0,
    Connected = 1,
    Disconnected = 2,
    Error = 3,
    Timeout = 4
}

/// <summary>
/// Request DTO for submitting a print job
/// </summary>
public class SubmitPrintJobRequestDto
{
    /// <summary>
    /// Print job details
    /// </summary>
    public PrintJobDto PrintJob { get; set; } = new PrintJobDto();

    /// <summary>
    /// Whether to validate the printer before submitting
    /// </summary>
    public bool ValidatePrinter { get; set; } = true;

    /// <summary>
    /// Whether to wait for completion confirmation
    /// </summary>
    public bool WaitForCompletion { get; set; } = false;

    /// <summary>
    /// Timeout for waiting for completion in milliseconds
    /// </summary>
    public int CompletionTimeoutMs { get; set; } = 60000;
}

/// <summary>
/// Response DTO for print job submission
/// </summary>
public class SubmitPrintJobResponseDto
{
    /// <summary>
    /// Whether the submission was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The print job with updated status
    /// </summary>
    public PrintJobDto? PrintJob { get; set; }

    /// <summary>
    /// Error message if submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// QZ Tray job ID if available
    /// </summary>
    public string? QzJobId { get; set; }

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>
/// Request DTO for checking printer status
/// </summary>
public class PrinterStatusRequestDto
{
    /// <summary>
    /// Printer ID to check
    /// </summary>
    public string PrinterId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include detailed status information
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Timeout for the status check in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 10000;
}

/// <summary>
/// Response DTO for printer status check
/// </summary>
public class PrinterStatusResponseDto
{
    /// <summary>
    /// Whether the status check was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Current printer status
    /// </summary>
    public PrinterStatus Status { get; set; }

    /// <summary>
    /// Detailed status information
    /// </summary>
    public PrinterStatusDetailsDto? Details { get; set; }

    /// <summary>
    /// Error message if status check failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the status was checked
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed printer status information
/// </summary>
public class PrinterStatusDetailsDto
{
    /// <summary>
    /// Whether the printer is online
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Current paper level (0-100)
    /// </summary>
    public int? PaperLevel { get; set; }

    /// <summary>
    /// Current ink/toner levels
    /// </summary>
    public Dictionary<string, int> InkLevels { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Number of jobs in the printer queue
    /// </summary>
    public int QueueLength { get; set; }

    /// <summary>
    /// Temperature if supported (thermal printers)
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Last error if any
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Additional status properties
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
}
}