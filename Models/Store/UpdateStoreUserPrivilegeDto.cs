using System.ComponentModel.DataAnnotations;
using EventForge.Data.Entities.Store;

namespace EventForge.Models.Store;

/// <summary>
/// DTO for StoreUserPrivilege update operations.
/// </summary>
public class UpdateStoreUserPrivilegeDto
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
}