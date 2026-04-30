namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// Result returned after a daily fiscal closure (Z-report) has been executed.
/// </summary>
public class DailyClosureResultDto
{
    /// <summary>Whether the daily closure was executed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if the closure failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Unique identifier of the closure record saved in the database.</summary>
    public Guid? ClosureId { get; set; }

    /// <summary>Z-report sequence number reported by the printer.</summary>
    public int? ZReportNumber { get; set; }

    /// <summary>Date/time when the closure was executed (UTC).</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Number of fiscal receipts included in the Z-report.</summary>
    public int ReceiptCount { get; set; }

    /// <summary>Total amount reported on the Z-report.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Cash portion of the total amount.</summary>
    public decimal CashAmount { get; set; }

    /// <summary>The operator who triggered the closure.</summary>
    public string? Operator { get; set; }

    /// <summary>
    /// How the closure was performed.
    /// <list type="bullet">
    ///   <item><see cref="ClosureType.Fiscale"/> – Z-report sent to hardware successfully.</item>
    ///   <item><see cref="ClosureType.NonFiscale"/> – No fiscal hardware; summary printed on thermal printer.</item>
    ///   <item><see cref="ClosureType.SoloDatabase"/> – DB-only: fiscal printer was configured but unreachable,
    ///         or explicitly skipped.</item>
    /// </list>
    /// </summary>
    public ClosureType ClosureType { get; set; } = ClosureType.Fiscale;

    /// <summary>
    /// When <c>true</c>, the fiscal Z-report could not be sent to the printer hardware.
    /// The totals are saved in the database but the hardware closure is still pending.
    /// Use <c>RetryFiscalClosureAsync</c> to retry once the printer is back online.
    /// </summary>
    public bool FiscalClosurePending { get; set; }

    /// <summary>
    /// Error message captured from the printer at the time of the closure attempt.
    /// Populated only when <see cref="FiscalClosurePending"/> is <c>true</c>.
    /// </summary>
    public string? PrinterErrors { get; set; }
}

