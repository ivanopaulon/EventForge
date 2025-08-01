using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for updating an existing contact.
    /// </summary>
    public class UpdateContactDto
    {
        /// <summary>
        /// Type of contact.
        /// </summary>
        [Required(ErrorMessage = "Contact type is required.")]
        public ContactType ContactType { get; set; } = ContactType.Email;

        /// <summary>
        /// Contact value (e.g., email address, phone number).
        /// </summary>
        [Required(ErrorMessage = "Contact value is required.")]
        [MaxLength(100, ErrorMessage = "Contact value cannot exceed 100 characters.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Notes cannot exceed 100 characters.")]
        public string? Notes { get; set; }
    }
}
