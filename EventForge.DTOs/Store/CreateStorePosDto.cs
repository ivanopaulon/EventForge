using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StorePos creation operations.
    /// </summary>
    public class CreateStorePosDto
    {
        /// <summary>
        /// Name or identifier code of the POS.
        /// </summary>
        [Required(ErrorMessage = "The POS name is required.")]
        [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
        [Display(Name = "POS Name", Description = "Name or identifier code of the POS.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the POS.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
        [Display(Name = "Description", Description = "Description of the POS.")]
        public string? Description { get; set; }

        /// <summary>
        /// Status of the POS.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the POS.")]
        public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Active;

        /// <summary>
        /// Physical or virtual location of the POS.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The location cannot exceed 100 characters.")]
        [Display(Name = "Location", Description = "Physical or virtual location of the POS.")]
        public string? Location { get; set; }

        /// <summary>
        /// Additional notes about the POS.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
        [Display(Name = "Notes", Description = "Additional notes about the POS.")]
        public string? Notes { get; set; }

        // --- Issue #315: Extended Fields ---

        /// <summary>
        /// Terminal hardware identifier (e.g., serial number, MAC address).
        /// </summary>
        [MaxLength(100, ErrorMessage = "The terminal identifier cannot exceed 100 characters.")]
        [Display(Name = "Terminal Identifier", Description = "Terminal hardware identifier.")]
        public string? TerminalIdentifier { get; set; }

        /// <summary>
        /// IP address of the POS terminal (supports both IPv4 and IPv6).
        /// </summary>
        [MaxLength(45, ErrorMessage = "The IP address cannot exceed 45 characters.")]
        [Display(Name = "IP Address", Description = "IP address of the POS terminal.")]
        public string? IPAddress { get; set; }

        /// <summary>
        /// Geographical latitude coordinate of the POS location (-90 to 90).
        /// </summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        [Display(Name = "Location Latitude", Description = "Geographical latitude (-90 to 90).")]
        public decimal? LocationLatitude { get; set; }

        /// <summary>
        /// Geographical longitude coordinate of the POS location (-180 to 180).
        /// </summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        [Display(Name = "Location Longitude", Description = "Geographical longitude (-180 to 180).")]
        public decimal? LocationLongitude { get; set; }

        /// <summary>
        /// Currency code (ISO 4217, e.g., EUR, USD, GBP).
        /// </summary>
        [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Invalid currency code. Use ISO 4217 format (e.g., EUR, USD).")]
        [Display(Name = "Currency Code", Description = "ISO 4217 currency code (e.g., EUR, USD).")]
        public string? CurrencyCode { get; set; }

        /// <summary>
        /// Time zone identifier (IANA time zone database, e.g., Europe/Rome).
        /// </summary>
        [MaxLength(50, ErrorMessage = "The time zone cannot exceed 50 characters.")]
        [Display(Name = "Time Zone", Description = "IANA time zone (e.g., Europe/Rome).")]
        public string? TimeZone { get; set; }
    }
}
