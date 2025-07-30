using System;
using System.ComponentModel.DataAnnotations;

using EventForge.DTOs.Common;
namespace EventForge.DTOs.Documents
{
    
    /// <summary>
    /// Query parameters for filtering document headers.
    /// </summary>
    public class DocumentHeaderQueryParameters
    {
        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;
    
        /// <summary>
        /// Page size for pagination.
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    
        /// <summary>
        /// Filter by document type ID.
        /// </summary>
        public Guid? DocumentTypeId { get; set; }
    
        /// <summary>
        /// Filter by document number (partial match).
        /// </summary>
        public string? Number { get; set; }
    
        /// <summary>
        /// Filter by document series.
        /// </summary>
        public string? Series { get; set; }
    
        /// <summary>
        /// Filter documents from this date.
        /// </summary>
        public DateTime? FromDate { get; set; }
    
        /// <summary>
        /// Filter documents until this date.
        /// </summary>
        public DateTime? ToDate { get; set; }
    
        /// <summary>
        /// Filter by business party ID.
        /// </summary>
        public Guid? BusinessPartyId { get; set; }
    
        /// <summary>
        /// Filter by customer name (partial match).
        /// </summary>
        public string? CustomerName { get; set; }
    
        /// <summary>
        /// Filter by document status.
        /// </summary>
        public DocumentStatus? Status { get; set; }
    
        /// <summary>
        /// Filter by payment status.
        /// </summary>
        public PaymentStatus? PaymentStatus { get; set; }
    
        /// <summary>
        /// Filter by approval status.
        /// </summary>
        public ApprovalStatus? ApprovalStatus { get; set; }
    
        /// <summary>
        /// Filter by team ID.
        /// </summary>
        public Guid? TeamId { get; set; }
    
        /// <summary>
        /// Filter by event ID.
        /// </summary>
        public Guid? EventId { get; set; }
    
        /// <summary>
        /// Filter by source warehouse ID.
        /// </summary>
        public Guid? SourceWarehouseId { get; set; }
    
        /// <summary>
        /// Filter by destination warehouse ID.
        /// </summary>
        public Guid? DestinationWarehouseId { get; set; }
    
        /// <summary>
        /// Filter by fiscal document flag.
        /// </summary>
        public bool? IsFiscal { get; set; }
    
        /// <summary>
        /// Filter by proforma document flag.
        /// </summary>
        public bool? IsProforma { get; set; }
    
        /// <summary>
        /// Sort field (default: Date).
        /// </summary>
        public string SortBy { get; set; } = "Date";
    
        /// <summary>
        /// Sort direction (asc/desc, default: desc).
        /// </summary>
        public string SortDirection { get; set; } = "desc";
    
        /// <summary>
        /// Include document rows in the response.
        /// </summary>
        public bool IncludeRows { get; set; } = false;
    
        /// <summary>
        /// Number of items to skip for pagination.
        /// </summary>
        public int Skip => (Page - 1) * PageSize;
    }
}
