using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;


/// <summary>
/// Represents a business entity (customer, supplier, or both).
/// </summary>
public class BusinessParty : AuditableEntity
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
    public string? TaxCode { get; set; } = string.Empty;

    /// <summary>
    /// VAT number.
    /// </summary>
    [MaxLength(20, ErrorMessage = "The VAT number cannot exceed 20 characters.")]
    [Display(Name = "VAT Number", Description = "VAT number.")]
    public string? VatNumber { get; set; } = string.Empty;

    /// <summary>
    /// SDI code (for electronic invoicing).
    /// </summary>
    [MaxLength(10, ErrorMessage = "The SDI code cannot exceed 10 characters.")]
    [Display(Name = "SDI Code", Description = "SDI recipient code.")]
    public string? SdiCode { get; set; } = string.Empty;

    /// <summary>
    /// Certified email (PEC).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The PEC cannot exceed 100 characters.")]
    [Display(Name = "PEC", Description = "Certified email (PEC).")]
    public string? Pec { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;

    /// <summary>
    /// Addresses associated with the business party.
    /// </summary>
    [Display(Name = "Addresses", Description = "Addresses associated with the business party.")]
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    /// <summary>
    /// Contacts associated with the business party.
    /// </summary>
    [Display(Name = "Contacts", Description = "General contacts.")]
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    /// <summary>
    /// Reference persons associated with the business party.
    /// </summary>
    [Display(Name = "References", Description = "Reference persons.")]
    public ICollection<Reference> References { get; set; } = new List<Reference>();

    /// <summary>
    /// Modalità di applicazione prezzo predefinita per questo business party
    /// </summary>
    [Display(Name = "Default Price Application Mode", Description = "Default price application mode for this business party.")]
    public PriceApplicationMode DefaultPriceApplicationMode { get; set; } = PriceApplicationMode.Automatic;

    /// <summary>
    /// Listino forzato (se PriceApplicationMode = ForcedPriceList o Hybrid)
    /// </summary>
    [Display(Name = "Forced Price List", Description = "Forced price list (if PriceApplicationMode = ForcedPriceList or Hybrid).")]
    public Guid? ForcedPriceListId { get; set; }

    /// <summary>
    /// Navigation property per il listino forzato
    /// </summary>
    public PriceList.PriceList? ForcedPriceList { get; set; }

    /// <summary>
    /// Listino vendita predefinito per questo Business Party
    /// </summary>
    [Display(Name = "Default Sales Price List", Description = "Default sales price list for this business party.")]
    public Guid? DefaultSalesPriceListId { get; set; }

    /// <summary>
    /// Navigation property per il listino vendita predefinito
    /// </summary>
    public PriceList.PriceList? DefaultSalesPriceList { get; set; }

    /// <summary>
    /// Listino acquisto predefinito per questo Business Party
    /// </summary>
    [Display(Name = "Default Purchase Price List", Description = "Default purchase price list for this business party.")]
    public Guid? DefaultPurchasePriceListId { get; set; }

    /// <summary>
    /// Navigation property per il listino acquisto predefinito
    /// </summary>
    public PriceList.PriceList? DefaultPurchasePriceList { get; set; }

    /// <summary>
    /// Gruppi di appartenenza del Business Party
    /// </summary>
    [Display(Name = "Group Memberships", Description = "Gruppi di appartenenza")]
    public ICollection<BusinessPartyGroupMember> GroupMemberships { get; set; } = new List<BusinessPartyGroupMember>();
}

/// <summary>
/// Business party type.
/// </summary>
public enum BusinessPartyType
{
    Cliente,
    Fornitore,
    ClienteFornitore
}