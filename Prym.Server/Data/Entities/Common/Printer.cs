using System.ComponentModel.DataAnnotations;

namespace Prym.Server.Data.Entities.Common;


/// <summary>
/// Represents a physical or virtual printer used in the POS system.
/// </summary>
public class Printer : AuditableEntity
{
    /// <summary>
    /// Display name of the printer.
    /// </summary>
    [Required(ErrorMessage = "The printer name is required.")]
    [MaxLength(50, ErrorMessage = "The printer name cannot exceed 50 characters.")]
    [Display(Name = "Printer Name", Description = "Name or label of the printer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type or role of the printer (e.g., Kitchen, Bar, Draft, Receipt).
    /// </summary>
    [Required(ErrorMessage = "The printer type is required.")]
    [MaxLength(30, ErrorMessage = "The printer type cannot exceed 30 characters.")]
    [Display(Name = "Type", Description = "Type or role of the printer (e.g., Kitchen, Bar, Draft, Receipt).")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Printer model (for technical reference).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The model cannot exceed 50 characters.")]
    [Display(Name = "Model", Description = "Printer model.")]
    public string? Model { get; set; }

    /// <summary>
    /// Printer location (physical or logical).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The location cannot exceed 50 characters.")]
    [Display(Name = "Location", Description = "Physical or logical location of the printer.")]
    public string? Location { get; set; }

    /// <summary>
    /// Network address or identifier (e.g., IP, USB path).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The address cannot exceed 100 characters.")]
    [Display(Name = "Address", Description = "IP address, USB path, or identifier of the printer.")]
    public string? Address { get; set; }

    /// <summary>
    /// Current status of the printer.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the printer.")]
    public PrinterConfigurationStatus Status { get; set; } = PrinterConfigurationStatus.Active;

    /// <summary>
    /// Foreign key to the associated station (optional).
    /// </summary>
    [Display(Name = "Station", Description = "Station associated with the printer.")]
    public Guid? StationId { get; set; }

    /// <summary>
    /// Navigation property for the station.
    /// </summary>
    public Station? Station { get; set; }

    // --- Fiscal Printer Support ---

    /// <summary>
    /// Indicates if this is a fiscal printer.
    /// </summary>
    [Display(Name = "Is Fiscal Printer", Description = "Indicates if this is a fiscal printer.")]
    public bool IsFiscalPrinter { get; set; }

    /// <summary>
    /// Protocol type for fiscal printer communication (e.g., Custom, Epson, RCH, Ditron).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The protocol type cannot exceed 50 characters.")]
    [Display(Name = "Protocol Type", Description = "Protocol type for fiscal printer communication.")]
    public string? ProtocolType { get; set; }

    /// <summary>
    /// Advanced configuration for fiscal printer (JSON format).
    /// </summary>
    [MaxLength(500, ErrorMessage = "The connection string cannot exceed 500 characters.")]
    [Display(Name = "Connection String", Description = "Advanced configuration (JSON).")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// TCP/IP port for fiscal printer communication (1-65535).
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    [Display(Name = "Port", Description = "TCP/IP port for communication.")]
    public int? Port { get; set; }

    /// <summary>
    /// Serial port baud rate (300-115200).
    /// </summary>
    [Range(300, 115200, ErrorMessage = "Baud rate must be between 300 and 115200.")]
    [Display(Name = "Baud Rate", Description = "Serial port baud rate.")]
    public int? BaudRate { get; set; }

    /// <summary>
    /// Serial port name (e.g., COM1, /dev/ttyUSB0).
    /// </summary>
    [MaxLength(20, ErrorMessage = "The serial port name cannot exceed 20 characters.")]
    [Display(Name = "Serial Port Name", Description = "Serial port identifier.")]
    public string? SerialPortName { get; set; }

    // --- Connection Type (Fase B: USB via Agent) ---

    /// <summary>
    /// Type of connection to the printer.
    /// </summary>
    [Display(Name = "Connection Type", Description = "How the printer is connected (TCP, Serial, USB via Agent, etc).")]
    public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.Tcp;

    /// <summary>
    /// Agent identifier for USB-via-Agent printers (references registered Agent).
    /// </summary>
    [Display(Name = "Agent ID", Description = "Agent used for USB-via-Agent connection.")]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// USB device identifier (e.g. VID:PID or friendly name) for USB printers.
    /// </summary>
    [MaxLength(100, ErrorMessage = "USB device ID cannot exceed 100 characters.")]
    [Display(Name = "USB Device ID", Description = "USB device identifier.")]
    public string? UsbDeviceId { get; set; }

    // --- Non-Fiscal Printer Classification ---

    /// <summary>
    /// Category of printer: Fiscal, Receipt, KDS, Label, or Report.
    /// </summary>
    [Display(Name = "Printer Category", Description = "Functional category of the printer.")]
    public PrinterCategory Category { get; set; } = PrinterCategory.Receipt;

    /// <summary>
    /// Indicates if this is a thermal printer (direct thermal or thermal transfer).
    /// </summary>
    [Display(Name = "Is Thermal", Description = "Thermal printing technology.")]
    public bool IsThermal { get; set; }

    /// <summary>
    /// Print width in characters per line (e.g. 42, 58, 80).
    /// </summary>
    [Range(10, 200, ErrorMessage = "Printer width must be between 10 and 200 characters.")]
    [Display(Name = "Printer Width", Description = "Characters per line (e.g. 42, 58, 80).")]
    public int? PrinterWidth { get; set; }

    /// <summary>
    /// Paper width in millimeters.
    /// </summary>
    [Display(Name = "Paper Width", Description = "Paper width in mm.")]
    public PaperWidth? PaperWidth { get; set; }

    /// <summary>
    /// Print language / command language used by the printer.
    /// </summary>
    [Display(Name = "Print Language", Description = "Command language (ESC/POS, STAR, ZPL, etc).")]
    public PrintLanguage? PrintLanguage { get; set; }
}

