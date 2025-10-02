using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Business
{

    /// <summary>
    /// DTO for BusinessParty output/display operations.
    /// </summary>
    public class BusinessPartyDto
    {
        /// <summary>
        /// Unique identifier for the business party.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type of business party (Customer, Supplier, Both).
        /// </summary>
        public BusinessPartyType PartyType { get; set; }

        /// <summary>
        /// Company name or full name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tax code.
        /// </summary>
        public string? TaxCode { get; set; }

        /// <summary>
        /// VAT number.
        /// </summary>
        public string? VatNumber { get; set; }

        /// <summary>
        /// SDI code (for electronic invoicing).
        /// </summary>
        public string? SdiCode { get; set; }

        /// <summary>
        /// Certified email (PEC).
        /// </summary>
        public string? Pec { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Number of addresses associated with the business party.
        /// </summary>
        public int AddressCount { get; set; }

        /// <summary>
        /// Number of contacts associated with the business party.
        /// </summary>
        public int ContactCount { get; set; }

        /// <summary>
        /// Number of reference persons associated with the business party.
        /// </summary>
        public int ReferenceCount { get; set; }

        /// <summary>
        /// Indicates if accounting data exists for this business party.
        /// </summary>
        public bool HasAccountingData { get; set; }

        /// <summary>
        /// Indicates if the business party is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date and time when the business party was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the business party.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the business party was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the business party.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
