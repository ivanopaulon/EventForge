using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Store;

/// <summary>
/// Represents a group of cashiers with assigned privileges.
/// </summary>
public class StoreUserGroup : AuditableEntity
{
    /// <summary>
    /// Technical code of the group (for programmatic use).
    /// </summary>
    [Required(ErrorMessage = "The group code is required.")]
    [MaxLength(50, ErrorMessage = "The code cannot exceed 50 characters.")]
    [Display(Name = "Code", Description = "Technical code of the group.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the group.
    /// </summary>
    [Required(ErrorMessage = "The group name is required.")]
    [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "Group Name", Description = "Display name of the cashier group.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the group.")]
    public string? Description { get; set; }

    /// <summary>
    /// Status of the group.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the group.")]
    public CashierGroupStatus Status { get; set; } = CashierGroupStatus.Active;

    /// <summary>
    /// Cashiers belonging to this group.
    /// </summary>
    [Display(Name = "Cashiers", Description = "Cashiers belonging to this group.")]
    public ICollection<StoreUser> Cashiers { get; set; } = new List<StoreUser>();   

    /// <summary>
    /// Privileges assigned to this group.
    /// </summary>
    [Display(Name = "Privileges", Description = "Privileges assigned to this group.")]
    public ICollection<StoreUserPrivilege> Privileges { get; set; } = new List<StoreUserPrivilege>();
}

/// <summary>
/// Status for the cashier group.
/// </summary>
public enum CashierGroupStatus
{
    Active,      // Group is active and assignable
    Suspended,   // Group is temporarily suspended
    Deprecated,  // Group is deprecated (should not be used anymore)
    Deleted      // Group is deleted/disabled
}