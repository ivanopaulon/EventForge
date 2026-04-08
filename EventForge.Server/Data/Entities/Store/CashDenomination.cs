using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventForge.DTOs.Store;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a specific cash denomination tracked in a fiscal drawer
/// (e.g., 3 banknotes of €10, 2 coins of €0.50).
/// </summary>
public class CashDenomination : AuditableEntity
{
    [Required]
    public Guid FiscalDrawerId { get; set; }
    public FiscalDrawer FiscalDrawer { get; set; } = null!;

    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>Face value of the denomination (e.g., 10.00 for a €10 note).</summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    [Required]
    public DenominationType DenominationType { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; } = 0;

    public int SortOrder { get; set; } = 0;
}
