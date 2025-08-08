using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Maps permissions to license features.
/// </summary>
public class LicenseFeaturePermission : AuditableEntity
{
    /// <summary>
    /// Foreign key to the license feature.
    /// </summary>
    [Required]
    [Display(Name = "License Feature ID", Description = "License feature this permission belongs to.")]
    public Guid LicenseFeatureId { get; set; }

    /// <summary>
    /// Foreign key to the permission.
    /// </summary>
    [Required]
    [Display(Name = "Permission ID", Description = "Permission required for this feature.")]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Navigation property: License feature this permission belongs to.
    /// </summary>
    public virtual LicenseFeature LicenseFeature { get; set; } = null!;

    /// <summary>
    /// Navigation property: Permission required for this feature.
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;
}