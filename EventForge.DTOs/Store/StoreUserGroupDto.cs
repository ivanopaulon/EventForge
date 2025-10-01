using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUserGroup output/display operations.
    /// </summary>
    public class StoreUserGroupDto
    {
        /// <summary>
        /// Unique identifier for the store user group.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Technical code of the group (for programmatic use).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the group.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Status of the group.
        /// </summary>
        public CashierGroupStatus Status { get; set; }

        /// <summary>
        /// Number of cashiers in this group.
        /// </summary>
        public int CashierCount { get; set; }

        /// <summary>
        /// Number of privileges assigned to this group.
        /// </summary>
        public int PrivilegeCount { get; set; }

        // --- Issue #315: Image Management & Branding Fields ---

        /// <summary>
        /// Logo document identifier (references DocumentReference).
        /// </summary>
        public Guid? LogoDocumentId { get; set; }

        /// <summary>
        /// Logo URL (from LogoDocument if available).
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Logo thumbnail URL (from LogoDocument if available).
        /// </summary>
        public string? LogoThumbnailUrl { get; set; }

        /// <summary>
        /// Brand color in hexadecimal format (e.g., #FF5733).
        /// </summary>
        public string? ColorHex { get; set; }

        /// <summary>
        /// Indicates if this is a system-defined group (cannot be deleted).
        /// </summary>
        public bool IsSystemGroup { get; set; }

        /// <summary>
        /// Indicates if this is the default group for new users.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Date and time when the store user group was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the store user group.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the store user group was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the store user group.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
