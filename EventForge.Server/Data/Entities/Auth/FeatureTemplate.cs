using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents a master catalog of available features in the system.
/// Used as a template for creating license features.
/// </summary>
public class FeatureTemplate : AuditableEntity
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
    /// Minimum tier level required for this feature (1 = Basic, 2 = Standard, 3 = Premium, etc.).
    /// </summary>
    [Range(1, 10, ErrorMessage = "Minimum tier level must be between 1 and 10.")]
    [Display(Name = "Minimum Tier Level", Description = "Minimum tier level required for this feature.")]
    public int MinimumTierLevel { get; set; } = 1;

    /// <summary>
    /// Indicates if this feature is currently available for assignment.
    /// </summary>
    [Display(Name = "Is Available", Description = "Indicates if this feature is currently available for assignment.")]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Sort order for displaying features.
    /// </summary>
    [Display(Name = "Sort Order", Description = "Sort order for displaying features.")]
    public int SortOrder { get; set; } = 0;
}
