namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for Contact output/display operations.
    /// </summary>
    public class ContactDto
    {
        /// <summary>
        /// Unique identifier for the contact.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the owning entity.
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Type of the owning entity.
        /// </summary>
        public string OwnerType { get; set; } = string.Empty;

        /// <summary>
        /// Type of contact.
        /// </summary>
        public ContactType ContactType { get; set; }

        /// <summary>
        /// Contact value (e.g., email address, phone number).
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Purpose of this contact (Primary, Emergency, Billing, Coach, etc.).
        /// </summary>
        public ContactPurpose Purpose { get; set; }

        /// <summary>
        /// Relationship to the owner (for emergency contacts, family relationships, etc.).
        /// </summary>
        public string? Relationship { get; set; }

        /// <summary>
        /// Indicates if this is the primary contact of its type.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the contact was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the contact.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the contact was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the contact.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
