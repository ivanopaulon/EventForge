using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for document version information
    /// </summary>
    public class DocumentVersionDto
    {
        /// <summary>
        /// Unique identifier for the document version
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the document header
        /// </summary>
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Version number (incremental)
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Version label or tag
        /// </summary>
        public string? VersionLabel { get; set; }

        /// <summary>
        /// Description of changes in this version
        /// </summary>
        public string? ChangeDescription { get; set; }

        /// <summary>
        /// Indicates if this is the current active version
        /// </summary>
        public bool IsCurrentVersion { get; set; }

        /// <summary>
        /// Version creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the version
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Checksum for data integrity verification
        /// </summary>
        public string? DataChecksum { get; set; }

        /// <summary>
        /// Digital signature status
        /// </summary>
        public bool IsSigned { get; set; }

        /// <summary>
        /// Number of signatures on this version
        /// </summary>
        public int SignatureCount { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document version
    /// </summary>
    public class CreateDocumentVersionDto
    {
        /// <summary>
        /// Reference to the document header
        /// </summary>
        [Required(ErrorMessage = "Document header is required.")]
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Version label or tag
        /// </summary>
        [StringLength(50, ErrorMessage = "Version label cannot exceed 50 characters.")]
        public string? VersionLabel { get; set; }

        /// <summary>
        /// Description of changes in this version
        /// </summary>
        [StringLength(1000, ErrorMessage = "Change description cannot exceed 1000 characters.")]
        public string? ChangeDescription { get; set; }

        /// <summary>
        /// Indicates if this should be the current active version
        /// </summary>
        public bool IsCurrentVersion { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating document version information
    /// </summary>
    public class UpdateDocumentVersionDto
    {
        /// <summary>
        /// Version label or tag
        /// </summary>
        [StringLength(50, ErrorMessage = "Version label cannot exceed 50 characters.")]
        public string? VersionLabel { get; set; }

        /// <summary>
        /// Description of changes in this version
        /// </summary>
        [StringLength(1000, ErrorMessage = "Change description cannot exceed 1000 characters.")]
        public string? ChangeDescription { get; set; }

        /// <summary>
        /// Indicates if this should be the current active version
        /// </summary>
        public bool IsCurrentVersion { get; set; }
    }
}