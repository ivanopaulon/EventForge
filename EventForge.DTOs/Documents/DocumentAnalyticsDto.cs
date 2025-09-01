using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for document analytics information
    /// </summary>
    public class DocumentAnalyticsDto
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Tenant ID for multi-tenant support
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Reference to the document header
        /// </summary>
        [Required]
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Analytics date (for time-based reporting)
        /// </summary>
        [Required]
        public DateTime AnalyticsDate { get; set; }

        /// <summary>
        /// Document type for grouping analytics
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Business party for customer/supplier analytics
        /// </summary>
        public Guid? BusinessPartyId { get; set; }

        /// <summary>
        /// User who created the document
        /// </summary>
        public string? CreatedByUser { get; set; }

        /// <summary>
        /// Department or organizational unit
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Document processing status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Priority level
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Category for grouping
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Tags for classification
        /// </summary>
        public string? Tags { get; set; }

        // --- Time Metrics ---
        /// <summary>
        /// Time to complete the document workflow (in hours)
        /// </summary>
        public decimal? TimeToCompletionHours { get; set; }

        /// <summary>
        /// Time to first approval (in hours)
        /// </summary>
        public decimal? TimeToFirstApprovalHours { get; set; }

        /// <summary>
        /// Time to closure (in hours)
        /// </summary>
        public decimal? TimeToClosureHours { get; set; }

        /// <summary>
        /// Processing time (active work time in hours)
        /// </summary>
        public decimal? ProcessingTimeHours { get; set; }

        // --- Workflow Metrics ---
        /// <summary>
        /// Number of approval steps required
        /// </summary>
        public int ApprovalStepsRequired { get; set; }

        /// <summary>
        /// Number of approval steps completed
        /// </summary>
        public int ApprovalStepsCompleted { get; set; }

        /// <summary>
        /// Number of approvals received
        /// </summary>
        public int ApprovalsReceived { get; set; }

        /// <summary>
        /// Number of rejections received
        /// </summary>
        public int Rejections { get; set; }

        /// <summary>
        /// Average approval time per step (in hours)
        /// </summary>
        public decimal? AverageApprovalTimeHours { get; set; }

        /// <summary>
        /// Number of escalations occurred
        /// </summary>
        public int Escalations { get; set; }

        // --- Error and Revision Metrics ---
        /// <summary>
        /// Number of errors encountered
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Number of revisions made
        /// </summary>
        public int RevisionCount { get; set; }

        /// <summary>
        /// Number of rework iterations
        /// </summary>
        public int ReworkIterations { get; set; }

        // --- Business Value Metrics ---
        /// <summary>
        /// Document value in currency
        /// </summary>
        public decimal? DocumentValue { get; set; }

        /// <summary>
        /// Processing cost estimate
        /// </summary>
        public decimal? ProcessingCost { get; set; }

        /// <summary>
        /// ROI or efficiency score
        /// </summary>
        public decimal? EfficiencyScore { get; set; }

        // --- Status and Quality Metrics ---
        /// <summary>
        /// Final status of the document workflow
        /// </summary>
        public string? FinalStatus { get; set; }

        /// <summary>
        /// Quality score based on errors, revisions, and completion time
        /// </summary>
        public decimal? QualityScore { get; set; }

        /// <summary>
        /// Compliance score (if applicable)
        /// </summary>
        public decimal? ComplianceScore { get; set; }

        /// <summary>
        /// Customer satisfaction rating
        /// </summary>
        public decimal? SatisfactionRating { get; set; }

        // --- Metadata ---
        /// <summary>
        /// Additional analytics data as JSON
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// Data source for analytics (workflow, manual, external)
        /// </summary>
        public string? DataSource { get; set; } = "workflow";

        /// <summary>
        /// Last analytics update timestamp
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for document analytics summary data
    /// </summary>
    public class DocumentAnalyticsSummaryDto
    {
        /// <summary>
        /// Summary period start date
        /// </summary>
        public DateTime? PeriodStart { get; set; }

        /// <summary>
        /// Summary period end date
        /// </summary>
        public DateTime? PeriodEnd { get; set; }

        /// <summary>
        /// Grouping dimension (time, documentType, department)
        /// </summary>
        public string? GroupBy { get; set; }

        /// <summary>
        /// Total number of documents
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Completed documents count
        /// </summary>
        public int CompletedDocuments { get; set; }

        /// <summary>
        /// Pending documents count
        /// </summary>
        public int PendingDocuments { get; set; }

        /// <summary>
        /// Average completion time in hours
        /// </summary>
        public decimal? AverageCompletionTimeHours { get; set; }

        /// <summary>
        /// Average quality score
        /// </summary>
        public decimal? AverageQualityScore { get; set; }

        /// <summary>
        /// Total document value
        /// </summary>
        public decimal? TotalDocumentValue { get; set; }

        /// <summary>
        /// Summary breakdown by groups
        /// </summary>
        public List<AnalyticsGroupDto> Groups { get; set; } = new List<AnalyticsGroupDto>();
    }

    /// <summary>
    /// DTO for analytics group breakdown
    /// </summary>
    public class AnalyticsGroupDto
    {
        /// <summary>
        /// Group key (e.g., document type name, date, department)
        /// </summary>
        public string GroupKey { get; set; } = string.Empty;

        /// <summary>
        /// Group display label
        /// </summary>
        public string GroupLabel { get; set; } = string.Empty;

        /// <summary>
        /// Document count in this group
        /// </summary>
        public int DocumentCount { get; set; }

        /// <summary>
        /// Average completion time for this group
        /// </summary>
        public decimal? AverageCompletionTime { get; set; }

        /// <summary>
        /// Total value for this group
        /// </summary>
        public decimal? TotalValue { get; set; }

        /// <summary>
        /// Average quality score for this group
        /// </summary>
        public decimal? AverageQuality { get; set; }
    }

    /// <summary>
    /// DTO for document KPI summary
    /// </summary>
    public class DocumentKpiSummaryDto
    {
        /// <summary>
        /// Period start date
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Period end date
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Total documents processed
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Completion rate percentage
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// Average processing time in hours
        /// </summary>
        public decimal AverageProcessingTime { get; set; }

        /// <summary>
        /// Quality score average
        /// </summary>
        public decimal AverageQualityScore { get; set; }

        /// <summary>
        /// Error rate percentage
        /// </summary>
        public decimal ErrorRate { get; set; }

        /// <summary>
        /// Escalation rate percentage
        /// </summary>
        public decimal EscalationRate { get; set; }

        /// <summary>
        /// Customer satisfaction average
        /// </summary>
        public decimal? AverageCustomerSatisfaction { get; set; }

        /// <summary>
        /// Total business value processed
        /// </summary>
        public decimal? TotalBusinessValue { get; set; }

        /// <summary>
        /// Cost efficiency ratio
        /// </summary>
        public decimal? CostEfficiencyRatio { get; set; }
    }
}