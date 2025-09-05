using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for DocumentReference update operations.
    /// </summary>
    public class UpdateDocumentReferenceDto
    {
        /// <summary>
        /// Original filename of the document.
        /// </summary>
        [MaxLength(255, ErrorMessage = "The filename cannot exceed 255 characters.")]
        [Display(Name = "Filename", Description = "Original filename of the document.")]
        public string? FileName { get; set; }

        /// <summary>
        /// Type of document (MedicalCertificate, MembershipCard, ProfilePhoto, etc.).
        /// </summary>
        [Display(Name = "Type", Description = "Type of document.")]
        public DocumentReferenceType? Type { get; set; }

        /// <summary>
        /// Sub-type for more specific categorization (ProfilePhoto, TeamLogo, Thumbnail, etc.).
        /// </summary>
        [Display(Name = "Sub Type", Description = "Sub-type for more specific categorization.")]
        public DocumentReferenceSubType? SubType { get; set; }

        /// <summary>
        /// Public URL for accessing the document (if applicable).
        /// </summary>
        [MaxLength(1000, ErrorMessage = "The URL cannot exceed 1000 characters.")]
        [Display(Name = "URL", Description = "Public URL for accessing the document.")]
        public string? Url { get; set; }

        /// <summary>
        /// Storage key for thumbnail image (if applicable).
        /// </summary>
        [MaxLength(500, ErrorMessage = "The thumbnail storage key cannot exceed 500 characters.")]
        [Display(Name = "Thumbnail Storage Key", Description = "Storage key for thumbnail image.")]
        public string? ThumbnailStorageKey { get; set; }

        /// <summary>
        /// Expiry date for the document (for certificates, licenses, etc.).
        /// </summary>
        [Display(Name = "Expiry Date", Description = "Expiry date for the document.")]
        public DateTime? Expiry { get; set; }

        /// <summary>
        /// Title or description of the document.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The title cannot exceed 200 characters.")]
        [Display(Name = "Title", Description = "Title or description of the document.")]
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the document.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
        [Display(Name = "Notes", Description = "Additional notes about the document.")]
        public string? Notes { get; set; }
    }
}