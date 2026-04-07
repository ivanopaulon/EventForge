using Prym.DTOs.FiscalPrinting;

namespace Prym.Server.Services.FiscalPrinting.CustomProtocol;

/// <summary>
/// Parses the 3-byte status bitmap returned by the Custom fiscal printer in response to
/// the <see cref="CustomProtocolCommands.CMD_READ_STATUS"/> ("10") command.
/// </summary>
/// <remarks>
/// The printer returns the status bitmap as the payload of a data frame (inside STX…ETX).
/// Use <see cref="CustomResponseParser.TryExtractData"/> to extract the raw bytes first,
/// then pass them to <see cref="Parse"/>.
///
/// Bitmap layout:
/// <list type="bullet">
///   <item>Byte 1 – Error flags: paper out, cover open, head error, cutter error, fiscal memory full</item>
///   <item>Byte 2 – Warning/info flags: paper low, fiscal memory almost full, drawer open, head overheat</item>
///   <item>Byte 3 – Operational flags: receipt open, fiscal mode active, daily closure required</item>
/// </list>
/// </remarks>
public static class CustomStatusParser
{
    /// <summary>
    /// Parses a 3-byte status bitmap into a <see cref="FiscalPrinterStatus"/> instance.
    /// All boolean fields default to <c>false</c> when the corresponding bitmap byte is absent.
    /// </summary>
    /// <param name="statusData">
    /// The raw status bytes as received in the printer's data frame payload.
    /// Should contain at least 3 bytes; extra bytes are ignored.
    /// A null or short array is accepted and leaves unresolvable flags at their default values.
    /// </param>
    /// <returns>
    /// A fully populated <see cref="FiscalPrinterStatus"/> with <see cref="FiscalPrinterStatus.IsOnline"/>
    /// set to <c>true</c>, all bitmap flags extracted, and <see cref="FiscalPrinterStatus.LastCheck"/> set to UTC now.
    /// </returns>
    public static FiscalPrinterStatus Parse(byte[]? statusData)
    {
        var status = new FiscalPrinterStatus
        {
            IsOnline = true,
            LastCheck = DateTime.UtcNow
        };

        if (statusData is null || statusData.Length == 0)
        {
            status.PaperStatus = FiscalPrinterPaperStatus.Unknown.ToString().ToUpperInvariant();
            return status;
        }

        // --- Byte 1: Error flags ---
        if (statusData.Length >= 1)
        {
            byte b1 = statusData[0];
            status.IsPaperOut = (b1 & 0x01) != 0;           // bit 0
            status.IsCoverOpen = (b1 & 0x02) != 0;          // bit 1
            status.IsHeadError = (b1 & 0x04) != 0;          // bit 2
            status.IsCutterError = (b1 & 0x08) != 0;        // bit 3
            status.IsFiscalMemoryFull = (b1 & 0x10) != 0;   // bit 4
        }

        // --- Byte 2: Warning / info flags ---
        if (statusData.Length >= 2)
        {
            byte b2 = statusData[1];
            status.IsPaperLow = (b2 & 0x01) != 0;                  // bit 0
            status.IsFiscalMemoryAlmostFull = (b2 & 0x02) != 0;    // bit 1
            status.IsDrawerOpen = (b2 & 0x04) != 0;                 // bit 2
            status.IsHeadOverheat = (b2 & 0x08) != 0;               // bit 3
        }

        // --- Byte 3: Operational flags ---
        if (statusData.Length >= 3)
        {
            byte b3 = statusData[2];
            status.IsReceiptOpen = (b3 & 0x01) != 0;            // bit 0
            status.IsFiscalModeActive = (b3 & 0x02) != 0;       // bit 1
            status.IsDailyClosureRequired = (b3 & 0x04) != 0;   // bit 2
        }

        // Derive the human-readable paper status string
        status.PaperStatus = GetPaperStatusString(status);

        return status;
    }

    /// <summary>
    /// Returns a human-readable paper status string based on the bitmap flags.
    /// </summary>
    /// <param name="status">A <see cref="FiscalPrinterStatus"/> already populated by <see cref="Parse"/>.</param>
    /// <returns>"OUT" if paper is exhausted, "LOW" if running low, "OK" otherwise.</returns>
    public static string GetPaperStatusString(FiscalPrinterStatus status)
    {
        if (status.IsPaperOut) return FiscalPrinterPaperStatus.Out.ToString().ToUpperInvariant();
        if (status.IsPaperLow) return FiscalPrinterPaperStatus.Low.ToString().ToUpperInvariant();
        return FiscalPrinterPaperStatus.Ok.ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Returns <c>true</c> when the status contains at least one critical error that blocks printing.
    /// Critical conditions: paper out, fiscal memory full, head error, or cutter error.
    /// </summary>
    /// <param name="status">A <see cref="FiscalPrinterStatus"/> already populated by <see cref="Parse"/>.</param>
    /// <returns><c>true</c> if any critical error flag is set.</returns>
    public static bool HasCriticalErrors(FiscalPrinterStatus status)
        => status.IsPaperOut
        || status.IsFiscalMemoryFull
        || status.IsHeadError
        || status.IsCutterError;
}
