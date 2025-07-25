using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role : AuditableEntity
{
    /// <summary>
    /// Unique name of the role.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique name of the role.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the role.
    /// </summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [Display(Name = "Display Name", Description = "Human-readable display name for the role.")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role and its purpose.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of the role and its purpose.")]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if this is a system-defined role that cannot be deleted.
    /// </summary>
    [Display(Name = "Is System Role", Description = "Indicates if this is a system-defined role that cannot be deleted.")]
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Navigation property: Users assigned to this role.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Navigation property: Permissions granted to this role.
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}