using EventForge.DTOs.Common;
using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for DocumentHeader output/display operations.
    /// </summary>
    public class DocumentHeaderDto
    {
        /// <summary>
        /// Unique identifier for the document header.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type of the document.
        /// </summary>
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Document type name for display.
        /// </summary>
        public string? DocumentTypeName { get; set; }

        /// <summary>
        /// Document series for progressive numbering.
        /// </summary>
        public string? Series { get; set; }

        /// <summary>
        /// Document number.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Document date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Customer or supplier.
        /// </summary>
        public Guid BusinessPartyId { get; set; }

        /// <summary>
        /// Business party name for display.
        /// </summary>
        public string? BusinessPartyName { get; set; }

        /// <summary>
        /// Destination address.
        /// </summary>
        public Guid? BusinessPartyAddressId { get; set; }

        /// <summary>
        /// Customer name or reference (free text).
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Source warehouse ID.
        /// </summary>
        public Guid? SourceWarehouseId { get; set; }

        /// <summary>
        /// Source warehouse name for display.
        /// </summary>
        public string? SourceWarehouseName { get; set; }

        /// <summary>
        /// Destination warehouse ID.
        /// </summary>
        public Guid? DestinationWarehouseId { get; set; }

        /// <summary>
        /// Destination warehouse name for display.
        /// </summary>
        public string? DestinationWarehouseName { get; set; }

        /// <summary>
        /// Expected or actual shipping date.
        /// </summary>
        public DateTime? ShippingDate { get; set; }

        /// <summary>
        /// Shipping carrier.
        /// </summary>
        public string? CarrierName { get; set; }

        /// <summary>
        /// Shipping tracking number.
        /// </summary>
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Shipping notes.
        /// </summary>
        public string? ShippingNotes { get; set; }

        /// <summary>
        /// Team member associated with the document.
        /// </summary>
        public Guid? TeamMemberId { get; set; }

        /// <summary>
        /// Team member name for display.
        /// </summary>
        public string? TeamMemberName { get; set; }

        /// <summary>
        /// Team associated with the document.
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Team name for display.
        /// </summary>
        public string? TeamName { get; set; }

        /// <summary>
        /// Event associated with the document.
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Event name for display.
        /// </summary>
        public string? EventName { get; set; }

        /// <summary>
        /// Cash register associated with the document.
        /// </summary>
        public Guid? CashRegisterId { get; set; }

        /// <summary>
        /// Cashier/operator associated with the document.
        /// </summary>
        public Guid? CashierId { get; set; }

        /// <summary>
        /// Cashier name for display.
        /// </summary>
        public string? CashierName { get; set; }

        /// <summary>
        /// Reference to an external document number.
        /// </summary>
        public string? ExternalDocumentNumber { get; set; }

        /// <summary>
        /// Reference to an external document series.
        /// </summary>
        public string? ExternalDocumentSeries { get; set; }

        /// <summary>
        /// Reference to an external document date.
        /// </summary>
        public DateTime? ExternalDocumentDate { get; set; }

        /// <summary>
        /// Reason for the document.
        /// </summary>
        public string? DocumentReason { get; set; }

        /// <summary>
        /// Indicates if the document is a proforma.
        /// </summary>
        public bool IsProforma { get; set; }

        /// <summary>
        /// Indicates if the document is fiscal.
        /// </summary>
        public bool IsFiscal { get; set; }

        /// <summary>
        /// Fiscal document number.
        /// </summary>
        public string? FiscalDocumentNumber { get; set; }

        /// <summary>
        /// Fiscal issue date.
        /// </summary>
        public DateTime? FiscalDate { get; set; }

        /// <summary>
        /// Total VAT amount for the document.
        /// </summary>
        public decimal VatAmount { get; set; }

        /// <summary>
        /// Net total (before VAT).
        /// </summary>
        public decimal TotalNetAmount { get; set; }

        /// <summary>
        /// Gross total (net + VAT).
        /// </summary>
        public decimal TotalGrossAmount { get; set; }

        /// <summary>
        /// Document currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Exchange rate applied if currency is not base.
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Total converted in base currency.
        /// </summary>
        public decimal? BaseCurrencyAmount { get; set; }

        /// <summary>
        /// Payment due date.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Status of the payment.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Amount actually paid.
        /// </summary>
        public decimal AmountPaid { get; set; }

        /// <summary>
        /// Payment method used for the document.
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Reference for the payment.
        /// </summary>
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Overall discount on the document total (percentage).
        /// </summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// Overall discount amount on the document.
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Approval status of the document.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; }

        /// <summary>
        /// User who approved the document.
        /// </summary>
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// Date and time of approval.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Date and time when the document was closed.
        /// </summary>
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Document status.
        /// </summary>
        public DocumentStatus Status { get; set; }

        /// <summary>
        /// Reference to another document.
        /// </summary>
        public Guid? ReferenceDocumentId { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the document was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the document.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the document was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the document.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Document rows.
        /// </summary>
        public List<DocumentRowDto>? Rows { get; set; } = new List<DocumentRowDto>();

        /// <summary>
        /// Total before discounts (calculated).
        /// </summary>
        public decimal TotalBeforeDiscount { get; set; }

        /// <summary>
        /// Total after all discounts (calculated).
        /// </summary>
        public decimal TotalAfterDiscount { get; set; }
    }
}
