using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents an attachment linked to a document (header or row)
/// </summary>
public class DocumentAttachment : AuditableEntity
{
    /// <summary>
    /// Reference to the document header (if attached to header)
    /// </summary>
    [Display(Name = "Document Header", Description = "Reference to the document header (if attached to header).")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Reference to the document row (if attached to specific row)
    /// </summary>
    [Display(Name = "Document Row", Description = "Reference to the document row (if attached to specific row).")]
    public Guid? DocumentRowId { get; set; }

    /// <summary>
    /// Navigation property for the document row
    /// </summary>
    public DocumentRow? DocumentRow { get; set; }

    /// <summary>
    /// Original filename of the attachment
    /// </summary>
    [Required(ErrorMessage = "Filename is required.")]
    [StringLength(255, ErrorMessage = "Filename cannot exceed 255 characters.")]
    [Display(Name = "Filename", Description = "Original filename of the attachment.")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path or identifier for the file
    /// </summary>
    [Required(ErrorMessage = "Storage path is required.")]
    [StringLength(500, ErrorMessage = "Storage path cannot exceed 500 characters.")]
    [Display(Name = "Storage Path", Description = "Storage path or identifier for the file.")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [Required(ErrorMessage = "MIME type is required.")]
    [StringLength(100, ErrorMessage = "MIME type cannot exceed 100 characters.")]
    [Display(Name = "MIME Type", Description = "MIME type of the file.")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative.")]
    [Display(Name = "File Size", Description = "Size of the file in bytes.")]
    public long FileSizeBytes { get; set; } = 0;

    /// <summary>
    /// Version number for this attachment
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Version must be at least 1.")]
    [Display(Name = "Version", Description = "Version number for this attachment.")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Reference to previous version (for versioning)
    /// </summary>
    [Display(Name = "Previous Version", Description = "Reference to previous version (for versioning).")]
    public Guid? PreviousVersionId { get; set; }

    /// <summary>
    /// Navigation property for the previous version
    /// </summary>
    public DocumentAttachment? PreviousVersion { get; set; }

    /// <summary>
    /// Title or description of the attachment
    /// </summary>
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Title", Description = "Title or description of the attachment.")]
    public string? Title { get; set; }

    /// <summary>
    /// Additional notes about the attachment
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the attachment.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if this attachment is digitally signed
    /// </summary>
    [Display(Name = "Is Signed", Description = "Indicates if this attachment is digitally signed.")]
    public bool IsSigned { get; set; } = false;

    /// <summary>
    /// Digital signature information (if signed)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Signature info cannot exceed 1000 characters.")]
    [Display(Name = "Signature Info", Description = "Digital signature information (if signed).")]
    public string? SignatureInfo { get; set; }

    /// <summary>
    /// Date when the attachment was signed
    /// </summary>
    [Display(Name = "Signed At", Description = "Date when the attachment was signed.")]
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// User who signed the attachment
    /// </summary>
    [StringLength(100, ErrorMessage = "Signed by cannot exceed 100 characters.")]
    [Display(Name = "Signed By", Description = "User who signed the attachment.")]
    public string? SignedBy { get; set; }

    /// <summary>
    /// Indicates if this is the current/active version
    /// </summary>
    [Display(Name = "Is Current Version", Description = "Indicates if this is the current/active version.")]
    public bool IsCurrentVersion { get; set; } = true;

    /// <summary>
    /// Category or type of attachment
    /// </summary>
    [Display(Name = "Category", Description = "Category or type of attachment.")]
    public DocumentAttachmentCategory Category { get; set; } = DocumentAttachmentCategory.Document;

    /// <summary>
    /// Access level for the attachment
    /// </summary>
    [Display(Name = "Access Level", Description = "Access level for the attachment.")]
    public AttachmentAccessLevel AccessLevel { get; set; } = AttachmentAccessLevel.Internal;

    /// <summary>
    /// External storage provider (if stored externally)
    /// </summary>
    [StringLength(50, ErrorMessage = "Storage provider cannot exceed 50 characters.")]
    [Display(Name = "Storage Provider", Description = "External storage provider (if stored externally).")]
    public string? StorageProvider { get; set; }

    /// <summary>
    /// External reference ID (for cloud storage, etc.)
    /// </summary>
    [StringLength(200, ErrorMessage = "External reference cannot exceed 200 characters.")]
    [Display(Name = "External Reference", Description = "External reference ID (for cloud storage, etc.).")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Collection of newer versions of this attachment
    /// </summary>
    public ICollection<DocumentAttachment> NewerVersions { get; set; } = new List<DocumentAttachment>();
}

/// <summary>
/// Document attachment category enumeration
/// </summary>
public enum DocumentAttachmentCategory
{
    Document,
    Image,
    Audio,
    Video,
    Archive,
    Signature,
    Certificate,
    Other
}

/// <summary>
/// Attachment access level enumeration
/// </summary>
public enum AttachmentAccessLevel
{
    Public,
    Internal,
    Confidential,
    Restricted
}