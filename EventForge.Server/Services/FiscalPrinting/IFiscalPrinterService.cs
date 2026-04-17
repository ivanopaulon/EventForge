using Prym.DTOs.FiscalPrinting;

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

    /// <summary>
    /// Tests a TCP connection to an arbitrary IP/port without requiring a printer record in the DB.
    /// Used by the setup wizard (Step 2A) before the printer is saved.
    /// </summary>
    /// <param name="ipAddress">IP address to connect to.</param>
    /// <param name="port">TCP port to connect to (typically 9100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> TestTcpConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a serial connection to an arbitrary port without requiring a printer record in the DB.
    /// Used by the setup wizard (Step 2B) before the printer is saved.
    /// </summary>
    /// <param name="serialPortName">Serial port name (e.g., COM1, /dev/ttyUSB0).</param>
    /// <param name="baudRate">Baud rate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrintResult> TestSerialConnectionAsync(
        string serialPortName,
        int baudRate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads basic info (model, serial, firmware, memory %) from a printer
    /// identified by IP/port, without requiring a DB record.
    /// Used by the setup wizard (Step 3).
    /// </summary>
    /// <param name="ipAddress">IP address.</param>
    /// <param name="port">TCP port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FiscalPrinterInfoDto> GetPrinterInfoByAddressAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans the specified subnet for devices responding on the fiscal printer port (default 9100).
    /// Results are returned as soon as they are discovered (streaming-style via IAsyncEnumerable is
    /// not used here to keep it REST-friendly; all results are collected and returned at once).
    /// </summary>
    /// <param name="subnetPrefix">Subnet prefix, e.g., "192.168.1" (scans .1 to .254).</param>
    /// <param name="port">Port to probe (default 9100).</param>
    /// <param name="timeoutMs">Per-host connection timeout in milliseconds (default 300 ms).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<NetworkScanResultDto>> ScanNetworkAsync(
        string subnetPrefix,
        int port = 9100,
        int timeoutMs = 300,
        CancellationToken cancellationToken = default);

    // ── Daily closure workflow ────────────────────────────────────────────────

    /// <summary>
    /// Lightweight "morning check": returns whether the previous business day's closure
    /// was performed. This is a DB-only query (no printer hardware communication) and
    /// is safe to call even when the printer is offline.
    /// </summary>
    Task<PreviousDayClosureStatusDto> GetPreviousDayClosureStatusAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether it is safe to execute the daily fiscal closure for the specified printer.
    /// Returns a summary of today's receipts and warns if an open receipt exists.
    /// </summary>
    Task<DailyClosurePreCheckDto> GetDailyClosurePreCheckAsync(
        Guid printerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the daily fiscal closure (Z-report) and records the result in the closure history.
    /// </summary>
    Task<DailyClosureResultDto> ExecuteDailyClosureAsync(
        Guid printerId,
        string operatorName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the paginated history of daily closures for the specified printer.
    /// </summary>
    Task<List<DailyClosureHistoryDto>> GetClosureHistoryAsync(
        Guid printerId,
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprints the Z-report for a previously executed closure.
    /// </summary>
    Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates (or returns a cached copy of) the PDF Z-report for the specified closure.
    /// On first call the PDF is generated with QuestPDF and persisted to the DB.
    /// Subsequent calls return the stored bytes.
    /// </summary>
    /// <returns>PDF bytes, or <c>null</c> if the closure was not found.</returns>
    Task<byte[]?> GenerateZReportPdfAsync(
        Guid closureId,
        CancellationToken cancellationToken = default);
}
