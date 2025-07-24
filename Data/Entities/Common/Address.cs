using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Common;


/// <summary>
/// Address associated with any entity (e.g., BusinessParty, Bank, User).
/// </summary>
public class Address : AuditableEntity
{
    /// <summary>
    /// ID of the owning entity (e.g., BusinessParty, Bank, User).
    /// </summary>
    [Required]
    [Display(Name = "Owner ID", Description = "ID of the entity that owns this address.")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Type of the owning entity (e.g., "BusinessParty", "Bank", "User").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Owner Type", Description = "Type of the entity that owns this address.")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// Address type (Legal, Operational, Destination, etc.).
    /// </summary>
    [Display(Name = "Address Type", Description = "Address type (Legal, Operational, Destination, etc.).")]
    public AddressType AddressType { get; set; } = AddressType.Operational;

    /// <summary>
    /// Street and street number.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Street", Description = "Street and street number.")]
    public string? Street { get; set; } = string.Empty;

    /// <summary>
    /// City.
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "City", Description = "City.")]
    public string? City { get; set; } = string.Empty;

    /// <summary>
    /// ZIP code.
    /// </summary>
    [MaxLength(10)]
    [Display(Name = "ZIP Code", Description = "Postal code.")]
    public string? ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Province.
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "Province", Description = "Province.")]
    public string? Province { get; set; } = string.Empty;

    /// <summary>
    /// Country.
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "Country", Description = "Country.")]
    public string? Country { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;
}

/// <summary>
/// Address type enumeration.
/// </summary>
public enum AddressType
{
    Legal,
    Operational,
    Destination
}