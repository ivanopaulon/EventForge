using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Auth;

/// <summary>
/// Represents a permission in the system.
/// </summary>
public class Permission : AuditableEntity
{
    /// <summary>
    /// Unique name of the permission.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Permission name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique name of the permission.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the permission.
    /// </summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [Display(Name = "Display Name", Description = "Human-readable display name for the permission.")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this permission allows.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of what this permission allows.")]
    public string? Description { get; set; }

    /// <summary>
    /// Category/module this permission belongs to.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    [Display(Name = "Category", Description = "Category/module this permission belongs to.")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Resource or entity this permission applies to.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Resource cannot exceed 100 characters.")]
    [Display(Name = "Resource", Description = "Resource or entity this permission applies to.")]
    public string? Resource { get; set; }

    /// <summary>
    /// Action this permission allows (Create, Read, Update, Delete, etc.).
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Action cannot exceed 50 characters.")]
    [Display(Name = "Action", Description = "Action this permission allows.")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a system-defined permission that cannot be deleted.
    /// </summary>
    [Display(Name = "Is System Permission", Description = "Indicates if this is a system-defined permission that cannot be deleted.")]
    public bool IsSystemPermission { get; set; } = false;

    /// <summary>
    /// Navigation property: Roles that have this permission.
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}