using Prym.DTOs.Common;
namespace Prym.DTOs.Station
{

    /// <summary>
    /// DTO for Station output/display operations.
    /// </summary>
    public class StationDto
    {
        /// <summary>
        /// Unique identifier for the station.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the station.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the station.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Current status of the station.
        /// </summary>
        public StationStatus Status { get; set; }

        /// <summary>
        /// Station location (physical or logical).
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Custom sort order for displaying stations.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Additional notes for the station.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Number of printers assigned to this station.
        /// </summary>
        public int PrinterCount { get; set; }

        /// <summary>
        /// Functional type of the station (KDS, Kitchen, Bar, POS, etc.).
        /// </summary>
        public StationType StationType { get; set; }

        /// <summary>
        /// ID of the printer assigned to this station for order/KDS output.
        /// </summary>
        public Guid? AssignedPrinterId { get; set; }

        /// <summary>
        /// If true, items routed to this station also appear on the fiscal receipt.
        /// </summary>
        public bool PrintsReceiptCopy { get; set; }

        /// <summary>
        /// Date and time when the station was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the station.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the station was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the station.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
