using Prym.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace Prym.DTOs.Station
{

    /// <summary>
    /// DTO for Printer update operations.
    /// </summary>
    public class UpdatePrinterDto
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

        // --- Fiscal Printer Support ---

        /// <summary>Indicates if this is a fiscal printer.</summary>
        [Display(Name = "Is Fiscal Printer")]
        public bool IsFiscalPrinter { get; set; }

        /// <summary>Protocol type (e.g., "Custom"). Other values are reserved for future use.</summary>
        [MaxLength(50)]
        [Display(Name = "Protocol Type")]
        public string? ProtocolType { get; set; }

        /// <summary>TCP/IP port for fiscal printer communication (1-65535).</summary>
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        [Display(Name = "Port")]
        public int? Port { get; set; }

        /// <summary>Serial port baud rate (300-115200).</summary>
        [Range(300, 115200, ErrorMessage = "Baud rate must be between 300 and 115200.")]
        [Display(Name = "Baud Rate")]
        public int? BaudRate { get; set; }

        /// <summary>Serial port name (e.g., COM1, /dev/ttyUSB0).</summary>
        [MaxLength(20)]
        [Display(Name = "Serial Port Name")]
        public string? SerialPortName { get; set; }

        // --- Connection Type & USB-via-Agent ---

        /// <summary>How the printer is connected (TCP, Serial, USB via Agent, etc).</summary>
        [Display(Name = "Connection Type")]
        public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.Tcp;

        /// <summary>Agent used for UsbViaAgent connection.</summary>
        [Display(Name = "Agent ID")]
        public Guid? AgentId { get; set; }

        /// <summary>USB device identifier (e.g. VID:PID or friendly name).</summary>
        [MaxLength(100)]
        [Display(Name = "USB Device ID")]
        public string? UsbDeviceId { get; set; }

        // --- Non-Fiscal Printer Classification ---

        /// <summary>Functional category of the printer.</summary>
        [Display(Name = "Printer Category")]
        public PrinterCategory Category { get; set; } = PrinterCategory.Receipt;

        /// <summary>Indicates if this is a thermal printer.</summary>
        [Display(Name = "Is Thermal")]
        public bool IsThermal { get; set; }

        /// <summary>Print width in characters per line (e.g. 42, 58, 80).</summary>
        [Range(10, 200)]
        [Display(Name = "Printer Width")]
        public int? PrinterWidth { get; set; }

        /// <summary>Paper width in millimeters.</summary>
        [Display(Name = "Paper Width")]
        public PaperWidth? PaperWidth { get; set; }

        /// <summary>Command language used by the printer.</summary>
        [Display(Name = "Print Language")]
        public PrintLanguage? PrintLanguage { get; set; }
    }
}
