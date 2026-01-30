using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Business
{

    /// <summary>
    /// DTO for BusinessParty update operations.
    /// </summary>
    public class UpdateBusinessPartyDto
    {
        /// <summary>
        /// Type of business party (Customer, Supplier, Both).
        /// </summary>
        [Required]
        [Display(Name = "Type", Description = "Type of business party (Customer, Supplier, Both).")]
        public BusinessPartyType PartyType { get; set; } = BusinessPartyType.Cliente;

        /// <summary>
        /// Company name or full name.
        /// </summary>
        [Required(ErrorMessage = "The name is required.")]
        [MaxLength(200, ErrorMessage = "The name cannot exceed 200 characters.")]
        [Display(Name = "Name", Description = "Company name or full name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tax code.
        /// </summary>
        [MaxLength(20, ErrorMessage = "The tax code cannot exceed 20 characters.")]
        [Display(Name = "Tax Code", Description = "Tax code.")]
        public string? TaxCode { get; set; }

        /// <summary>
        /// VAT number.
        /// </summary>
        [MaxLength(20, ErrorMessage = "The VAT number cannot exceed 20 characters.")]
        [Display(Name = "VAT Number", Description = "VAT number.")]
        public string? VatNumber { get; set; }

        /// <summary>
        /// SDI code (for electronic invoicing).
        /// </summary>
        [MaxLength(10, ErrorMessage = "The SDI code cannot exceed 10 characters.")]
        [Display(Name = "SDI Code", Description = "SDI recipient code.")]
        public string? SdiCode { get; set; }

        /// <summary>
        /// Certified email (PEC).
        /// </summary>
        [MaxLength(100, ErrorMessage = "The PEC cannot exceed 100 characters.")]
        [Display(Name = "PEC", Description = "Certified email (PEC).")]
        public string? Pec { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
        [Display(Name = "Notes", Description = "Additional notes.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Default sales price list ID for this business party.
        /// </summary>
        public Guid? DefaultSalesPriceListId { get; set; }

        /// <summary>
        /// Default purchase price list ID for this business party.
        /// </summary>
        public Guid? DefaultPurchasePriceListId { get; set; }
    }
}
