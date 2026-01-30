using EventForge.DTOs.Common;
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
        /// Primary city/locality from the first address.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Primary province from the first address.
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Primary country from the first address.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// List of contacts associated with the business party (for tooltip display).
        /// </summary>
        public List<ContactDto> Contacts { get; set; } = new List<ContactDto>();

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

        /// <summary>
        /// Default sales price list ID for this business party.
        /// </summary>
        public Guid? DefaultSalesPriceListId { get; set; }

        /// <summary>
        /// Default sales price list name for this business party.
        /// </summary>
        public string? DefaultSalesPriceListName { get; set; }

        /// <summary>
        /// Default purchase price list ID for this business party.
        /// </summary>
        public Guid? DefaultPurchasePriceListId { get; set; }

        /// <summary>
        /// Default purchase price list name for this business party.
        /// </summary>
        public string? DefaultPurchasePriceListName { get; set; }

        /// <summary>
        /// List of groups this business party belongs to (for progressive enhancement).
        /// Null or empty when groups are not loaded/available.
        /// </summary>
        public List<BusinessPartyGroupDto>? Groups { get; set; }
    }
}
