using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{

    /// <summary>
    /// DTO for creating a new document header.
    /// </summary>
    public class CreateDocumentHeaderDto
    {
        /// <summary>
        /// Type of the document.
        /// </summary>
        [Required(ErrorMessage = "Document type is required.")]
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Document series for progressive numbering.
        /// </summary>
        [StringLength(10, ErrorMessage = "Series cannot exceed 10 characters.")]
        public string? Series { get; set; }

        /// <summary>
        /// Document number. If not provided, it will be auto-generated based on the document type counter.
        /// </summary>
        [StringLength(30, ErrorMessage = "Number cannot exceed 30 characters.")]
        public string? Number { get; set; }

        /// <summary>
        /// Document date.
        /// </summary>
        [Required(ErrorMessage = "Document date is required.")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Customer or supplier.
        /// </summary>
        [Required(ErrorMessage = "Business party is required.")]
        public Guid BusinessPartyId { get; set; }

        /// <summary>
        /// Destination address.
        /// </summary>
        public Guid? BusinessPartyAddressId { get; set; }

        /// <summary>
        /// Customer name or reference (free text).
        /// </summary>
        [MaxLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Source warehouse ID.
        /// </summary>
        public Guid? SourceWarehouseId { get; set; }

        /// <summary>
        /// Destination warehouse ID.
        /// </summary>
        public Guid? DestinationWarehouseId { get; set; }

        /// <summary>
        /// Expected or actual shipping date.
        /// </summary>
        public DateTime? ShippingDate { get; set; }

        /// <summary>
        /// Shipping carrier.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Carrier name cannot exceed 100 characters.")]
        public string? CarrierName { get; set; }

        /// <summary>
        /// Shipping tracking number.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Tracking number cannot exceed 50 characters.")]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Shipping notes.
        /// </summary>
        [MaxLength(200, ErrorMessage = "Shipping notes cannot exceed 200 characters.")]
        public string? ShippingNotes { get; set; }

        /// <summary>
        /// Team member associated with the document.
        /// </summary>
        public Guid? TeamMemberId { get; set; }

        /// <summary>
        /// Team associated with the document.
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Event associated with the document.
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Cash register associated with the document.
        /// </summary>
        public Guid? CashRegisterId { get; set; }

        /// <summary>
        /// Cashier/operator associated with the document.
        /// </summary>
        public Guid? CashierId { get; set; }

        /// <summary>
        /// Reference to an external document number.
        /// </summary>
        [StringLength(30, ErrorMessage = "External document number cannot exceed 30 characters.")]
        public string? ExternalDocumentNumber { get; set; }

        /// <summary>
        /// Reference to an external document series.
        /// </summary>
        [StringLength(10, ErrorMessage = "External document series cannot exceed 10 characters.")]
        public string? ExternalDocumentSeries { get; set; }

        /// <summary>
        /// Reference to an external document date.
        /// </summary>
        public DateTime? ExternalDocumentDate { get; set; }

        /// <summary>
        /// Reason for the document.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Reason cannot exceed 100 characters.")]
        public string? DocumentReason { get; set; }

        /// <summary>
        /// Indicates if the document is a proforma.
        /// </summary>
        public bool IsProforma { get; set; } = false;

        /// <summary>
        /// Indicates if the document is fiscal.
        /// </summary>
        public bool IsFiscal { get; set; } = true;

        /// <summary>
        /// Fiscal document number.
        /// </summary>
        [StringLength(30, ErrorMessage = "Fiscal document number cannot exceed 30 characters.")]
        public string? FiscalDocumentNumber { get; set; }

        /// <summary>
        /// Fiscal issue date.
        /// </summary>
        public DateTime? FiscalDate { get; set; }

        /// <summary>
        /// Document currency code.
        /// </summary>
        [StringLength(3, ErrorMessage = "Currency code cannot exceed 3 characters.")]
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Exchange rate applied if currency is not base.
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Payment due date.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Payment method used for the document.
        /// </summary>
        [MaxLength(30, ErrorMessage = "Payment method cannot exceed 30 characters.")]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Reference for the payment.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Payment reference cannot exceed 50 characters.")]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Overall discount on the document total (percentage).
        /// </summary>
        [Range(0, 100, ErrorMessage = "Total discount must be between 0 and 100.")]
        public decimal TotalDiscount { get; set; } = 0m;

        /// <summary>
        /// Overall discount amount on the document.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Total discount amount must be non-negative.")]
        public decimal TotalDiscountAmount { get; set; } = 0m;

        /// <summary>
        /// Reference to another document.
        /// </summary>
        public Guid? ReferenceDocumentId { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Document rows to create with the header.
        /// </summary>
        public List<CreateDocumentRowDto>? Rows { get; set; }
    }
}
