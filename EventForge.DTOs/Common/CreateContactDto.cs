using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for creating a new contact.
    /// </summary>
    public class CreateContactDto
    {
        /// <summary>
        /// ID of the owning entity.
        /// </summary>
        [Required(ErrorMessage = "Owner ID is required.")]
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Type of the owning entity.
        /// </summary>
        [Required(ErrorMessage = "Owner type is required.")]
        [MaxLength(50, ErrorMessage = "Owner type cannot exceed 50 characters.")]
        public string OwnerType { get; set; } = string.Empty;

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
        /// Purpose of this contact (Primary, Emergency, Billing, Coach, etc.).
        /// </summary>
        [Display(Name = "Purpose", Description = "Purpose of this contact.")]
        public ContactPurpose Purpose { get; set; } = ContactPurpose.Primary;

        /// <summary>
        /// Relationship to the owner (for emergency contacts, family relationships, etc.).
        /// </summary>
        [MaxLength(50, ErrorMessage = "The relationship cannot exceed 50 characters.")]
        [Display(Name = "Relationship", Description = "Relationship to the owner.")]
        public string? Relationship { get; set; }

        /// <summary>
        /// Indicates if this is the primary contact of its type.
        /// </summary>
        [Display(Name = "Is Primary", Description = "Indicates if this is the primary contact of its type.")]
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Additional notes.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Notes cannot exceed 100 characters.")]
        public string? Notes { get; set; }
    }
}
