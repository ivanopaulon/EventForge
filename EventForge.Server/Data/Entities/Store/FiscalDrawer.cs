using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prym.DTOs.Store;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a fiscal drawer (cassetto fiscale) that tracks cash and payment flows for a POS or operator.
/// </summary>
public class FiscalDrawer : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public FiscalDrawerAssignmentType AssignmentType { get; set; } = FiscalDrawerAssignmentType.Fixed;

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Invalid currency code. Use ISO 4217 format (e.g., EUR, USD).")]
    public string CurrencyCode { get; set; } = "EUR";

    [Required]
    public FiscalDrawerStatus Status { get; set; } = FiscalDrawerStatus.Active;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OpeningBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentBalance { get; set; } = 0;

    /// <summary>POS this drawer is fixed to (if AssignmentType == Fixed).</summary>
    public Guid? PosId { get; set; }
    public StorePos? Pos { get; set; }

    /// <summary>Operator this drawer is assigned to (if AssignmentType == Floating).</summary>
    public Guid? OperatorId { get; set; }
    public StoreUser? Operator { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public ICollection<FiscalDrawerSession> Sessions { get; set; } = new List<FiscalDrawerSession>();
    public ICollection<FiscalDrawerTransaction> Transactions { get; set; } = new List<FiscalDrawerTransaction>();
    public ICollection<CashDenomination> CashDenominations { get; set; } = new List<CashDenomination>();
}
