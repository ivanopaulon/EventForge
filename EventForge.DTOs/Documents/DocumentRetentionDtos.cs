using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for document retention policy configuration.
    /// </summary>
    public class DocumentRetentionPolicyDto
    {
        /// <summary>
        /// Policy ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Document type this policy applies to.
        /// </summary>
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Document type name.
        /// </summary>
        public string? DocumentTypeName { get; set; }

        /// <summary>
        /// Retention period in days (null = indefinite).
        /// </summary>
        [Range(1, 36500)] // Max ~100 years
        public int? RetentionDays { get; set; }

        /// <summary>
        /// Whether to enable automatic deletion.
        /// </summary>
        public bool AutoDeleteEnabled { get; set; }

        /// <summary>
        /// Grace period in days before deletion after retention expires.
        /// </summary>
        [Range(0, 365)]
        public int GracePeriodDays { get; set; }

        /// <summary>
        /// Whether to archive instead of delete.
        /// </summary>
        public bool ArchiveInsteadOfDelete { get; set; }

        /// <summary>
        /// Whether policy is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Policy notes.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Created by user.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Created date.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Last updated by user.
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Last updated date.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a document retention policy.
    /// </summary>
    public class CreateDocumentRetentionPolicyDto
    {
        /// <summary>
        /// Document type this policy applies to.
        /// </summary>
        [Required]
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Retention period in days (null = indefinite).
        /// </summary>
        [Range(1, 36500)]
        public int? RetentionDays { get; set; }

        /// <summary>
        /// Whether to enable automatic deletion.
        /// </summary>
        public bool AutoDeleteEnabled { get; set; }

        /// <summary>
        /// Grace period in days before deletion after retention expires.
        /// </summary>
        [Range(0, 365)]
        public int GracePeriodDays { get; set; } = 30;

        /// <summary>
        /// Whether to archive instead of delete.
        /// </summary>
        public bool ArchiveInsteadOfDelete { get; set; }

        /// <summary>
        /// Whether policy is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Policy notes.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating a document retention policy.
    /// </summary>
    public class UpdateDocumentRetentionPolicyDto
    {
        /// <summary>
        /// Retention period in days (null = indefinite).
        /// </summary>
        [Range(1, 36500)]
        public int? RetentionDays { get; set; }

        /// <summary>
        /// Whether to enable automatic deletion.
        /// </summary>
        public bool? AutoDeleteEnabled { get; set; }

        /// <summary>
        /// Grace period in days before deletion after retention expires.
        /// </summary>
        [Range(0, 365)]
        public int? GracePeriodDays { get; set; }

        /// <summary>
        /// Whether to archive instead of delete.
        /// </summary>
        public bool? ArchiveInsteadOfDelete { get; set; }

        /// <summary>
        /// Whether policy is active.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Policy notes.
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
