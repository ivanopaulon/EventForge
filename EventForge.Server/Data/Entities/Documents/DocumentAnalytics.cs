using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents analytics and KPI tracking data for documents
/// </summary>
public class DocumentAnalytics : AuditableEntity
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
    /// Analytics date (for time-based reporting)
    /// </summary>
    [Required(ErrorMessage = "Analytics date is required.")]
    [Display(Name = "Analytics Date", Description = "Analytics date for time-based reporting.")]
    public DateTime AnalyticsDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Document type for grouping analytics
    /// </summary>
    [Display(Name = "Document Type", Description = "Document type for grouping analytics.")]
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Business party for customer/supplier analytics
    /// </summary>
    [Display(Name = "Business Party", Description = "Business party for analytics.")]
    public Guid? BusinessPartyId { get; set; }

    /// <summary>
    /// User who created the document (for user analytics)
    /// </summary>
    [StringLength(100, ErrorMessage = "Creator cannot exceed 100 characters.")]
    [Display(Name = "Document Creator", Description = "User who created the document.")]
    public string? DocumentCreator { get; set; }

    /// <summary>
    /// Department or team associated with the document
    /// </summary>
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    [Display(Name = "Department", Description = "Department or team associated with the document.")]
    public string? Department { get; set; }

    // --- Cycle Time Metrics ---
    /// <summary>
    /// Time from document creation to first approval (in hours)
    /// </summary>
    [Display(Name = "Time To First Approval (Hours)", Description = "Time from creation to first approval.")]
    public decimal? TimeToFirstApprovalHours { get; set; }

    /// <summary>
    /// Time from document creation to final approval (in hours)
    /// </summary>
    [Display(Name = "Time To Final Approval (Hours)", Description = "Time from creation to final approval.")]
    public decimal? TimeToFinalApprovalHours { get; set; }

    /// <summary>
    /// Time from document creation to closure (in hours)
    /// </summary>
    [Display(Name = "Time To Closure (Hours)", Description = "Time from creation to closure.")]
    public decimal? TimeToClosureHours { get; set; }

    /// <summary>
    /// Total processing time including all revisions (in hours)
    /// </summary>
    [Display(Name = "Total Processing Time (Hours)", Description = "Total processing time including revisions.")]
    public decimal? TotalProcessingTimeHours { get; set; }

    // --- Approval Metrics ---
    /// <summary>
    /// Number of approval steps required
    /// </summary>
    [Range(0, 100, ErrorMessage = "Approval steps must be between 0 and 100.")]
    [Display(Name = "Approval Steps Required", Description = "Number of approval steps required.")]
    public int ApprovalStepsRequired { get; set; } = 0;

    /// <summary>
    /// Number of approval steps completed
    /// </summary>
    [Range(0, 100, ErrorMessage = "Approval steps completed must be between 0 and 100.")]
    [Display(Name = "Approval Steps Completed", Description = "Number of approval steps completed.")]
    public int ApprovalStepsCompleted { get; set; } = 0;

    /// <summary>
    /// Number of approvals received
    /// </summary>
    [Range(0, 100, ErrorMessage = "Approvals received must be between 0 and 100.")]
    [Display(Name = "Approvals Received", Description = "Number of approvals received.")]
    public int ApprovalsReceived { get; set; } = 0;

    /// <summary>
    /// Number of rejections received
    /// </summary>
    [Range(0, 100, ErrorMessage = "Rejections must be between 0 and 100.")]
    [Display(Name = "Rejections", Description = "Number of rejections received.")]
    public int Rejections { get; set; } = 0;

    /// <summary>
    /// Average approval time per step (in hours)
    /// </summary>
    [Display(Name = "Average Approval Time (Hours)", Description = "Average approval time per step.")]
    public decimal? AverageApprovalTimeHours { get; set; }

    /// <summary>
    /// Number of escalations occurred
    /// </summary>
    [Range(0, 10, ErrorMessage = "Escalations must be between 0 and 10.")]
    [Display(Name = "Escalations", Description = "Number of escalations occurred.")]
    public int Escalations { get; set; } = 0;

    // --- Error and Revision Metrics ---
    /// <summary>
    /// Number of revisions made to the document
    /// </summary>
    [Range(0, 100, ErrorMessage = "Revisions must be between 0 and 100.")]
    [Display(Name = "Revisions", Description = "Number of revisions made.")]
    public int Revisions { get; set; } = 0;

    /// <summary>
    /// Number of errors detected during processing
    /// </summary>
    [Range(0, 100, ErrorMessage = "Errors must be between 0 and 100.")]
    [Display(Name = "Errors", Description = "Number of errors detected.")]
    public int Errors { get; set; } = 0;

    /// <summary>
    /// Number of comments added to the document
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Comments must be between 0 and 1000.")]
    [Display(Name = "Comments Count", Description = "Number of comments added.")]
    public int CommentsCount { get; set; } = 0;

    /// <summary>
    /// Number of attachments added to the document
    /// </summary>
    [Range(0, 100, ErrorMessage = "Attachments must be between 0 and 100.")]
    [Display(Name = "Attachments Count", Description = "Number of attachments added.")]
    public int AttachmentsCount { get; set; } = 0;

    /// <summary>
    /// Number of versions created
    /// </summary>
    [Range(0, 100, ErrorMessage = "Versions must be between 0 and 100.")]
    [Display(Name = "Versions Count", Description = "Number of versions created.")]
    public int VersionsCount { get; set; } = 1;

    // --- Business Value Metrics ---
    /// <summary>
    /// Document value (total amount if applicable)
    /// </summary>
    [Display(Name = "Document Value", Description = "Document value or total amount.")]
    public decimal? DocumentValue { get; set; }

    /// <summary>
    /// Currency of the document value
    /// </summary>
    [StringLength(3, ErrorMessage = "Currency code cannot exceed 3 characters.")]
    [Display(Name = "Currency", Description = "Currency of the document value.")]
    public string? Currency { get; set; }

    /// <summary>
    /// Cost of processing this document (calculated)
    /// </summary>
    [Display(Name = "Processing Cost", Description = "Cost of processing this document.")]
    public decimal? ProcessingCost { get; set; }

    /// <summary>
    /// ROI or efficiency score
    /// </summary>
    [Range(0, 100, ErrorMessage = "Efficiency score must be between 0 and 100.")]
    [Display(Name = "Efficiency Score", Description = "ROI or efficiency score.")]
    public decimal? EfficiencyScore { get; set; }

    // --- Status and Quality Metrics ---
    /// <summary>
    /// Final status of the document workflow
    /// </summary>
    [Display(Name = "Final Status", Description = "Final status of the document workflow.")]
    public WorkflowExecutionStatus? FinalStatus { get; set; }

    /// <summary>
    /// Quality score based on errors, revisions, and completion time
    /// </summary>
    [Range(0, 100, ErrorMessage = "Quality score must be between 0 and 100.")]
    [Display(Name = "Quality Score", Description = "Quality score based on errors and revisions.")]
    public decimal? QualityScore { get; set; }

    /// <summary>
    /// Compliance score for regulatory requirements
    /// </summary>
    [Range(0, 100, ErrorMessage = "Compliance score must be between 0 and 100.")]
    [Display(Name = "Compliance Score", Description = "Compliance score for regulatory requirements.")]
    public decimal? ComplianceScore { get; set; }

    /// <summary>
    /// Customer satisfaction score (if applicable)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Satisfaction score must be between 0 and 100.")]
    [Display(Name = "Satisfaction Score", Description = "Customer satisfaction score.")]
    public decimal? SatisfactionScore { get; set; }

    // --- Timing Metrics ---
    /// <summary>
    /// Indicates if document was completed on time
    /// </summary>
    [Display(Name = "Completed On Time", Description = "Indicates if document was completed on time.")]
    public bool? CompletedOnTime { get; set; }

    /// <summary>
    /// Variance from expected completion time (in hours, negative = early, positive = late)
    /// </summary>
    [Display(Name = "Time Variance (Hours)", Description = "Variance from expected completion time.")]
    public decimal? TimeVarianceHours { get; set; }

    /// <summary>
    /// Peak processing load during document lifecycle
    /// </summary>
    [Range(0, 100, ErrorMessage = "Peak load must be between 0 and 100.")]
    [Display(Name = "Peak Load", Description = "Peak processing load during lifecycle.")]
    public decimal? PeakLoad { get; set; }

    // --- Additional Metrics ---
    /// <summary>
    /// Number of users involved in the document process
    /// </summary>
    [Range(0, 100, ErrorMessage = "Users involved must be between 0 and 100.")]
    [Display(Name = "Users Involved", Description = "Number of users involved in the process.")]
    public int UsersInvolved { get; set; } = 1;

    /// <summary>
    /// Number of external systems integrated
    /// </summary>
    [Range(0, 20, ErrorMessage = "External systems must be between 0 and 20.")]
    [Display(Name = "External Systems", Description = "Number of external systems integrated.")]
    public int ExternalSystems { get; set; } = 0;

    /// <summary>
    /// Number of notifications sent during processing
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Notifications must be between 0 and 1000.")]
    [Display(Name = "Notifications Sent", Description = "Number of notifications sent.")]
    public int NotificationsSent { get; set; } = 0;

    /// <summary>
    /// Additional analytics data as JSON
    /// </summary>
    [Display(Name = "Additional Data", Description = "Additional analytics data as JSON.")]
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Analytics category for reporting grouping
    /// </summary>
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    [Display(Name = "Analytics Category", Description = "Analytics category for reporting.")]
    public string? AnalyticsCategory { get; set; }

    /// <summary>
    /// Tags for flexible analytics filtering
    /// </summary>
    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters.")]
    [Display(Name = "Tags", Description = "Tags for flexible analytics filtering.")]
    public string? Tags { get; set; }
}

/// <summary>
/// Represents aggregated analytics data for dashboard reporting
/// </summary>
public class DocumentAnalyticsSummary : AuditableEntity
{
    /// <summary>
    /// Summary period (daily, weekly, monthly, yearly)
    /// </summary>
    [Required(ErrorMessage = "Summary period is required.")]
    [StringLength(20, ErrorMessage = "Summary period cannot exceed 20 characters.")]
    [Display(Name = "Summary Period", Description = "Summary period type.")]
    public string SummaryPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the summary period
    /// </summary>
    [Required(ErrorMessage = "Period start is required.")]
    [Display(Name = "Period Start", Description = "Start date of the summary period.")]
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of the summary period
    /// </summary>
    [Required(ErrorMessage = "Period end is required.")]
    [Display(Name = "Period End", Description = "End date of the summary period.")]
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Document type for this summary (null for all types)
    /// </summary>
    [Display(Name = "Document Type", Description = "Document type for this summary.")]
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Department for this summary (null for all departments)
    /// </summary>
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    [Display(Name = "Department", Description = "Department for this summary.")]
    public string? Department { get; set; }

    /// <summary>
    /// Total number of documents processed
    /// </summary>
    [Display(Name = "Total Documents", Description = "Total number of documents processed.")]
    public int TotalDocuments { get; set; } = 0;

    /// <summary>
    /// Number of completed documents
    /// </summary>
    [Display(Name = "Completed Documents", Description = "Number of completed documents.")]
    public int CompletedDocuments { get; set; } = 0;

    /// <summary>
    /// Number of pending documents
    /// </summary>
    [Display(Name = "Pending Documents", Description = "Number of pending documents.")]
    public int PendingDocuments { get; set; } = 0;

    /// <summary>
    /// Number of rejected documents
    /// </summary>
    [Display(Name = "Rejected Documents", Description = "Number of rejected documents.")]
    public int RejectedDocuments { get; set; } = 0;

    /// <summary>
    /// Average processing time in hours
    /// </summary>
    [Display(Name = "Average Processing Time (Hours)", Description = "Average processing time.")]
    public decimal? AverageProcessingTimeHours { get; set; }

    /// <summary>
    /// Average approval time in hours
    /// </summary>
    [Display(Name = "Average Approval Time (Hours)", Description = "Average approval time.")]
    public decimal? AverageApprovalTimeHours { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    [Range(0, 100, ErrorMessage = "Success rate must be between 0 and 100.")]
    [Display(Name = "Success Rate", Description = "Success rate percentage.")]
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// Quality score average
    /// </summary>
    [Range(0, 100, ErrorMessage = "Quality score must be between 0 and 100.")]
    [Display(Name = "Average Quality Score", Description = "Quality score average.")]
    public decimal? AverageQualityScore { get; set; }

    /// <summary>
    /// Total value processed
    /// </summary>
    [Display(Name = "Total Value", Description = "Total value processed.")]
    public decimal? TotalValue { get; set; }

    /// <summary>
    /// Total processing cost
    /// </summary>
    [Display(Name = "Total Cost", Description = "Total processing cost.")]
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Summary data as JSON for additional metrics
    /// </summary>
    [Display(Name = "Summary Data", Description = "Summary data as JSON.")]
    public string? SummaryData { get; set; }
}