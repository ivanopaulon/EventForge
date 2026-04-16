using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Station;

namespace Prym.Web.Services;

/// <summary>
/// HTTP client wrapper for the fiscal printing API.
/// Delegates all calls to <c>/api/v1/fiscal-printing/*</c> via
/// <see cref="IHttpClientService"/>.
/// </summary>
/// <remarks>
/// Registered as <b>Scoped</b> (see <c>Program.cs</c>).
/// Each method returns <c>null</c> on non-critical HTTP errors (e.g., 404, 503)
/// so the caller can decide whether to show a fallback UI or toast notification.
/// </remarks>
public class FiscalPrintingService(
    IHttpClientService httpClientService,
    ILogger<FiscalPrintingService> logger) : IFiscalPrintingService
{
    private const string BaseUrl = "api/v1/fiscal-printing";

    // -------------------------------------------------------------------------
    //  Print / cancel receipt
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> PrintReceiptAsync(
        Guid printerId,
        FiscalReceiptData receipt,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<FiscalReceiptData, FiscalPrintResult>(
                $"{BaseUrl}/print-receipt?printerId={printerId}", receipt, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PrintReceiptAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> CancelCurrentReceiptAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/cancel-receipt/{printerId}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "CancelCurrentReceiptAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    // -------------------------------------------------------------------------
    //  Refunds
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> PrintRefundReceiptAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<FiscalRefundData, FiscalPrintResult>(
                $"{BaseUrl}/print-refund?printerId={printerId}", refund, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PrintRefundReceiptAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> PrintPartialRefundAsync(
        Guid printerId,
        FiscalRefundData refund,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<FiscalRefundData, FiscalPrintResult>(
                $"{BaseUrl}/partial-refund?printerId={printerId}", refund, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PrintPartialRefundAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> DailyClosureAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/daily-closure/{printerId}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "DailyClosureAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    // -------------------------------------------------------------------------
    //  Status / health / test / drawer
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrinterStatus?> GetStatusAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FiscalPrinterStatus>(
                $"{BaseUrl}/status/{printerId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetStatusAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> TestConnectionAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/test/{printerId}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TestConnectionAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> OpenDrawerAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/open-drawer/{printerId}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenDrawerAsync failed for printer {PrinterId}", printerId);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrinterHealthDto?> GetHealthAsync(
        Guid printerId,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FiscalPrinterHealthDto>(
                $"{BaseUrl}/health/{printerId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetHealthAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    // ── Wizard endpoints ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> TestTcpConnectionAsync(
        string ipAddress, int port, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/test-tcp?ipAddress={Uri.EscapeDataString(ipAddress)}&port={port}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TestTcpConnectionAsync failed for {IpAddress}:{Port}", ipAddress, port);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> TestTcpViaAgentAsync(
        Guid agentId, string ipAddress, int port, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/test-tcp-via-agent?agentId={agentId}&ipAddress={Uri.EscapeDataString(ipAddress)}&port={port}",
                new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TestTcpViaAgentAsync failed for agent={AgentId} {Ip}:{Port}", agentId, ipAddress, port);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> TestSerialConnectionAsync(
        string serialPortName, int baudRate, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/test-serial?serialPortName={Uri.EscapeDataString(serialPortName)}&baudRate={baudRate}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TestSerialConnectionAsync failed for {Port}", serialPortName);
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrinterInfoDto?> GetPrinterInfoByAddressAsync(
        string ipAddress, int port, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FiscalPrinterInfoDto>(
                $"{BaseUrl}/printer-info?ipAddress={Uri.EscapeDataString(ipAddress)}&port={port}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetPrinterInfoByAddressAsync failed for {IpAddress}:{Port}", ipAddress, port);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<NetworkScanResultDto>?> ScanNetworkAsync(
        string subnetPrefix, int port = 9100, int timeoutMs = 300, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<NetworkScanResultDto>>(
                $"{BaseUrl}/scan-network?subnetPrefix={Uri.EscapeDataString(subnetPrefix)}&port={port}&timeoutMs={timeoutMs}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ScanNetworkAsync failed for subnet {Subnet}", subnetPrefix);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PrinterDto?> SaveSetupAsync(FiscalPrinterSetupDto setup, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<FiscalPrinterSetupDto, PrinterDto>(
                $"{BaseUrl}/setup", setup, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "SaveSetupAsync failed");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrinterSetupDto?> GetSetupAsync(Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FiscalPrinterSetupDto>(
                $"{BaseUrl}/setup/{printerId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetSetupAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PrinterDto?> UpdateSetupAsync(Guid printerId, FiscalPrinterSetupDto setup, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<FiscalPrinterSetupDto, PrinterDto>(
                $"{BaseUrl}/setup/{printerId}", setup, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "UpdateSetupAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    // ── Daily closure workflow ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<DailyClosurePreCheckDto?> GetDailyClosurePreCheckAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<DailyClosurePreCheckDto>(
                $"{BaseUrl}/daily-closure/precheck/{printerId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetDailyClosurePreCheckAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DailyClosureResultDto?> ExecuteDailyClosureAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, DailyClosureResultDto>(
                $"{BaseUrl}/daily-closure/execute/{printerId}", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ExecuteDailyClosureAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<DailyClosureHistoryDto>?> GetClosureHistoryAsync(
        Guid printerId, int page = 1, int pageSize = 20,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/closures/{printerId}?page={page}&pageSize={pageSize}";
            if (fromDate.HasValue)
                url += $"&fromDate={fromDate.Value:o}";
            if (toDate.HasValue)
                url += $"&toDate={toDate.Value:o}";
            return await httpClientService.GetAsync<List<DailyClosureHistoryDto>>(url, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GetClosureHistoryAsync failed for printer {PrinterId}", printerId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult?> ReprintZReportAsync(
        Guid closureId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, FiscalPrintResult>(
                $"{BaseUrl}/closures/{closureId}/reprint", new { }, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ReprintZReportAsync failed for closure {ClosureId}", closureId);
            return null;
        }
    }

    // ── Overdue closure tracking ──────────────────────────────────────────────

    private bool _isClosureOverdue;

    /// <inheritdoc />
    public bool IsClosureOverdue => _isClosureOverdue;

    /// <inheritdoc />
    public event Action? ClosureStatusChanged;

    /// <inheritdoc />
    public void SetClosureOverdue(bool value)
    {
        _isClosureOverdue = value;
        ClosureStatusChanged?.Invoke();
    }

    public async Task<byte[]?> DownloadClosurePdfAsync(
        Guid closureId, CancellationToken ct = default)
    {
        try
        {
            var stream = await httpClientService.GetStreamAsync(
                $"{BaseUrl}/closures/{closureId}/pdf", ct);
            using var ms = new System.IO.MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "DownloadClosurePdfAsync failed for closure {ClosureId}", closureId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAgentSystemPrintersAsync(Guid agentId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<AgentSystemPrintersResult>(
                $"{BaseUrl}/agent-system-printers?agentId={agentId}", ct);
            return result?.Printers ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetAgentSystemPrintersAsync failed for agent {AgentId}", agentId);
            return [];
        }
    }

    /// <summary>Internal DTO matching the server's AgentSystemPrintersResponse payload.</summary>
    private sealed record AgentSystemPrintersResult(List<string>? Printers);
}
