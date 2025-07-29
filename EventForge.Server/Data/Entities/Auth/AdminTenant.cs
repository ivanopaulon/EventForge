using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents the mapping between super administrators and tenants they can manage.
/// </summary>
public class AdminTenant : AuditableEntity
{
    /// <summary>
    /// User ID of the super administrator.
    /// </summary>
    [Required]
    [Display(Name = "User ID", Description = "User ID of the super administrator.")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant ID that the admin can manage.
    /// </summary>
    [Required]
    [Display(Name = "Managed Tenant ID", Description = "Tenant ID that the admin can manage.")]
    public Guid ManagedTenantId { get; set; }

    /// <summary>
    /// Admin access level for this tenant.
    /// </summary>
    [Display(Name = "Access Level", Description = "Admin access level for this tenant.")]
    public AdminAccessLevel AccessLevel { get; set; } = AdminAccessLevel.TenantAdmin;

    /// <summary>
    /// Date when admin access was granted (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Granted At", Description = "Date when admin access was granted (UTC).")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when admin access expires (UTC). Null means no expiration.
    /// </summary>
    [Display(Name = "Expires At", Description = "Date when admin access expires (UTC).")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property: The super administrator user.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property: The tenant being managed.
    /// </summary>
    public virtual Tenant ManagedTenant { get; set; } = null!;
}