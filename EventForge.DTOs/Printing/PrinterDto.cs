using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Printing
{

/// <summary>
/// Data transfer object for printer information from QZ Tray
/// </summary>
public class PrinterDto
{
    /// <summary>
    /// Unique identifier for the printer
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the printer
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the printer
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the printer
    /// </summary>
    public PrinterStatus Status { get; set; } = PrinterStatus.Unknown;

    /// <summary>
    /// Whether the printer is the default printer
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether the printer is available for use
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Type of printer (thermal, laser, inkjet, etc.)
    /// </summary>
    public PrinterType Type { get; set; } = PrinterType.Unknown;

    /// <summary>
    /// IP address or hostname if network printer
    /// </summary>
    public string? NetworkAddress { get; set; }

    /// <summary>
    /// Port number for network printers
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Driver name
    /// </summary>
    public string? Driver { get; set; }

    /// <summary>
    /// Paper sizes supported by the printer
    /// </summary>
    public List<string> SupportedPaperSizes { get; set; } = new List<string>();

    /// <summary>
    /// Last time the printer status was updated
    /// </summary>
    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Configuration specific to QZ Tray
    /// </summary>
    public QzConfigurationDto? QzConfiguration { get; set; }
}

/// <summary>
/// Printer status enumeration
/// </summary>
public enum PrinterStatus
{
    Unknown = 0,
    Online = 1,
    Offline = 2,
    Error = 3,
    OutOfPaper = 4,
    PaperJam = 5,
    Busy = 6,
    Idle = 7
}

/// <summary>
/// Printer type enumeration
/// </summary>
public enum PrinterType
{
    Unknown = 0,
    Thermal = 1,
    Laser = 2,
    Inkjet = 3,
    DotMatrix = 4,
    Label = 5,
    Receipt = 6
}

/// <summary>
/// QZ Tray specific configuration
/// </summary>
public class QzConfigurationDto
{
    /// <summary>
    /// QZ Tray instance URL
    /// </summary>
    public string QzUrl { get; set; } = "ws://localhost:8182";

    /// <summary>
    /// Certificate for QZ authentication
    /// </summary>
    public string? Certificate { get; set; }

    /// <summary>
    /// Private key for QZ authentication
    /// </summary>
    public string? PrivateKey { get; set; }

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
}