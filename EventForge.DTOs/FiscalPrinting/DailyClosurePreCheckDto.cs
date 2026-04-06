namespace EventForge.DTOs.FiscalPrinting;

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
}
