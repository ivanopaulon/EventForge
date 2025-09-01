using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a version snapshot of a document for complete versioning support
/// </summary>
public class DocumentVersion : AuditableEntity
{
    /// <summary>
    /// Reference to the main document header
    /// </summary>
    [Required(ErrorMessage = "Document header is required.")]
    [Display(Name = "Document Header", Description = "Reference to the main document header.")]
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Version number (incremental)
    /// </summary>
    [Required(ErrorMessage = "Version number is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Version number must be positive.")]
    [Display(Name = "Version Number", Description = "Version number (incremental).")]
    public int VersionNumber { get; set; }

    /// <summary>
    /// Version label or tag
    /// </summary>
    [StringLength(50, ErrorMessage = "Version label cannot exceed 50 characters.")]
    [Display(Name = "Version Label", Description = "Version label or tag.")]
    public string? VersionLabel { get; set; }

    /// <summary>
    /// Description of changes in this version
    /// </summary>
    [StringLength(1000, ErrorMessage = "Change description cannot exceed 1000 characters.")]
    [Display(Name = "Change Description", Description = "Description of changes in this version.")]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Indicates if this is the current active version
    /// </summary>
    [Display(Name = "Is Current Version", Description = "Indicates if this is the current active version.")]
    public bool IsCurrentVersion { get; set; } = false;

    /// <summary>
    /// Snapshot of document data at this version (JSON)
    /// </summary>
    [Required(ErrorMessage = "Document snapshot is required.")]
    [Display(Name = "Document Snapshot", Description = "Snapshot of document data at this version.")]
    public string DocumentSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of document rows at this version (JSON)
    /// </summary>
    [Display(Name = "Rows Snapshot", Description = "Snapshot of document rows at this version.")]
    public string? RowsSnapshot { get; set; }

    /// <summary>
    /// Size of the version data in bytes
    /// </summary>
    [Display(Name = "Data Size", Description = "Size of the version data in bytes.")]
    public long DataSize { get; set; } = 0;

    /// <summary>
    /// Checksum for data integrity verification
    /// </summary>
    [StringLength(64, ErrorMessage = "Checksum cannot exceed 64 characters.")]
    [Display(Name = "Checksum", Description = "Checksum for data integrity verification.")]
    public string? Checksum { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    [StringLength(100, ErrorMessage = "Version creator cannot exceed 100 characters.")]
    [Display(Name = "Version Creator", Description = "User who created this version.")]
    public string? VersionCreator { get; set; }

    /// <summary>
    /// Reason for creating this version
    /// </summary>
    [StringLength(200, ErrorMessage = "Version reason cannot exceed 200 characters.")]
    [Display(Name = "Version Reason", Description = "Reason for creating this version.")]
    public string? VersionReason { get; set; }

    /// <summary>
    /// Workflow state when this version was created
    /// </summary>
    [Display(Name = "Workflow State", Description = "Workflow state when this version was created.")]
    public WorkflowState? WorkflowState { get; set; }

    /// <summary>
    /// Approval status for this version
    /// </summary>
    [Display(Name = "Approval Status", Description = "Approval status for this version.")]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.None;

    /// <summary>
    /// User who approved this version
    /// </summary>
    [StringLength(100, ErrorMessage = "Approved by cannot exceed 100 characters.")]
    [Display(Name = "Approved By", Description = "User who approved this version.")]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Date and time when this version was approved
    /// </summary>
    [Display(Name = "Approved At", Description = "Date and time when this version was approved.")]
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Digital signatures for this version
    /// </summary>
    [Display(Name = "Version Signatures", Description = "Digital signatures for this version.")]
    public ICollection<DocumentVersionSignature> Signatures { get; set; } = new List<DocumentVersionSignature>();

    /// <summary>
    /// Workflow steps executed for this version
    /// </summary>
    [Display(Name = "Workflow Steps", Description = "Workflow steps executed for this version.")]
    public ICollection<DocumentWorkflowStep> WorkflowSteps { get; set; } = new List<DocumentWorkflowStep>();
}

/// <summary>
/// Represents a digital signature on a document version
/// </summary>
public class DocumentVersionSignature : AuditableEntity
{
    /// <summary>
    /// Reference to the document version
    /// </summary>
    [Required(ErrorMessage = "Document version is required.")]
    [Display(Name = "Document Version", Description = "Reference to the document version.")]
    public Guid DocumentVersionId { get; set; }

    /// <summary>
    /// Navigation property for the document version
    /// </summary>
    public DocumentVersion? DocumentVersion { get; set; }

    /// <summary>
    /// User who signed the version
    /// </summary>
    [Required(ErrorMessage = "Signer is required.")]
    [StringLength(100, ErrorMessage = "Signer cannot exceed 100 characters.")]
    [Display(Name = "Signer", Description = "User who signed the version.")]
    public string Signer { get; set; } = string.Empty;

    /// <summary>
    /// Signer role or title
    /// </summary>
    [StringLength(100, ErrorMessage = "Signer role cannot exceed 100 characters.")]
    [Display(Name = "Signer Role", Description = "Signer role or title.")]
    public string? SignerRole { get; set; }

    /// <summary>
    /// Digital signature data
    /// </summary>
    [Required(ErrorMessage = "Signature data is required.")]
    [Display(Name = "Signature Data", Description = "Digital signature data.")]
    public string SignatureData { get; set; } = string.Empty;

    /// <summary>
    /// Signature algorithm used
    /// </summary>
    [StringLength(50, ErrorMessage = "Signature algorithm cannot exceed 50 characters.")]
    [Display(Name = "Signature Algorithm", Description = "Signature algorithm used.")]
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// Certificate information (if applicable)
    /// </summary>
    [StringLength(500, ErrorMessage = "Certificate info cannot exceed 500 characters.")]
    [Display(Name = "Certificate Info", Description = "Certificate information.")]
    public string? CertificateInfo { get; set; }

    /// <summary>
    /// Date and time when the signature was created
    /// </summary>
    [Required(ErrorMessage = "Signature timestamp is required.")]
    [Display(Name = "Signed At", Description = "Date and time when the signature was created.")]
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the signer
    /// </summary>
    [StringLength(45, ErrorMessage = "IP address cannot exceed 45 characters.")]
    [Display(Name = "Signer IP", Description = "IP address of the signer.")]
    public string? SignerIpAddress { get; set; }

    /// <summary>
    /// User agent of the signer
    /// </summary>
    [StringLength(200, ErrorMessage = "User agent cannot exceed 200 characters.")]
    [Display(Name = "User Agent", Description = "User agent of the signer.")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Indicates if the signature is still valid
    /// </summary>
    [Display(Name = "Is Valid", Description = "Indicates if the signature is still valid.")]
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Timestamp server response (if used)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Timestamp cannot exceed 1000 characters.")]
    [Display(Name = "Timestamp", Description = "Timestamp server response.")]
    public string? Timestamp { get; set; }
}