namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// Health summary for a fiscal printer, returned by the
/// <c>GET /api/v1/fiscal-printing/health/{printerId}</c> endpoint.
/// Combines a live connection test result with the most recent cached status snapshot.
/// </summary>
public class FiscalPrinterHealthDto
{
    /// <summary>Unique identifier of the printer.</summary>
    public Guid PrinterId { get; set; }

    /// <summary>
    /// Whether the printer responded to the live connection test (ENQ frame).
    /// A <c>false</c> value means the printer is offline or unreachable.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Error message from the live connection test when <see cref="IsOnline"/> is <c>false</c>.
    /// <c>null</c> when the printer is online.
    /// </summary>
    public string? ConnectionError { get; set; }

    /// <summary>
    /// Most recent cached status from the background polling service (may be up to 15 seconds old).
    /// <c>null</c> if the printer has never been polled or the cache entry has expired.
    /// </summary>
    public FiscalPrinterStatus? CachedStatus { get; set; }

    /// <summary>UTC timestamp when this health check was executed.</summary>
    public DateTime CheckedAt { get; set; }

    // -------------------------------------------------------------------------
    //  Computed helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Whether any critical condition requires immediate attention
    /// (fiscal memory full, paper out, cover open, head error, cutter error).
    /// </summary>
    public bool HasCriticalIssue =>
        CachedStatus is not null && (
            CachedStatus.IsFiscalMemoryFull ||
            CachedStatus.IsPaperOut ||
            CachedStatus.IsCoverOpen ||
            CachedStatus.IsHeadError ||
            CachedStatus.IsCutterError);

    /// <summary>
    /// Whether a daily fiscal closure is required.
    /// Custom printers block new receipts if closure is not performed within 24 hours.
    /// </summary>
    public bool IsDailyClosureRequired => CachedStatus?.IsDailyClosureRequired == true;

    /// <summary>
    /// Human-readable summary of the printer health state.
    /// </summary>
    public string Summary =>
        !IsOnline ? "Offline" :
        HasCriticalIssue ? "Critical" :
        IsDailyClosureRequired ? "Closure Required" :
        CachedStatus?.IsPaperLow == true ? "Warning" :
        "OK";
}
