using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Documents;


/// <summary>
/// Represents a document header (e.g., order, invoice, delivery note, transfer, summary invoice).
/// </summary>
public class DocumentHeader : AuditableEntity
{
    // --- Identifiers and references ---
    /// <summary>
    /// Type of the document.
    /// </summary>
    [Required(ErrorMessage = "Document type is required.")]
    [Display(Name = "Document Type", Description = "Type of the document.")]
    public Guid DocumentTypeId { get; set; } = Guid.Empty;

    /// <summary>
    /// Navigation property for the document type.
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Document series for progressive numbering.
    /// </summary>
    [StringLength(10, ErrorMessage = "Series cannot exceed 10 characters.")]
    [Display(Name = "Series", Description = "Document series for progressive numbering.")]
    public string? Series { get; set; } = string.Empty;

    /// <summary>
    /// Document number.
    /// </summary>
    [Required(ErrorMessage = "Document number is required.")]
    [StringLength(30, ErrorMessage = "Number cannot exceed 30 characters.")]
    [Display(Name = "Number", Description = "Document number.")]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Document date.
    /// </summary>
    [Required(ErrorMessage = "Document date is required.")]
    [Display(Name = "Date", Description = "Document date.")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // --- Customer/Supplier info ---
    /// <summary>
    /// Customer or supplier.
    /// </summary>
    [Required(ErrorMessage = "Business party is required.")]
    [Display(Name = "Business Party", Description = "Customer or supplier.")]
    public Guid BusinessPartyId { get; set; } = Guid.Empty;

    /// <summary>
    /// Navigation property for the business party.
    /// </summary>
    public BusinessParty? BusinessParty { get; set; }

    /// <summary>
    /// Destination address.
    /// </summary>
    [Display(Name = "Business Party Address", Description = "Destination address.")]
    public Guid? BusinessPartyAddressId { get; set; }

    /// <summary>
    /// Navigation property for the destination address.
    /// </summary>
    public Address? BusinessPartyAddress { get; set; }

    /// <summary>
    /// Customer name or reference (free text).
    /// </summary>
    [MaxLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name", Description = "Customer name or reference (free text).")]
    public string? CustomerName { get; set; } = string.Empty;

    // --- Warehouse and logistics ---
    [Display(Name = "Source Warehouse", Description = "Warehouse from which stock is moved.")]
    public Guid? SourceWarehouseId { get; set; }
    public StorageFacility? SourceWarehouse { get; set; }

    [Display(Name = "Destination Warehouse", Description = "Warehouse to which stock is moved.")]
    public Guid? DestinationWarehouseId { get; set; }
    public StorageFacility? DestinationWarehouse { get; set; }

    [Display(Name = "Shipping Date", Description = "Expected or actual shipping date.")]
    public DateTime? ShippingDate { get; set; }

    [MaxLength(100, ErrorMessage = "Carrier name cannot exceed 100 characters.")]
    [Display(Name = "Carrier", Description = "Shipping carrier.")]
    public string? CarrierName { get; set; }

    [MaxLength(50, ErrorMessage = "Tracking number cannot exceed 50 characters.")]
    [Display(Name = "Tracking Number", Description = "Shipping tracking number.")]
    public string? TrackingNumber { get; set; }

    [MaxLength(200, ErrorMessage = "Shipping notes cannot exceed 200 characters.")]
    [Display(Name = "Shipping Notes", Description = "Shipping notes.")]
    public string? ShippingNotes { get; set; }

    // --- Team, event, cash register, operator ---
    [Display(Name = "Team Member", Description = "Team member associated with the document.")]
    public Guid? TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }

    [Display(Name = "Team", Description = "Team associated with the document.")]
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    [Display(Name = "Event", Description = "Event associated with the document.")]
    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    [Display(Name = "Cash Register", Description = "Cash register associated with the document.")]
    public Guid? CashRegisterId { get; set; }
    public StorePos? CashRegister { get; set; }

    [Display(Name = "Cashier", Description = "Cashier/operator associated with the document.")]
    public Guid? CashierId { get; set; }
    public StoreUser? Cashier { get; set; }

    // --- External document and reason ---
    [StringLength(30, ErrorMessage = "External document number cannot exceed 30 characters.")]
    [Display(Name = "External Document Number", Description = "Reference to an external document number.")]
    public string? ExternalDocumentNumber { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "External document series cannot exceed 10 characters.")]
    [Display(Name = "External Document Series", Description = "Reference to an external document series.")]
    public string? ExternalDocumentSeries { get; set; } = string.Empty;

    [Display(Name = "External Document Date", Description = "Reference to an external document date.")]
    public DateTime? ExternalDocumentDate { get; set; }

    [MaxLength(100, ErrorMessage = "Reason cannot exceed 100 characters.")]
    [Display(Name = "Reason", Description = "Reason for the document (e.g., sale, return, transfer, etc.).")]
    public string? DocumentReason { get; set; } = string.Empty;

    // --- Fiscal and accounting ---
    [Display(Name = "Is Proforma", Description = "Indicates if the document is a proforma (not fiscal).")]
    public bool IsProforma { get; set; } = false;

    [Display(Name = "Is Fiscal", Description = "Indicates if the document is fiscal.")]
    public bool IsFiscal { get; set; } = true;

    [StringLength(30, ErrorMessage = "Fiscal document number cannot exceed 30 characters.")]
    [Display(Name = "Fiscal Document Number", Description = "Fiscal document number (if different from internal number).")]
    public string? FiscalDocumentNumber { get; set; }

    [Display(Name = "Fiscal Date", Description = "Fiscal issue date (if different from document date).")]
    public DateTime? FiscalDate { get; set; }

    [Display(Name = "VAT Amount", Description = "Total VAT amount for the document.")]
    public decimal VatAmount { get; set; } = 0m;

    [Display(Name = "Net Total", Description = "Net total (before VAT).")]
    public decimal TotalNetAmount { get; set; } = 0m;

    [Display(Name = "Gross Total", Description = "Gross total (net + VAT).")]
    public decimal TotalGrossAmount { get; set; } = 0m;

    // --- Payments ---
    [StringLength(3, ErrorMessage = "Currency code cannot exceed 3 characters.")]
    [Display(Name = "Currency", Description = "Document currency code.")]
    public string Currency { get; set; } = "EUR";

    [Display(Name = "Exchange Rate", Description = "Exchange rate applied if currency is not base.")]
    public decimal? ExchangeRate { get; set; }

    [Display(Name = "Base Currency Amount", Description = "Total converted in base currency.")]
    public decimal? BaseCurrencyAmount { get; set; }

    [Display(Name = "Due Date", Description = "Payment due date.")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Payment Status", Description = "Status of the payment.")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [Display(Name = "Amount Paid", Description = "Amount actually paid.")]
    public decimal AmountPaid { get; set; } = 0m;

    [MaxLength(30, ErrorMessage = "Payment method cannot exceed 30 characters.")]
    [Display(Name = "Payment Method", Description = "Payment method used for the document.")]
    public string? PaymentMethod { get; set; }

    [MaxLength(50, ErrorMessage = "Payment reference cannot exceed 50 characters.")]
    [Display(Name = "Payment Reference", Description = "Reference for the payment (e.g., transaction ID).")]
    public string? PaymentReference { get; set; }

    // --- Discounts ---
    [Range(0, 100, ErrorMessage = "Total discount must be between 0 and 100.")]
    [Display(Name = "Total Discount (%)", Description = "Overall discount on the document total (percentage).")]
    public decimal TotalDiscount { get; set; } = 0m;

    [Range(0, double.MaxValue, ErrorMessage = "Total discount amount must be non-negative.")]
    [Display(Name = "Total Discount Amount", Description = "Overall discount amount on the document (absolute value).")]
    public decimal TotalDiscountAmount { get; set; } = 0m;

    // --- Workflow and approval ---
    [Display(Name = "Approval Status", Description = "Approval status of the document.")]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.None;

    [MaxLength(100, ErrorMessage = "Approved by cannot exceed 100 characters.")]
    [Display(Name = "Approved By", Description = "User who approved the document.")]
    public string? ApprovedBy { get; set; }

    [Display(Name = "Approved At", Description = "Date and time of approval.")]
    public DateTime? ApprovedAt { get; set; }

    // --- Status and closure ---
    [Display(Name = "Closed At", Description = "Date and time when the document was closed.")]
    public DateTime? ClosedAt { get; set; }

    [Display(Name = "Status", Description = "Document status.")]
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    // --- Document links ---
    [Display(Name = "Reference Document", Description = "Reference to another document (for links, corrections, etc.).")]
    public Guid? ReferenceDocumentId { get; set; }
    public DocumentHeader? ReferenceDocument { get; set; }

    /// <summary>
    /// List of detailed documents linked to this summary document (e.g., DDTs for a summary invoice).
    /// </summary>
    public ICollection<DocumentSummaryLink> SummaryDocuments { get; set; } = new List<DocumentSummaryLink>();

    /// <summary>
    /// List of summary documents this document is included in (reverse navigation).
    /// </summary>
    public ICollection<DocumentSummaryLink> IncludedInSummaries { get; set; } = new List<DocumentSummaryLink>();

    // --- Rows and notes ---
    [Display(Name = "Rows", Description = "Document rows.")]
    public List<DocumentRow> Rows { get; set; } = new();

    /// <summary>
    /// Document attachments linked to this header
    /// </summary>
    [Display(Name = "Attachments", Description = "Document attachments linked to this header.")]
    public ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();

    /// <summary>
    /// Document comments linked to this header
    /// </summary>
    [Display(Name = "Comments", Description = "Document comments linked to this header.")]
    public ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();

    // --- Template and recurrence tracking ---
    /// <summary>
    /// Template used to create this document (if any)
    /// </summary>
    [Display(Name = "Source Template", Description = "Template used to create this document.")]
    public Guid? SourceTemplateId { get; set; }

    /// <summary>
    /// Navigation property for the source template
    /// </summary>
    public DocumentTemplate? SourceTemplate { get; set; }

    /// <summary>
    /// Recurring schedule that generated this document (if any)
    /// </summary>
    [Display(Name = "Source Recurrence", Description = "Recurring schedule that generated this document.")]
    public Guid? SourceRecurrenceId { get; set; }

    /// <summary>
    /// Navigation property for the source recurrence
    /// </summary>
    public DocumentRecurrence? SourceRecurrence { get; set; }

    // --- Versioning and workflow ---
    /// <summary>
    /// Current version number of this document
    /// </summary>
    [Display(Name = "Current Version", Description = "Current version number of this document.")]
    public int CurrentVersionNumber { get; set; } = 1;

    /// <summary>
    /// Indicates if versioning is enabled for this document
    /// </summary>
    [Display(Name = "Versioning Enabled", Description = "Indicates if versioning is enabled for this document.")]
    public bool VersioningEnabled { get; set; } = false;

    /// <summary>
    /// Current workflow execution for this document
    /// </summary>
    [Display(Name = "Current Workflow Execution", Description = "Current workflow execution for this document.")]
    public Guid? CurrentWorkflowExecutionId { get; set; }

    /// <summary>
    /// Navigation property for the current workflow execution
    /// </summary>
    public DocumentWorkflowExecution? CurrentWorkflowExecution { get; set; }

    /// <summary>
    /// Document versions
    /// </summary>
    [Display(Name = "Document Versions", Description = "Document versions.")]
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    /// <summary>
    /// Workflow executions for this document
    /// </summary>
    [Display(Name = "Workflow Executions", Description = "Workflow executions for this document.")]
    public ICollection<DocumentWorkflowExecution> WorkflowExecutions { get; set; } = new List<DocumentWorkflowExecution>();

    // --- Analytics and scheduling ---
    /// <summary>
    /// Analytics data for this document
    /// </summary>
    [Display(Name = "Analytics", Description = "Analytics data for this document.")]
    public DocumentAnalytics? Analytics { get; set; }

    /// <summary>
    /// Reminders associated with this document
    /// </summary>
    [Display(Name = "Reminders", Description = "Reminders associated with this document.")]
    public ICollection<DocumentReminder> Reminders { get; set; } = new List<DocumentReminder>();

    /// <summary>
    /// Schedules associated with this document
    /// </summary>
    [Display(Name = "Schedules", Description = "Schedules associated with this document.")]
    public ICollection<DocumentSchedule> Schedules { get; set; } = new List<DocumentSchedule>();

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;

    // --- Collaboration and Lock Management ---
    /// <summary>
    /// User currently editing this document (email/username).
    /// </summary>
    [StringLength(256)]
    [Display(Name = "Locked By", Description = "User currently editing this document.")]
    public string? LockedBy { get; set; }

    /// <summary>
    /// Timestamp when lock was acquired.
    /// </summary>
    [Display(Name = "Locked At", Description = "Timestamp when lock was acquired.")]
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// SignalR connection ID of user holding the lock.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Lock Connection ID", Description = "SignalR connection ID holding the lock.")]
    public string? LockConnectionId { get; set; }

    // --- Calculated properties ---
    [NotMapped]
    [Display(Name = "Total Before Discounts", Description = "Total before discounts.")]
    public decimal TotalBeforeDiscount => Rows?.Sum(r => r.UnitPrice * r.Quantity) ?? 0m;

    [NotMapped]
    [Display(Name = "Total After Discounts", Description = "Total after all discounts.")]
    public decimal TotalAfterDiscount
    {
        get
        {
            var total = Rows?.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m))) ?? 0m;
            total -= TotalDiscountAmount;
            if (TotalDiscount > 0)
                total -= total * (TotalDiscount / 100m);
            return total < 0 ? 0 : total;
        }
    }
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue
}

/// <summary>
/// Approval status enumeration.
/// </summary>
public enum ApprovalStatus
{
    None,
    Pending,
    Approved,
    Rejected
}