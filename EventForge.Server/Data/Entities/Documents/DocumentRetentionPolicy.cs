using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a document retention policy for GDPR compliance.
/// Defines how long documents should be kept and when they should be deleted.
/// </summary>
public class DocumentRetentionPolicy : AuditableEntity
{
    /// <summary>
    /// Document type this policy applies to.
    /// </summary>
    [Required]
    [Display(Name = "Document Type", Description = "Document type this policy applies to.")]
    public Guid DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type.
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Retention period in days (null = indefinite retention).
    /// </summary>
    [Range(1, 36500)] // Max ~100 years
    [Display(Name = "Retention Days", Description = "Number of days to retain documents (null = indefinite).")]
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Whether to enable automatic deletion when retention period expires.
    /// </summary>
    [Display(Name = "Auto Delete Enabled", Description = "Enable automatic deletion when retention expires.")]
    public bool AutoDeleteEnabled { get; set; } = false;

    /// <summary>
    /// Grace period in days before deletion after retention period expires.
    /// Allows for manual review before automatic deletion.
    /// </summary>
    [Range(0, 365)]
    [Display(Name = "Grace Period Days", Description = "Grace period before deletion after retention expires.")]
    public int GracePeriodDays { get; set; } = 30;

    /// <summary>
    /// Whether to archive documents instead of deleting them.
    /// Archived documents are moved to cold storage and marked as archived.
    /// </summary>
    [Display(Name = "Archive Instead Of Delete", Description = "Archive documents instead of deleting them.")]
    public bool ArchiveInsteadOfDelete { get; set; } = false;

    /// <summary>
    /// Additional notes about this retention policy.
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Notes", Description = "Additional notes about this retention policy.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Legal or business reason for this retention policy.
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Reason", Description = "Legal or business reason for this retention policy.")]
    public string? Reason { get; set; }

    /// <summary>
    /// Last date when this policy was applied/executed.
    /// </summary>
    [Display(Name = "Last Applied At", Description = "Last date when this policy was applied.")]
    public DateTime? LastAppliedAt { get; set; }

    /// <summary>
    /// Number of documents deleted by this policy.
    /// </summary>
    [Display(Name = "Documents Deleted", Description = "Number of documents deleted by this policy.")]
    public int DocumentsDeleted { get; set; } = 0;

    /// <summary>
    /// Number of documents archived by this policy.
    /// </summary>
    [Display(Name = "Documents Archived", Description = "Number of documents archived by this policy.")]
    public int DocumentsArchived { get; set; } = 0;
}
