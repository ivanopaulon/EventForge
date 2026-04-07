using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents a license type available in the system.
/// </summary>
public class License : AuditableEntity
{
    /// <summary>
    /// Unique name of the license type.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "License name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique name of the license type.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the license.
    /// </summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [Display(Name = "Display Name", Description = "Human-readable display name for the license.")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this license includes.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Description of what this license includes.")]
    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of users allowed with this license.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Max users must be at least 1.")]
    [Display(Name = "Max Users", Description = "Maximum number of users allowed with this license.")]
    public int MaxUsers { get; set; } = 100;

    /// <summary>
    /// Maximum number of API calls per month allowed with this license.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Max API calls must be non-negative.")]
    [Display(Name = "Max API Calls", Description = "Maximum number of API calls per month allowed with this license.")]
    public int MaxApiCallsPerMonth { get; set; } = 10000;

    /// <summary>
    /// License tier level (1 = Basic, 2 = Standard, 3 = Premium, etc.).
    /// </summary>
    [Range(1, 10, ErrorMessage = "Tier level must be between 1 and 10.")]
    [Display(Name = "Tier Level", Description = "License tier level (1 = Basic, 2 = Standard, 3 = Premium, etc.).")]
    public int TierLevel { get; set; } = 1;

    /// <summary>
    /// Navigation property: Features available with this license.
    /// </summary>
    public virtual ICollection<LicenseFeature> LicenseFeatures { get; set; } = new List<LicenseFeature>();

    /// <summary>
    /// Navigation property: Tenants that have this license.
    /// </summary>
    public virtual ICollection<TenantLicense> TenantLicenses { get; set; } = new List<TenantLicense>();
}