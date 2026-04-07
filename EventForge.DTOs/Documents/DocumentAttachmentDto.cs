using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for document attachment data transfer
    /// </summary>
    public class DocumentAttachmentDto
    {
        /// <summary>
        /// Unique identifier for the attachment
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the document header (if attached to header)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to the document row (if attached to specific row)
        /// </summary>
        public Guid? DocumentRowId { get; set; }

        /// <summary>
        /// Original filename of the attachment
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Version number for this attachment
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Reference to previous version (for versioning)
        /// </summary>
        public Guid? PreviousVersionId { get; set; }

        /// <summary>
        /// Title or description of the attachment
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the attachment
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indicates if this attachment is digitally signed
        /// </summary>
        public bool IsSigned { get; set; }

        /// <summary>
        /// Date when the attachment was signed
        /// </summary>
        public DateTime? SignedAt { get; set; }

        /// <summary>
        /// User who signed the attachment
        /// </summary>
        public string? SignedBy { get; set; }

        /// <summary>
        /// Indicates if this is the current/active version
        /// </summary>
        public bool IsCurrentVersion { get; set; }

        /// <summary>
        /// Category or type of attachment
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Access level for the attachment
        /// </summary>
        public string AccessLevel { get; set; } = string.Empty;

        /// <summary>
        /// External storage provider (if stored externally)
        /// </summary>
        public string? StorageProvider { get; set; }

        /// <summary>
        /// External reference ID (for cloud storage, etc.)
        /// </summary>
        public string? ExternalReference { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the attachment
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// User who last modified the attachment
        /// </summary>
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating document attachments
    /// </summary>
    public class CreateDocumentAttachmentDto
    {
        /// <summary>
        /// Reference to the document header (if attached to header)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to the document row (if attached to specific row)
        /// </summary>
        public Guid? DocumentRowId { get; set; }

        /// <summary>
        /// Original filename of the attachment
        /// </summary>
        [Required(ErrorMessage = "Filename is required.")]
        [StringLength(255, ErrorMessage = "Filename cannot exceed 255 characters.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Storage path or identifier for the file
        /// </summary>
        [Required(ErrorMessage = "Storage path is required.")]
        [StringLength(500, ErrorMessage = "Storage path cannot exceed 500 characters.")]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the file
        /// </summary>
        [Required(ErrorMessage = "MIME type is required.")]
        [StringLength(100, ErrorMessage = "MIME type cannot exceed 100 characters.")]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative.")]
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Title or description of the attachment
        /// </summary>
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the attachment
        /// </summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Category or type of attachment
        /// </summary>
        public string Category { get; set; } = "Document";

        /// <summary>
        /// Access level for the attachment
        /// </summary>
        public string AccessLevel { get; set; } = "Internal";

        /// <summary>
        /// External storage provider (if stored externally)
        /// </summary>
        [StringLength(50, ErrorMessage = "Storage provider cannot exceed 50 characters.")]
        public string? StorageProvider { get; set; }

        /// <summary>
        /// External reference ID (for cloud storage, etc.)
        /// </summary>
        [StringLength(200, ErrorMessage = "External reference cannot exceed 200 characters.")]
        public string? ExternalReference { get; set; }
    }

    /// <summary>
    /// DTO for updating document attachments
    /// </summary>
    public class UpdateDocumentAttachmentDto
    {
        /// <summary>
        /// Title or description of the attachment
        /// </summary>
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }

        /// <summary>
        /// Additional notes about the attachment
        /// </summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Category or type of attachment
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Access level for the attachment
        /// </summary>
        public string? AccessLevel { get; set; }
    }

    /// <summary>
    /// DTO for uploading new attachment versions
    /// </summary>
    public class AttachmentVersionDto
    {
        /// <summary>
        /// New filename for the version
        /// </summary>
        [Required(ErrorMessage = "Filename is required.")]
        [StringLength(255, ErrorMessage = "Filename cannot exceed 255 characters.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Storage path for the new version
        /// </summary>
        [Required(ErrorMessage = "Storage path is required.")]
        [StringLength(500, ErrorMessage = "Storage path cannot exceed 500 characters.")]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the new version
        /// </summary>
        [Required(ErrorMessage = "MIME type is required.")]
        [StringLength(100, ErrorMessage = "MIME type cannot exceed 100 characters.")]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the new version file in bytes
        /// </summary>
        [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative.")]
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Notes about this version
        /// </summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}