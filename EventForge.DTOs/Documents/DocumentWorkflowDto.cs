using System;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for DocumentWorkflow output/display operations
    /// </summary>
    public class DocumentWorkflowDto
    {
        /// <summary>
        /// Unique identifier for the workflow
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the workflow
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the workflow
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Document type this workflow applies to
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Document type name for display
        /// </summary>
        public string? DocumentTypeName { get; set; }

        /// <summary>
        /// Workflow category for organization
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Priority level of the workflow
        /// </summary>
        public WorkflowPriority Priority { get; set; }

        /// <summary>
        /// Indicates if this workflow is active and available for use
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Minimum threshold for automatic approval
        /// </summary>
        public decimal? AutoApprovalThreshold { get; set; }

        /// <summary>
        /// Maximum duration in hours before escalation
        /// </summary>
        public int? EscalationTimeoutHours { get; set; }

        /// <summary>
        /// Default assignee for workflow steps
        /// </summary>
        public string? DefaultAssignee { get; set; }

        /// <summary>
        /// Notification configuration in JSON format
        /// </summary>
        public string? NotificationConfig { get; set; }

        /// <summary>
        /// Version number for tracking workflow changes
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Date and time when the workflow was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the workflow
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the workflow was last modified (UTC)
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the workflow
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Number of step definitions in this workflow
        /// </summary>
        public int StepCount { get; set; }

        /// <summary>
        /// Number of active executions using this workflow
        /// </summary>
        public int ActiveExecutions { get; set; }
    }

    /// <summary>
    /// DTO for creating a new document workflow
    /// </summary>
    public class CreateDocumentWorkflowDto
    {
        /// <summary>
        /// Name of the workflow
        /// </summary>
        [Required(ErrorMessage = "Workflow name is required.")]
        [StringLength(100, ErrorMessage = "Workflow name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the workflow
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Document type this workflow applies to
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Workflow category for organization
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Priority level of the workflow
        /// </summary>
        public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

        /// <summary>
        /// Minimum threshold for automatic approval
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Auto approval threshold must be positive.")]
        public decimal? AutoApprovalThreshold { get; set; }

        /// <summary>
        /// Maximum duration in hours before escalation
        /// </summary>
        [Range(1, 8760, ErrorMessage = "Escalation timeout must be between 1 and 8760 hours (1 year).")]
        public int? EscalationTimeoutHours { get; set; }

        /// <summary>
        /// Default assignee for workflow steps
        /// </summary>
        [StringLength(100, ErrorMessage = "Default assignee cannot exceed 100 characters.")]
        public string? DefaultAssignee { get; set; }

        /// <summary>
        /// Notification configuration in JSON format
        /// </summary>
        [StringLength(2000, ErrorMessage = "Notification config cannot exceed 2000 characters.")]
        public string? NotificationConfig { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing document workflow
    /// </summary>
    public class UpdateDocumentWorkflowDto
    {
        /// <summary>
        /// Name of the workflow
        /// </summary>
        [Required(ErrorMessage = "Workflow name is required.")]
        [StringLength(100, ErrorMessage = "Workflow name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the workflow
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Document type this workflow applies to
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Workflow category for organization
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; }

        /// <summary>
        /// Priority level of the workflow
        /// </summary>
        public WorkflowPriority Priority { get; set; }

        /// <summary>
        /// Indicates if this workflow is active and available for use
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Minimum threshold for automatic approval
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Auto approval threshold must be positive.")]
        public decimal? AutoApprovalThreshold { get; set; }

        /// <summary>
        /// Maximum duration in hours before escalation
        /// </summary>
        [Range(1, 8760, ErrorMessage = "Escalation timeout must be between 1 and 8760 hours (1 year).")]
        public int? EscalationTimeoutHours { get; set; }

        /// <summary>
        /// Default assignee for workflow steps
        /// </summary>
        [StringLength(100, ErrorMessage = "Default assignee cannot exceed 100 characters.")]
        public string? DefaultAssignee { get; set; }

        /// <summary>
        /// Notification configuration in JSON format
        /// </summary>
        [StringLength(2000, ErrorMessage = "Notification config cannot exceed 2000 characters.")]
        public string? NotificationConfig { get; set; }
    }
}