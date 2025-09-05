using EventForge.DTOs.Common;
using System;

namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for DocumentReference output/display operations.
    /// </summary>
    public class DocumentReferenceDto
    {
        /// <summary>
        /// Unique identifier for the document reference.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the owning entity (Team or TeamMember).
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// Type of the owning entity ("Team" or "TeamMember").
        /// </summary>
        public string? OwnerType { get; set; }

        /// <summary>
        /// Original filename of the document.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Type of document (MedicalCertificate, MembershipCard, ProfilePhoto, etc.).
        /// </summary>
        public DocumentReferenceType Type { get; set; }

        /// <summary>
        /// Sub-type for more specific categorization (ProfilePhoto, TeamLogo, Thumbnail, etc.).
        /// </summary>
        public DocumentReferenceSubType SubType { get; set; }

        /// <summary>
        /// MIME type of the file.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Storage key/path for the document in the storage system.
        /// </summary>
        public string StorageKey { get; set; } = string.Empty;

        /// <summary>
        /// Public URL for accessing the document (if applicable).
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Storage key for thumbnail image (if applicable).
        /// </summary>
        public string? ThumbnailStorageKey { get; set; }

        /// <summary>
        /// Expiry date for the document (for certificates, licenses, etc.).
        /// </summary>
        public DateTime? Expiry { get; set; }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Title or description of the document.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the document.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the document was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the document.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the document was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the document.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates if the document is expired (based on Expiry date).
        /// </summary>
        public bool IsExpired => Expiry.HasValue && Expiry.Value < DateTime.UtcNow;

        /// <summary>
        /// Days until expiration (if applicable).
        /// </summary>
        public int? DaysUntilExpiration => Expiry.HasValue ? (int?)(Expiry.Value.Date - DateTime.UtcNow.Date).Days : null;
    }
}