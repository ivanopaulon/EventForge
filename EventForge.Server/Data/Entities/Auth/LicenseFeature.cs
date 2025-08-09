using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents a feature available in a license.
/// </summary>
public class LicenseFeature : AuditableEntity
{
    /// <summary>
    /// Unique name of the feature.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Feature name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique name of the feature.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the feature.
    /// </summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [Display(Name = "Display Name", Description = "Human-readable display name for the feature.")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this feature provides.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of what this feature provides.")]
    public string? Description { get; set; }

    /// <summary>
    /// Category/module this feature belongs to.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    [Display(Name = "Category", Description = "Category/module this feature belongs to.")]
    public string Category { get; set; } = string.Empty;



    /// <summary>
    /// Foreign key to the license this feature belongs to.
    /// </summary>
    [Required]
    [Display(Name = "License ID", Description = "License this feature belongs to.")]
    public Guid LicenseId { get; set; }

    /// <summary>
    /// Navigation property: License this feature belongs to.
    /// </summary>
    public virtual License License { get; set; } = null!;

    /// <summary>
    /// Navigation property: Permissions required for this feature.
    /// </summary>
    public virtual ICollection<LicenseFeaturePermission> LicenseFeaturePermissions { get; set; } = new List<LicenseFeaturePermission>();
}