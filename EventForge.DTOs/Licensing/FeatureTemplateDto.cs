using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing;

/// <summary>
/// DTO for FeatureTemplate entity.
/// Represents a master catalog entry of available features in the system.
/// </summary>
public class FeatureTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = string.Empty;

    public int MinimumTierLevel { get; set; }

    public bool IsAvailable { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for creating a new FeatureTemplate.
/// </summary>
public class CreateFeatureTemplateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MinimumTierLevel { get; set; } = 1;

    public bool IsAvailable { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// DTO for updating an existing FeatureTemplate.
/// Note: Name cannot be changed after creation.
/// </summary>
public class UpdateFeatureTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MinimumTierLevel { get; set; } = 1;

    public bool IsAvailable { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
