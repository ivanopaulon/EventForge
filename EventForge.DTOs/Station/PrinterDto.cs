using EventForge.DTOs.Common;
namespace EventForge.DTOs.Station
{

    /// <summary>
    /// DTO for Printer output/display operations.
    /// </summary>
    public class PrinterDto
    {
        /// <summary>
        /// Unique identifier for the printer.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name of the printer.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type or role of the printer (e.g., Kitchen, Bar, Draft, Receipt).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Printer model (for technical reference).
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Printer location (physical or logical).
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Network address or identifier (e.g., IP, USB path).
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Current status of the printer.
        /// </summary>
        public PrinterConfigurationStatus Status { get; set; }

        /// <summary>
        /// Foreign key to the associated station (optional).
        /// </summary>
        public Guid? StationId { get; set; }

        /// <summary>
        /// Station name (for display purposes).
        /// </summary>
        public string? StationName { get; set; }

        /// <summary>
        /// Date and time when the printer was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the printer.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the printer was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the printer.
        /// </summary>
        public string? ModifiedBy { get; set; }

        // --- Fiscal Printer Support ---

        /// <summary>
        /// Indicates if this is a fiscal printer.
        /// </summary>
        public bool IsFiscalPrinter { get; set; }

        /// <summary>
        /// Protocol type for fiscal printer communication (e.g., Custom, Epson, RCH, Ditron).
        /// </summary>
        public string? ProtocolType { get; set; }

        /// <summary>
        /// Advanced configuration for fiscal printer (JSON format).
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// TCP/IP port for fiscal printer communication.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Serial port baud rate.
        /// </summary>
        public int? BaudRate { get; set; }

        /// <summary>
        /// Serial port name (e.g., COM1, /dev/ttyUSB0).
        /// </summary>
        public string? SerialPortName { get; set; }
    }
}
