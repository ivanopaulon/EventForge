using EventForge.DTOs.FiscalPrinting;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// High-level service for all operations on Custom fiscal printers.
/// Orchestrates communication, command building, response parsing,
/// and error handling for the full fiscal printing workflow.
/// </summary>
public interface IFiscalPrinterService
{
    /// <summary>
    /// Prints a complete fiscal receipt on the specified printer.
    /// Builds the full command sequence (open, items, discount/surcharge, payments, close)
    /// and sends it via the appropriate communication channel (TCP or serial).
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="receipt">Complete receipt data including items, payments, and optional loyalty info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="FiscalPrintResult"/> containing success status, receipt number,
    /// and error details if the operation failed.
    /// </returns>
    Task<FiscalPrintResult> PrintReceiptAsync(
        Guid printerId,
        FiscalReceiptData receipt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the currently open receipt on the specified printer (annullo scontrino).
    /// Only valid when a receipt is open. Returns an error result if no receipt is open.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> CancelCurrentReceiptAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints a full refund receipt (reso totale) referencing the original receipt.
    /// All items in <see cref="FiscalRefundData.Items"/> are printed with negative quantities.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="refund">Refund data including original receipt reference, items, and payments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> PrintRefundReceiptAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints a partial refund receipt, refunding only selected items from the original receipt.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="refund">Partial refund data. Only items listed are refunded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> PrintPartialRefundAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the daily fiscal closure (Z-report / chiusura giornaliera).
    /// This operation is irreversible and resets the daily totals.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and parses the current status of the specified printer (CMD_READ_STATUS "10").
    /// Returns a detailed <see cref="FiscalPrinterStatus"/> including paper, cover, fiscal memory,
    /// and operational flags.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrinterStatus> GetStatusAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the cash drawer connected to the specified fiscal printer (CMD_OPEN_DRAWER "40").
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> OpenDrawerAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the TCP/serial connection to the specified printer.
    /// Sends an ENQ frame and verifies the printer responds.
    /// </summary>
    /// <param name="printerId">Unique identifier of the <c>Printer</c> entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> TestConnectionAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);
}
