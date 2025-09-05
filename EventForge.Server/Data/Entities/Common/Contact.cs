using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Common;


/// <summary>
/// Contact associated with any entity (e.g., BusinessParty, Bank, User, Reference, Team, TeamMember).
/// </summary>
public class Contact : AuditableEntity
{
    /// <summary>
    /// ID of the owning entity (e.g., BusinessParty, Bank, User, Reference, Team, TeamMember).
    /// </summary>
    [Required]
    [Display(Name = "Owner ID", Description = "ID of the entity that owns this contact.")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Type of the owning entity (e.g., "BusinessParty", "Bank", "User", "Reference", "Team", "TeamMember").
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
    /// Purpose of this contact (Primary, Emergency, Billing, Coach, etc.).
    /// </summary>
    [Display(Name = "Purpose", Description = "Purpose of this contact.")]
    public ContactPurpose Purpose { get; set; } = ContactPurpose.Primary;

    /// <summary>
    /// Relationship to the owner (for emergency contacts, family relationships, etc.).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The relationship cannot exceed 50 characters.")]
    [Display(Name = "Relationship", Description = "Relationship to the owner.")]
    public string? Relationship { get; set; }

    /// <summary>
    /// Indicates if this is the primary contact of its type.
    /// </summary>
    [Display(Name = "Is Primary", Description = "Indicates if this is the primary contact of its type.")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The notes cannot exceed 100 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;
}