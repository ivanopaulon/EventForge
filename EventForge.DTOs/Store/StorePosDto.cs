using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StorePos output/display operations.
    /// </summary>
    public class StorePosDto
    {
        /// <summary>
        /// Unique identifier for the POS.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name or identifier code of the POS.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the POS.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Status of the POS.
        /// </summary>
        public CashRegisterStatus Status { get; set; }

        /// <summary>
        /// Physical or virtual location of the POS.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Date and time of the last opening of the POS.
        /// </summary>
        public DateTime? LastOpenedAt { get; set; }

        /// <summary>
        /// Additional notes about the POS.
        /// </summary>
        public string? Notes { get; set; }

        // --- Issue #315: Image Management & Extended Fields ---

        /// <summary>
        /// Image document identifier (references DocumentReference).
        /// </summary>
        public Guid? ImageDocumentId { get; set; }

        /// <summary>
        /// Image URL (from ImageDocument if available).
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Image thumbnail URL (from ImageDocument if available).
        /// </summary>
        public string? ImageThumbnailUrl { get; set; }

        /// <summary>
        /// Terminal hardware identifier (e.g., serial number, MAC address).
        /// </summary>
        public string? TerminalIdentifier { get; set; }

        /// <summary>
        /// IP address of the POS terminal (supports both IPv4 and IPv6).
        /// </summary>
        public string? IPAddress { get; set; }

        /// <summary>
        /// Indicates if the POS is currently online/connected.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Date and time of the last synchronization with the server.
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Geographical latitude coordinate of the POS location (-90 to 90).
        /// </summary>
        public decimal? LocationLatitude { get; set; }

        /// <summary>
        /// Geographical longitude coordinate of the POS location (-180 to 180).
        /// </summary>
        public decimal? LocationLongitude { get; set; }

        /// <summary>
        /// Currency code (ISO 4217, e.g., EUR, USD, GBP).
        /// </summary>
        public string? CurrencyCode { get; set; }

        /// <summary>
        /// Time zone identifier (IANA time zone database, e.g., Europe/Rome).
        /// </summary>
        public string? TimeZone { get; set; }

        /// <summary>
        /// Date and time when the POS was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the POS.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the POS was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the POS.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
