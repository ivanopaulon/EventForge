using Prym.Server.Data.Entities.Audit;
using Prym.Server.Data.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prym.Server.Data.Entities.FiscalPrinting;

/// <summary>
/// Records a completed daily fiscal closure (Z-report) for a fiscal printer.
/// Each row represents one Z-report execution and stores the key metrics reported
/// by the printer (Z-report sequence number, receipt count, totals, operator).
/// </summary>
public class DailyClosureRecord : AuditableEntity
{
    // ── Foreign key ───────────────────────────────────────────────────────────

    /// <summary>Identifier of the fiscal <see cref="Printer"/> that produced this Z-report.</summary>
    [Required]
    public Guid PrinterId { get; set; }

    /// <summary>Navigation property to the printer.</summary>
    public Printer? Printer { get; set; }

    // ── Z-Report data ─────────────────────────────────────────────────────────

    /// <summary>Z-report sequence number as reported by the printer hardware.</summary>
    public int ZReportNumber { get; set; }

    /// <summary>Date/time when the closure was executed (UTC).</summary>
    [Required]
    public DateTime ClosedAt { get; set; }

    /// <summary>Number of fiscal receipts included in the Z-report since the last closure.</summary>
    public int ReceiptCount { get; set; }

    /// <summary>Total amount collected (sum of all fiscal receipt totals) since the last closure.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Amount collected via cash payments.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CashAmount { get; set; }

    /// <summary>Amount collected via card/electronic payments.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CardAmount { get; set; }

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>Name of the operator who triggered the closure (for display and audit).</summary>
    [MaxLength(100)]
    public string? Operator { get; set; }

    /// <summary>
    /// Whether a PDF copy of the Z-report has been generated and stored.
    /// When <c>true</c>, the PDF can be downloaded via the API.
    /// </summary>
    public bool HasPdf { get; set; }

    /// <summary>
    /// Binary content of the generated PDF Z-report.
    /// Stored in the DB to allow repeated downloads without re-generation.
    /// Null until a PDF is generated via the API.
    /// </summary>
    public byte[]? PdfBytes { get; set; }

    /// <summary>
    /// Raw printer response for diagnostics (truncated to 500 chars to avoid large rows).
    /// </summary>
    [MaxLength(500)]
    public string? PrinterResponse { get; set; }
}
