using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventForge.DTOs.Store;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a daily session for a fiscal drawer.
/// A session is opened at the start of the day and closed at the end.
/// </summary>
public class FiscalDrawerSession : AuditableEntity
{
    [Required]
    public Guid FiscalDrawerId { get; set; }
    public FiscalDrawer FiscalDrawer { get; set; } = null!;

    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OpeningBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ClosingBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCashIn { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCashOut { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeposits { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWithdrawals { get; set; } = 0;

    public int TransactionCount { get; set; } = 0;

    public Guid? OpenedByOperatorId { get; set; }
    public StoreUser? OpenedByOperator { get; set; }

    public Guid? ClosedByOperatorId { get; set; }
    public StoreUser? ClosedByOperator { get; set; }

    [Required]
    public FiscalDrawerSessionStatus Status { get; set; } = FiscalDrawerSessionStatus.Open;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<FiscalDrawerTransaction> Transactions { get; set; } = new List<FiscalDrawerTransaction>();
}
