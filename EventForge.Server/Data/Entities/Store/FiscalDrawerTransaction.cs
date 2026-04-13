using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prym.DTOs.Store;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a single transaction recorded in a fiscal drawer (sale, deposit, withdrawal, etc.).
/// </summary>
public class FiscalDrawerTransaction : AuditableEntity
{
    [Required]
    public Guid FiscalDrawerId { get; set; }
    public FiscalDrawer FiscalDrawer { get; set; } = null!;

    public Guid? FiscalDrawerSessionId { get; set; }
    public FiscalDrawerSession? FiscalDrawerSession { get; set; }

    [Required]
    public FiscalDrawerTransactionType TransactionType { get; set; }

    [Required]
    public FiscalDrawerPaymentType PaymentType { get; set; } = FiscalDrawerPaymentType.Cash;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Link to a sale session if this transaction originated from a sale.</summary>
    public Guid? SaleSessionId { get; set; }

    [Required]
    public DateTime TransactionAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? OperatorName { get; set; }
}
