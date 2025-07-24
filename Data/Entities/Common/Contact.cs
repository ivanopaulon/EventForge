using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Common;

/// <summary>
/// Contact associated with any entity (e.g., BusinessParty, Bank, User, Reference).
/// </summary>
public class Contact : AuditableEntity
{
    /// <summary>
    /// ID of the owning entity (e.g., BusinessParty, Bank, User, Reference).
    /// </summary>
    [Required]
    [Display(Name = "Owner ID", Description = "ID of the entity that owns this contact.")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Type of the owning entity (e.g., "BusinessParty", "Bank", "User", "Reference").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Owner Type", Description = "Type of the entity that owns this contact.")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// Type of contact (Email, Phone, Fax, PEC, etc.).
    /// </summary>
    [Required]
    [Display(Name = "Contact Type", Description = "Type of contact (Email, Phone, Fax, PEC, etc.).")]
    public ContactType ContactType { get; set; } = ContactType.Email;

    /// <summary>
    /// Contact value (e.g., email address, phone number).
    /// </summary>
    [Required(ErrorMessage = "The contact value is required.")]
    [MaxLength(100, ErrorMessage = "The contact value cannot exceed 100 characters.")]
    [Display(Name = "Value", Description = "Contact value.")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The notes cannot exceed 100 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;
}

/// <summary>
/// Contact type enumeration.
/// </summary>
public enum ContactType
{
    Email,
    Phone,
    Fax,
    PEC,
    Other
}