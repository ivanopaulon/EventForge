using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Auth;

/// <summary>
/// Many-to-many relationship between Role and Permission.
/// </summary>
public class RolePermission : AuditableEntity
{
    /// <summary>
    /// Foreign key to Role.
    /// </summary>
    [Required]
    [Display(Name = "Role ID", Description = "Foreign key to Role.")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key to Permission.
    /// </summary>
    [Required]
    [Display(Name = "Permission ID", Description = "Foreign key to Permission.")]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Date when the permission was granted to the role (UTC).
    /// </summary>
    [Display(Name = "Granted At", Description = "Date when the permission was granted to the role (UTC).")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who granted this permission to the role.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Granted By", Description = "User who granted this permission to the role.")]
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Navigation property: The role.
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property: The permission.
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;
}