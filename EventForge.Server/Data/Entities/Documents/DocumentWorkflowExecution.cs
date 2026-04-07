using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents an execution instance of a workflow for a specific document
/// </summary>
public class DocumentWorkflowExecution : AuditableEntity
{
    /// <summary>
    /// Reference to the document header
    /// </summary>
    [Required(ErrorMessage = "Document header is required.")]
    [Display(Name = "Document Header", Description = "Reference to the document header.")]
    public Guid DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Reference to the workflow definition
    /// </summary>
    [Required(ErrorMessage = "Workflow is required.")]
    [Display(Name = "Workflow", Description = "Reference to the workflow definition.")]
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Navigation property for the workflow
    /// </summary>
    public DocumentWorkflow? Workflow { get; set; }

    /// <summary>
    /// Current workflow state
    /// </summary>
    [Display(Name = "Current State", Description = "Current workflow state.")]
    public WorkflowState CurrentState { get; set; } = WorkflowState.Draft;

    /// <summary>
    /// Current step being executed
    /// </summary>
    [Display(Name = "Current Step Order", Description = "Current step being executed.")]
    public int? CurrentStepOrder { get; set; }

    /// <summary>
    /// Workflow status
    /// </summary>
    [Display(Name = "Status", Description = "Workflow status.")]
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Started;

    /// <summary>
    /// Priority of this workflow execution
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of this workflow execution.")]
    public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

    /// <summary>
    /// Date and time when workflow started
    /// </summary>
    [Display(Name = "Started At", Description = "Date and time when workflow started.")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Date and time when workflow completed
    /// </summary>
    [Display(Name = "Completed At", Description = "Date and time when workflow completed.")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who initiated the workflow
    /// </summary>
    [StringLength(100, ErrorMessage = "Initiator cannot exceed 100 characters.")]
    [Display(Name = "Initiated By", Description = "User who initiated the workflow.")]
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Reason for initiating the workflow
    /// </summary>
    [StringLength(500, ErrorMessage = "Initiation reason cannot exceed 500 characters.")]
    [Display(Name = "Initiation Reason", Description = "Reason for initiating the workflow.")]
    public string? InitiationReason { get; set; }

    /// <summary>
    /// Expected completion date
    /// </summary>
    [Display(Name = "Expected Completion", Description = "Expected completion date.")]
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Actual completion date
    /// </summary>
    [Display(Name = "Actual Completion", Description = "Actual completion date.")]
    public DateTime? ActualCompletionDate { get; set; }

    /// <summary>
    /// Processing time in hours
    /// </summary>
    [Display(Name = "Processing Time (Hours)", Description = "Processing time in hours.")]
    public decimal? ProcessingTimeHours { get; set; }

    /// <summary>
    /// Indicates if workflow has escalated due to delays
    /// </summary>
    [Display(Name = "Is Escalated", Description = "Indicates if workflow has escalated.")]
    public bool IsEscalated { get; set; } = false;

    /// <summary>
    /// Escalation level (0 = no escalation)
    /// </summary>
    [Range(0, 5, ErrorMessage = "Escalation level must be between 0 and 5.")]
    [Display(Name = "Escalation Level", Description = "Escalation level.")]
    public int EscalationLevel { get; set; } = 0;

    /// <summary>
    /// Last escalation date
    /// </summary>
    [Display(Name = "Last Escalated At", Description = "Last escalation date.")]
    public DateTime? LastEscalatedAt { get; set; }

    /// <summary>
    /// Workflow execution context data (JSON)
    /// </summary>
    [Display(Name = "Context Data", Description = "Workflow execution context data.")]
    public string? ContextData { get; set; }

    /// <summary>
    /// Final outcome of the workflow
    /// </summary>
    [StringLength(200, ErrorMessage = "Final outcome cannot exceed 200 characters.")]
    [Display(Name = "Final Outcome", Description = "Final outcome of the workflow.")]
    public string? FinalOutcome { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or comments.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Workflow steps executed in this workflow
    /// </summary>
    [Display(Name = "Workflow Steps", Description = "Workflow steps executed.")]
    public ICollection<DocumentWorkflowStep> WorkflowSteps { get; set; } = new List<DocumentWorkflowStep>();
}

/// <summary>
/// Represents an executed step in a workflow instance
/// </summary>
public class DocumentWorkflowStep : AuditableEntity
{
    /// <summary>
    /// Reference to the workflow execution
    /// </summary>
    [Required(ErrorMessage = "Workflow execution is required.")]
    [Display(Name = "Workflow Execution", Description = "Reference to the workflow execution.")]
    public Guid WorkflowExecutionId { get; set; }

    /// <summary>
    /// Navigation property for the workflow execution
    /// </summary>
    public DocumentWorkflowExecution? WorkflowExecution { get; set; }

    /// <summary>
    /// Reference to the step definition
    /// </summary>
    [Display(Name = "Step Definition", Description = "Reference to the step definition.")]
    public Guid? StepDefinitionId { get; set; }

    /// <summary>
    /// Navigation property for the step definition
    /// </summary>
    public DocumentWorkflowStepDefinition? StepDefinition { get; set; }

    /// <summary>
    /// Reference to the document version (if applicable)
    /// </summary>
    [Display(Name = "Document Version", Description = "Reference to the document version.")]
    public Guid? DocumentVersionId { get; set; }

    /// <summary>
    /// Navigation property for the document version
    /// </summary>
    public DocumentVersion? DocumentVersion { get; set; }

    /// <summary>
    /// Step order in the execution
    /// </summary>
    [Required(ErrorMessage = "Step order is required.")]
    [Range(1, 100, ErrorMessage = "Step order must be between 1 and 100.")]
    [Display(Name = "Step Order", Description = "Step order in the execution.")]
    public int StepOrder { get; set; }

    /// <summary>
    /// Name of the executed step
    /// </summary>
    [Required(ErrorMessage = "Step name is required.")]
    [StringLength(100, ErrorMessage = "Step name cannot exceed 100 characters.")]
    [Display(Name = "Step Name", Description = "Name of the executed step.")]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the executed step
    /// </summary>
    [Display(Name = "Step Type", Description = "Type of the executed step.")]
    public WorkflowStepType StepType { get; set; } = WorkflowStepType.Approval;

    /// <summary>
    /// Status of the step execution
    /// </summary>
    [Display(Name = "Status", Description = "Status of the step execution.")]
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.Pending;

    /// <summary>
    /// User assigned to this step
    /// </summary>
    [StringLength(100, ErrorMessage = "Assigned user cannot exceed 100 characters.")]
    [Display(Name = "Assigned User", Description = "User assigned to this step.")]
    public string? AssignedUser { get; set; }

    /// <summary>
    /// User who actually executed/completed this step
    /// </summary>
    [StringLength(100, ErrorMessage = "Executed by cannot exceed 100 characters.")]
    [Display(Name = "Executed By", Description = "User who executed this step.")]
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Date and time when step was started
    /// </summary>
    [Display(Name = "Started At", Description = "Date and time when step was started.")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Date and time when step was completed
    /// </summary>
    [Display(Name = "Completed At", Description = "Date and time when step was completed.")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Due date for this step
    /// </summary>
    [Display(Name = "Due Date", Description = "Due date for this step.")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Processing time for this step in hours
    /// </summary>
    [Display(Name = "Processing Time (Hours)", Description = "Processing time in hours.")]
    public decimal? ProcessingTimeHours { get; set; }

    /// <summary>
    /// Decision made in this step
    /// </summary>
    [StringLength(50, ErrorMessage = "Decision cannot exceed 50 characters.")]
    [Display(Name = "Decision", Description = "Decision made in this step.")]
    public string? Decision { get; set; }

    /// <summary>
    /// Comments provided during step execution
    /// </summary>
    [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
    [Display(Name = "Comments", Description = "Comments provided during execution.")]
    public string? Comments { get; set; }

    /// <summary>
    /// Data captured during step execution (JSON)
    /// </summary>
    [Display(Name = "Step Data", Description = "Data captured during step execution.")]
    public string? StepData { get; set; }

    /// <summary>
    /// Attachments or files related to this step
    /// </summary>
    [StringLength(500, ErrorMessage = "Attachments cannot exceed 500 characters.")]
    [Display(Name = "Attachments", Description = "Attachments or files related to this step.")]
    public string? Attachments { get; set; }

    /// <summary>
    /// Error message if step failed
    /// </summary>
    [StringLength(500, ErrorMessage = "Error message cannot exceed 500 characters.")]
    [Display(Name = "Error Message", Description = "Error message if step failed.")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Retry count for this step
    /// </summary>
    [Range(0, 10, ErrorMessage = "Retry count must be between 0 and 10.")]
    [Display(Name = "Retry Count", Description = "Retry count for this step.")]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Next step order to execute
    /// </summary>
    [Display(Name = "Next Step Order", Description = "Next step order to execute.")]
    public int? NextStepOrder { get; set; }

    /// <summary>
    /// Indicates if this step was escalated
    /// </summary>
    [Display(Name = "Is Escalated", Description = "Indicates if this step was escalated.")]
    public bool IsEscalated { get; set; } = false;

    /// <summary>
    /// Escalation date
    /// </summary>
    [Display(Name = "Escalated At", Description = "Escalation date.")]
    public DateTime? EscalatedAt { get; set; }
}