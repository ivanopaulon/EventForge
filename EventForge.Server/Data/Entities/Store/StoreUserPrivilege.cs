using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Store;


/// <summary>
/// Represents a privilege/permission for cashier operations.
/// </summary>
public class StoreUserPrivilege : AuditableEntity
{
    /// <summary>
    /// Technical code of the privilege (for programmatic use).
    /// </summary>
    [Required(ErrorMessage = "The privilege code is required.")]
    [MaxLength(50, ErrorMessage = "The code cannot exceed 50 characters.")]
    [Display(Name = "Code", Description = "Technical code of the privilege.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the privilege.
    /// </summary>
    [Required(ErrorMessage = "The privilege name is required.")]
    [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "Privilege", Description = "Display name of the privilege.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Privilege category (for grouping).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Category or functional group of the privilege.")]
    public string? Category { get; set; }

    /// <summary>
    /// Description of the privilege.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the privilege.")]
    public string? Description { get; set; }

    /// <summary>
    /// Status of the privilege.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the privilege.")]
    public CashierPrivilegeStatus Status { get; set; } = CashierPrivilegeStatus.Active;

    /// <summary>
    /// Custom sort order for displaying privileges.
    /// </summary>
    [Display(Name = "Sort Order", Description = "Display order of the privilege.")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Collection of groups that have this privilege.
    /// </summary>
    [Display(Name = "Groups", Description = "Groups that have this privilege.")]
    public ICollection<StoreUserGroup> Groups { get; set; } = new List<StoreUserGroup>();
}

/// <summary>
/// Status for the cashier privilege.
/// </summary>
public enum CashierPrivilegeStatus
{
    Active,      // Privilege is active and assignable
    Suspended,   // Privilege is temporarily suspended
    Deprecated,  // Privilege is deprecated (should not be assigned anymore)
    Deleted      // Privilege is deleted/disabled
}