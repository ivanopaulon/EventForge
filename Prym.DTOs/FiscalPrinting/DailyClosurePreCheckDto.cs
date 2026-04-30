namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// Data returned by the pre-check step of the daily closure workflow.
/// Indicates whether it is safe to proceed with the Z-report.
/// </summary>
public class DailyClosurePreCheckDto
{
    /// <summary>Whether the printer currently has an open receipt that must be cancelled or finalised first.</summary>
    public bool HasOpenReceipt { get; set; }

    /// <summary>Whether the cash drawer is currently open (informational warning only).</summary>
    public bool IsDrawerOpen { get; set; }

    /// <summary>Number of fiscal receipts printed since the last daily closure.</summary>
    public int ReceiptCount { get; set; }

    /// <summary>Total amount collected since the last daily closure (sum of all receipt totals).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Amount collected via cash payments.</summary>
    public decimal CashAmount { get; set; }

    /// <summary>Amount collected via card/electronic payments.</summary>
    public decimal CardAmount { get; set; }

    /// <summary>Date of the last successfully executed daily closure, or <c>null</c> if none.</summary>
    public DateTime? LastClosureDate { get; set; }

    /// <summary>Current UTC date/time on the server (reference for the summary).</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// <c>true</c> when it is safe to proceed with the closure
    /// (i.e., <see cref="HasOpenReceipt"/> is <c>false</c>).
    /// </summary>
    public bool CanProceed => !HasOpenReceipt;

    // ─── Printer reachability ────────────────────────────────────────────────

    /// <summary>
    /// Whether the fiscal printer was reachable when this pre-check was performed.
    /// <c>null</c> means no fiscal printer is configured for this terminal
    /// (closure will proceed as <see cref="ClosureType.NonFiscale"/> or <see cref="ClosureType.SoloDatabase"/>).
    /// <c>true</c> means the printer responded successfully.
    /// <c>false</c> means the printer is configured but did not respond.
    /// </summary>
    public bool? PrinterAvailable { get; set; }

    /// <summary>
    /// Error message from the printer reachability check.
    /// Populated only when <see cref="PrinterAvailable"/> is <c>false</c>.
    /// </summary>
    public string? PrinterReachabilityError { get; set; }

    /// <summary>
    /// The type of closure that will be executed based on the current state.
    /// <list type="bullet">
    ///   <item><see cref="ClosureType.Fiscale"/> – printer is configured and reachable.</item>
    ///   <item><see cref="ClosureType.NonFiscale"/> – no fiscal printer configured.</item>
    ///   <item><see cref="ClosureType.SoloDatabase"/> – printer configured but unreachable;
    ///         the fiscal Z-report will be pending.</item>
    /// </list>
    /// </summary>
    public ClosureType PlannedClosureType { get; set; } = ClosureType.Fiscale;
}

