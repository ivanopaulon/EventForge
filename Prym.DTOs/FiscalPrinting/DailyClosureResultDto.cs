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
}
