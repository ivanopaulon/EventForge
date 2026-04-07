using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StorePos update operations.
    /// Contains only fields that can be modified after POS creation.
    /// </summary>
    public class UpdateStorePosDto
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
        /// Indicates if the POS is currently online/connected.
        /// </summary>
        [Display(Name = "Is Online", Description = "POS is currently online.")]
        public bool IsOnline { get; set; } = false;
    }
}
