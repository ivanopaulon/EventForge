using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a workflow definition for document approval processes
/// </summary>
public class DocumentWorkflow : AuditableEntity
{
    /// <summary>
    /// Name of the workflow
    /// </summary>
    [Required(ErrorMessage = "Workflow name is required.")]
    [StringLength(100, ErrorMessage = "Workflow name cannot exceed 100 characters.")]
    [Display(Name = "Workflow Name", Description = "Name of the workflow.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the workflow
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the workflow.")]
    public string? Description { get; set; }

    /// <summary>
    /// Document type this workflow applies to
    /// </summary>
    [Display(Name = "Document Type", Description = "Document type this workflow applies to.")]
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Workflow category for organization
    /// </summary>
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Workflow category for organization.")]
    public string? Category { get; set; }

    /// <summary>
    /// Priority level of the workflow
    /// </summary>
    [Display(Name = "Priority", Description = "Priority level of the workflow.")]
    public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

    /// <summary>
    /// Indicates if this workflow is active and available for use
    /// </summary>
    [Display(Name = "Is Active", Description = "Indicates if this workflow is active.")]
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if this is the default workflow for the document type
    /// </summary>
    [Display(Name = "Is Default", Description = "Indicates if this is the default workflow.")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Workflow configuration as JSON
    /// </summary>
    [Display(Name = "Workflow Configuration", Description = "Workflow configuration as JSON.")]
    public string? WorkflowConfiguration { get; set; }

    /// <summary>
    /// Conditions for triggering this workflow (JSON)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Trigger conditions cannot exceed 2000 characters.")]
    [Display(Name = "Trigger Conditions", Description = "Conditions for triggering this workflow.")]
    public string? TriggerConditions { get; set; }

    /// <summary>
    /// Maximum processing time allowed for this workflow (in hours)
    /// </summary>
    [Range(1, 8760, ErrorMessage = "Max processing time must be between 1 and 8760 hours.")]
    [Display(Name = "Max Processing Time (Hours)", Description = "Maximum processing time allowed.")]
    public int? MaxProcessingTimeHours { get; set; }

    /// <summary>
    /// Escalation rules if workflow exceeds time limits (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Escalation rules cannot exceed 1000 characters.")]
    [Display(Name = "Escalation Rules", Description = "Escalation rules for time overruns.")]
    public string? EscalationRules { get; set; }

    /// <summary>
    /// Notification settings for this workflow (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notification settings cannot exceed 1000 characters.")]
    [Display(Name = "Notification Settings", Description = "Notification settings for this workflow.")]
    public string? NotificationSettings { get; set; }

    /// <summary>
    /// Auto-approval rules (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Auto-approval rules cannot exceed 1000 characters.")]
    [Display(Name = "Auto-Approval Rules", Description = "Auto-approval rules for this workflow.")]
    public string? AutoApprovalRules { get; set; }

    /// <summary>
    /// Version of the workflow (for change tracking)
    /// </summary>
    [Display(Name = "Workflow Version", Description = "Version of the workflow.")]
    public int WorkflowVersion { get; set; } = 1;

    /// <summary>
    /// Statistics: Number of times this workflow has been used
    /// </summary>
    [Display(Name = "Usage Count", Description = "Number of times this workflow has been used.")]
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Statistics: Average completion time in hours
    /// </summary>
    [Display(Name = "Average Completion Time", Description = "Average completion time in hours.")]
    public decimal? AverageCompletionTimeHours { get; set; }

    /// <summary>
    /// Statistics: Success rate percentage
    /// </summary>
    [Range(0, 100, ErrorMessage = "Success rate must be between 0 and 100.")]
    [Display(Name = "Success Rate", Description = "Success rate percentage.")]
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// Workflow steps definition
    /// </summary>
    [Display(Name = "Workflow Steps", Description = "Workflow steps definition.")]
    public ICollection<DocumentWorkflowStepDefinition> StepDefinitions { get; set; } = new List<DocumentWorkflowStepDefinition>();

    /// <summary>
    /// Document workflow executions using this workflow
    /// </summary>
    [Display(Name = "Workflow Executions", Description = "Document workflow executions.")]
    public ICollection<DocumentWorkflowExecution> WorkflowExecutions { get; set; } = new List<DocumentWorkflowExecution>();
}

/// <summary>
/// Represents a step definition in a workflow
/// </summary>
public class DocumentWorkflowStepDefinition : AuditableEntity
{
    /// <summary>
    /// Reference to the workflow
    /// </summary>
    [Required(ErrorMessage = "Workflow is required.")]
    [Display(Name = "Workflow", Description = "Reference to the workflow.")]
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Navigation property for the workflow
    /// </summary>
    public DocumentWorkflow? Workflow { get; set; }

    /// <summary>
    /// Step order in the workflow
    /// </summary>
    [Required(ErrorMessage = "Step order is required.")]
    [Range(1, 100, ErrorMessage = "Step order must be between 1 and 100.")]
    [Display(Name = "Step Order", Description = "Step order in the workflow.")]
    public int StepOrder { get; set; }

    /// <summary>
    /// Name of the step
    /// </summary>
    [Required(ErrorMessage = "Step name is required.")]
    [StringLength(100, ErrorMessage = "Step name cannot exceed 100 characters.")]
    [Display(Name = "Step Name", Description = "Name of the step.")]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the step
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the step.")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of the step
    /// </summary>
    [Display(Name = "Step Type", Description = "Type of the step.")]
    public WorkflowStepType StepType { get; set; } = WorkflowStepType.Approval;

    /// <summary>
    /// Required role or user for this step
    /// </summary>
    [StringLength(100, ErrorMessage = "Required role cannot exceed 100 characters.")]
    [Display(Name = "Required Role", Description = "Required role or user for this step.")]
    public string? RequiredRole { get; set; }

    /// <summary>
    /// Assigned user for this step (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Assigned user cannot exceed 100 characters.")]
    [Display(Name = "Assigned User", Description = "Assigned user for this step.")]
    public string? AssignedUser { get; set; }

    /// <summary>
    /// Time limit for this step in hours
    /// </summary>
    [Range(1, 168, ErrorMessage = "Time limit must be between 1 and 168 hours.")]
    [Display(Name = "Time Limit (Hours)", Description = "Time limit for this step.")]
    public int? TimeLimitHours { get; set; }

    /// <summary>
    /// Indicates if this step is mandatory or optional
    /// </summary>
    [Display(Name = "Is Mandatory", Description = "Indicates if this step is mandatory.")]
    public bool IsMandatory { get; set; } = true;

    /// <summary>
    /// Indicates if multiple approvers are required
    /// </summary>
    [Display(Name = "Requires Multiple Approvers", Description = "Indicates if multiple approvers are required.")]
    public bool RequiresMultipleApprovers { get; set; } = false;

    /// <summary>
    /// Minimum number of approvers required (if multiple)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Min approvers must be between 1 and 10.")]
    [Display(Name = "Min Approvers", Description = "Minimum number of approvers required.")]
    public int? MinApprovers { get; set; }

    /// <summary>
    /// Conditions for this step to be executed (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Conditions cannot exceed 1000 characters.")]
    [Display(Name = "Conditions", Description = "Conditions for this step to be executed.")]
    public string? Conditions { get; set; }

    /// <summary>
    /// Actions to execute when step is completed (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Actions cannot exceed 1000 characters.")]
    [Display(Name = "Actions", Description = "Actions to execute when step is completed.")]
    public string? Actions { get; set; }

    /// <summary>
    /// Next step on approval
    /// </summary>
    [Display(Name = "Next Step On Approval", Description = "Next step on approval.")]
    public int? NextStepOnApproval { get; set; }

    /// <summary>
    /// Next step on rejection
    /// </summary>
    [Display(Name = "Next Step On Rejection", Description = "Next step on rejection.")]
    public int? NextStepOnRejection { get; set; }

    /// <summary>
    /// Workflow steps executed based on this definition
    /// </summary>
    [Display(Name = "Executed Steps", Description = "Workflow steps executed based on this definition.")]
    public ICollection<DocumentWorkflowStep> ExecutedSteps { get; set; } = new List<DocumentWorkflowStep>();
}