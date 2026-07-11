using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

/// <summary>
/// A manageable fidelity level (tier) that replaces the fixed <c>FidelityCardType</c> enum.
/// Tiers are tenant-scoped and ordered by <see cref="SortOrder"/> (0 = base tier).
/// </summary>
public class FidelityTier : AuditableEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ordinal used to rank tiers. 0 is the base (lowest) tier; higher values are higher tiers.
    /// </summary>
    public int SortOrder { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    // Note: IsActive is inherited from AuditableEntity (defaults to true).

    /// <summary>
    /// Optional promotion/demotion rule attached to this tier.
    /// </summary>
    public FidelityTierRule? Rule { get; set; }
}
