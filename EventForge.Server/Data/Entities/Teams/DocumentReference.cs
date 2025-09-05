using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Teams;

/// <summary>
/// Represents a document reference for team or team member documentation (medical certificates, photos, etc.).
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class DocumentReference : AuditableEntity
{
    /// <summary>
    /// ID of the owning entity (Team or TeamMember).
    /// </summary>
    [Display(Name = "Owner ID", Description = "ID of the entity that owns this document.")]
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Type of the owning entity ("Team" or "TeamMember").
    /// </summary>
    [MaxLength(50, ErrorMessage = "The owner type cannot exceed 50 characters.")]
    [Display(Name = "Owner Type", Description = "Type of the entity that owns this document.")]
    public string? OwnerType { get; set; }

    /// <summary>
    /// Original filename of the document.
    /// </summary>
    [Required(ErrorMessage = "The filename is required.")]
    [MaxLength(255, ErrorMessage = "The filename cannot exceed 255 characters.")]
    [Display(Name = "Filename", Description = "Original filename of the document.")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Type of document (MedicalCertificate, MembershipCard, ProfilePhoto, etc.).
    /// </summary>
    [Required]
    [Display(Name = "Type", Description = "Type of document.")]
    public DocumentReferenceType Type { get; set; } = DocumentReferenceType.Other;

    /// <summary>
    /// Sub-type for more specific categorization (ProfilePhoto, TeamLogo, Thumbnail, etc.).
    /// </summary>
    [Display(Name = "Sub Type", Description = "Sub-type for more specific categorization.")]
    public DocumentReferenceSubType SubType { get; set; } = DocumentReferenceSubType.None;

    /// <summary>
    /// MIME type of the file.
    /// </summary>
    [Required(ErrorMessage = "The MIME type is required.")]
    [MaxLength(100, ErrorMessage = "The MIME type cannot exceed 100 characters.")]
    [Display(Name = "MIME Type", Description = "MIME type of the file.")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Storage key/path for the document in the storage system.
    /// </summary>
    [Required(ErrorMessage = "The storage key is required.")]
    [MaxLength(500, ErrorMessage = "The storage key cannot exceed 500 characters.")]
    [Display(Name = "Storage Key", Description = "Storage key/path for the document.")]
    public string StorageKey { get; set; } = string.Empty;

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
    /// Size of the file in bytes.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative.")]
    [Display(Name = "File Size", Description = "Size of the file in bytes.")]
    public long FileSizeBytes { get; set; } = 0;

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

    /// <summary>
    /// Navigation property for the owning team (if OwnerType is "Team").
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// Navigation property for the owning team member (if OwnerType is "TeamMember").
    /// </summary>
    public TeamMember? TeamMember { get; set; }
}