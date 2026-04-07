using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Many-to-many relationship between User and Role.
/// </summary>
public class UserRole : AuditableEntity
{
    /// <summary>
    /// Foreign key to User.
    /// </summary>
    [Required]
    [Display(Name = "User ID", Description = "Foreign key to User.")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to Role.
    /// </summary>
    [Required]
    [Display(Name = "Role ID", Description = "Foreign key to Role.")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Date when the role was granted to the user (UTC).
    /// </summary>
    [Display(Name = "Granted At", Description = "Date when the role was granted to the user (UTC).")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who granted this role assignment.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Granted By", Description = "User who granted this role assignment.")]
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Optional expiration date for this role assignment (UTC).
    /// </summary>
    [Display(Name = "Expires At", Description = "Optional expiration date for this role assignment (UTC).")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property: The user.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property: The role.
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Indicates if this role assignment is currently valid (not expired).
    /// </summary>
    public bool IsCurrentlyValid => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
}