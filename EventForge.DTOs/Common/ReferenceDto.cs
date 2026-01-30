namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for Reference output/display operations.
    /// </summary>
    public class ReferenceDto
    {
        /// <summary>
        /// Unique identifier for the reference.
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
        /// First name of the reference person.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the reference person.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Full name of the reference person.
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Department or role of the reference person.
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the reference was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the reference.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the reference was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the reference.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
