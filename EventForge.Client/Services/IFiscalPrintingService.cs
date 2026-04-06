using EventForge.DTOs.FiscalPrinting;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service interface for fiscal printer operations.
/// Mirrors the server-side <c>IFiscalPrinterService</c> via HTTP calls to
/// <c>/api/v1/fiscal-printing/*</c> endpoints.
/// </summary>
public interface IFiscalPrintingService
{
    /// <summary>Prints a complete fiscal receipt on the specified printer.</summary>
    Task<FiscalPrintResult?> PrintReceiptAsync(Guid printerId, FiscalReceiptData receipt, CancellationToken ct = default);

    /// <summary>Cancels the currently open receipt on the specified printer.</summary>
    Task<FiscalPrintResult?> CancelCurrentReceiptAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Prints a full refund receipt (reso totale).</summary>
    Task<FiscalPrintResult?> PrintRefundReceiptAsync(Guid printerId, FiscalRefundData refund, CancellationToken ct = default);

    /// <summary>Prints a partial refund receipt.</summary>
    Task<FiscalPrintResult?> PrintPartialRefundAsync(Guid printerId, FiscalRefundData refund, CancellationToken ct = default);

    /// <summary>Executes the daily fiscal closure (Z-report).</summary>
    Task<FiscalPrintResult?> DailyClosureAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Returns the most recent cached status of the specified printer.</summary>
    Task<FiscalPrinterStatus?> GetStatusAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Tests the connection to the specified printer.</summary>
    Task<FiscalPrintResult?> TestConnectionAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Opens the cash drawer connected to the specified printer.</summary>
    Task<FiscalPrintResult?> OpenDrawerAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Returns the health summary for the specified printer (live test + cached status).</summary>
    Task<FiscalPrinterHealthDto?> GetHealthAsync(Guid printerId, CancellationToken ct = default);
}
