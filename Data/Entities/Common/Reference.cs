using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Common;


/// <summary>
/// Reference person associated with any entity (e.g., BusinessParty, Bank, User).
/// </summary>
public class Reference : AuditableEntity
{
    /// <summary>
    /// ID of the owning entity (e.g., BusinessParty, Bank, User).
    /// </summary>
    [Required]
    [Display(Name = "Owner ID", Description = "ID of the entity that owns this reference.")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Type of the owning entity (e.g., "BusinessParty", "Bank", "User").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Owner Type", Description = "Type of the entity that owns this reference.")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// First name of the reference person.
    /// </summary>
    [Required(ErrorMessage = "The first name is required.")]
    [MaxLength(50, ErrorMessage = "The first name cannot exceed 50 characters.")]
    [Display(Name = "First Name", Description = "Reference person's first name.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the reference person.
    /// </summary>
    [Required(ErrorMessage = "The last name is required.")]
    [MaxLength(50, ErrorMessage = "The last name cannot exceed 50 characters.")]
    [Display(Name = "Last Name", Description = "Reference person's last name.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Department or role of the reference person.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The department cannot exceed 50 characters.")]
    [Display(Name = "Department", Description = "Department or role.")]
    public string? Department { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The notes cannot exceed 100 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; } = string.Empty;

    /// <summary>
    /// Contacts for this reference person.
    /// </summary>
    [Display(Name = "Contacts", Description = "Contacts for this reference person.")]
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}