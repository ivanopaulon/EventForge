using Prym.DTOs.FiscalPrinting;

namespace Prym.Web.Services;

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

    // ── Wizard endpoints ──────────────────────────────────────────────────────

    /// <summary>Tests a TCP connection to an arbitrary IP/port (wizard Step 2A).</summary>
    Task<FiscalPrintResult?> TestTcpConnectionAsync(string ipAddress, int port, CancellationToken ct = default);

    /// <summary>
    /// Tests TCP connectivity to a network printer on an agent's local network (wizard Step 2A – TcpViaAgent).
    /// The test is forwarded to the specified agent which opens the TCP socket.
    /// </summary>
    Task<FiscalPrintResult?> TestTcpViaAgentAsync(Guid agentId, string ipAddress, int port, CancellationToken ct = default);

    /// <summary>Tests a serial connection to an arbitrary port (wizard Step 2B).</summary>
    Task<FiscalPrintResult?> TestSerialConnectionAsync(string serialPortName, int baudRate, CancellationToken ct = default);

    /// <summary>Reads printer info (model, firmware, memory %) from an IP/port without a DB record.</summary>
    Task<FiscalPrinterInfoDto?> GetPrinterInfoByAddressAsync(string ipAddress, int port, CancellationToken ct = default);

    /// <summary>Scans the given subnet for devices responding on the fiscal printer port.</summary>
    Task<List<NetworkScanResultDto>?> ScanNetworkAsync(string subnetPrefix, int port = 9100, int timeoutMs = 300, CancellationToken ct = default);

    /// <summary>Saves the full wizard configuration (creates printer + associations).</summary>
    Task<Prym.DTOs.Station.PrinterDto?> SaveSetupAsync(FiscalPrinterSetupDto setup, CancellationToken ct = default);

    /// <summary>Loads the wizard setup payload for an existing printer (edit mode).</summary>
    Task<FiscalPrinterSetupDto?> GetSetupAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Updates an existing printer's configuration from the wizard (edit mode).</summary>
    Task<Prym.DTOs.Station.PrinterDto?> UpdateSetupAsync(Guid printerId, FiscalPrinterSetupDto setup, CancellationToken ct = default);

    // ── Daily closure workflow ────────────────────────────────────────────────

    /// <summary>Returns pre-check data before executing the daily closure.</summary>
    Task<DailyClosurePreCheckDto?> GetDailyClosurePreCheckAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>
    /// Lightweight "morning check": returns whether the previous business day's daily closure
    /// was performed. DB-only — safe to call even when the printer is offline.
    /// </summary>
    Task<PreviousDayClosureStatusDto?> GetPreviousDayClosureStatusAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>Executes the daily fiscal closure (Z-report) for the specified printer.</summary>
    Task<DailyClosureResultDto?> ExecuteDailyClosureAsync(Guid printerId, CancellationToken ct = default);

    /// <summary>
    /// Executes a daily closure for a POS terminal that has no fiscal printer configured.
    /// Saves totals to the database only (<c>ClosureType = NonFiscale</c>).
    /// </summary>
    Task<DailyClosureResultDto?> ExecuteNoPrinterDailyClosureAsync(Guid posId, CancellationToken ct = default);

    /// <summary>
    /// Retries the hardware fiscal Z-report for a closure record whose
    /// <c>FiscalClosurePending</c> flag is <c>true</c>.
    /// </summary>
    Task<DailyClosureResultDto?> RetryFiscalClosureAsync(Guid closureId, CancellationToken ct = default);

    /// <summary>Returns the history of daily closures for the specified printer.</summary>
    Task<List<DailyClosureHistoryDto>?> GetClosureHistoryAsync(
        Guid printerId, int page = 1, int pageSize = 20,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default);

    /// <summary>Reprints the Z-report for a previously executed closure.</summary>
    Task<FiscalPrintResult?> ReprintZReportAsync(Guid closureId, CancellationToken ct = default);

    /// <summary>Downloads the PDF Z-report for a closure. Returns null on failure.</summary>
    Task<byte[]?> DownloadClosurePdfAsync(Guid closureId, CancellationToken ct = default);

    // ── Agent proxy endpoints ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the list of system printers available on the machine running the specified agent.
    /// Proxied through the EventForge Server to the UpdateAgent's <c>/api/printer-proxy/system-printers</c>.
    /// Returns an empty list if the agent is unreachable or not configured.
    /// </summary>
    Task<List<string>> GetAgentSystemPrintersAsync(Guid agentId, CancellationToken ct = default);

    // ── Overdue closure tracking (SignalR push, consumed by MainLayout) ───────

    /// <summary>True when at least one printer has signalled an overdue daily closure.</summary>
    bool IsClosureOverdue { get; }

    /// <summary>Fired whenever <see cref="IsClosureOverdue"/> changes.</summary>
    event Action? ClosureStatusChanged;

    /// <summary>Sets <see cref="IsClosureOverdue"/> and fires <see cref="ClosureStatusChanged"/>.</summary>
    void SetClosureOverdue(bool value);
}
