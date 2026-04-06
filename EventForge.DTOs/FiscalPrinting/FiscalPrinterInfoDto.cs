namespace EventForge.DTOs.FiscalPrinting;

/// <summary>
/// Basic information read from a fiscal printer after a successful connection test.
/// Returned by <c>GET /api/v1/fiscal-printing/printer-info</c>.
/// </summary>
public class FiscalPrinterInfoDto
{
    /// <summary>Printer model as reported by the device (CMD_READ_STATUS response).</summary>
    public string? Model { get; set; }

    /// <summary>Firmware version string as reported by the device.</summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>Fiscal serial number (matricola fiscale).</summary>
    public string? FiscalSerialNumber { get; set; }

    /// <summary>
    /// Fiscal memory used as a percentage (0-100).
    /// <c>null</c> if the device did not report this value.
    /// </summary>
    public decimal? FiscalMemoryUsedPercent { get; set; }

    /// <summary>Current date/time as reported by the printer's real-time clock.</summary>
    public DateTime? PrinterDateTime { get; set; }

    /// <summary>Whether the printer is currently online and responding.</summary>
    public bool IsOnline { get; set; }

    /// <summary>Error message if the printer could not be reached or info could not be read.</summary>
    public string? ErrorMessage { get; set; }
}
