namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// Lightweight status DTO returned by the "morning check" endpoint.
/// Indicates whether the previous business day's daily fiscal closure was performed.
/// This check is DB-only and does NOT require communication with the printer hardware.
/// </summary>
public class PreviousDayClosureStatusDto
{
    /// <summary>
    /// <c>true</c> when no daily closure record exists in the database for the
    /// previous business day (i.e. <see cref="LastClosureDate"/> is <c>null</c> or
    /// its date is strictly before <see cref="PreviousBusinessDay"/>).
    /// </summary>
    public bool IsPreviousDayClosureMissing { get; set; }

    /// <summary>Previous business day date (UTC, time part is midnight).</summary>
    public DateTime PreviousBusinessDay { get; set; }

    /// <summary>UTC timestamp of the most recent closure record for this printer, or <c>null</c> if none.</summary>
    public DateTime? LastClosureDate { get; set; }
}
