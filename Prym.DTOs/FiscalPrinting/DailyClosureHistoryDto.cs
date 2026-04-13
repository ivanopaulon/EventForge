namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// A single entry in the history of daily fiscal closures for a specific printer.
/// Returned by <c>GET /api/v1/fiscal-printing/closures/{printerId}</c>.
/// </summary>
public class DailyClosureHistoryDto
{
    /// <summary>Unique identifier of this closure record.</summary>
    public Guid Id { get; set; }

    /// <summary>Identifier of the fiscal printer that executed the closure.</summary>
    public Guid PrinterId { get; set; }

    /// <summary>Display name of the printer (denormalised for list views).</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>Z-report sequence number as reported by the printer.</summary>
    public int ZReportNumber { get; set; }

    /// <summary>Date/time when the closure was executed (UTC).</summary>
    public DateTime ClosedAt { get; set; }

    /// <summary>Number of fiscal receipts included in the Z-report.</summary>
    public int ReceiptCount { get; set; }

    /// <summary>Total amount reported on the Z-report.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Amount collected via cash payments.</summary>
    public decimal CashAmount { get; set; }

    /// <summary>Amount collected via card/electronic payments.</summary>
    public decimal CardAmount { get; set; }

    /// <summary>The operator who triggered the closure.</summary>
    public string? Operator { get; set; }

    /// <summary>
    /// Whether a PDF copy of the Z-report is stored and downloadable.
    /// </summary>
    public bool HasPdf { get; set; }
}
