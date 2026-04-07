using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Routes <see cref="IFiscalPrinterService"/> calls to the protocol-specific implementation
/// based on the <c>ProtocolType</c> column of the target <see cref="Printer"/> entity.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <term>Custom</term>
///     <description>Routes to <see cref="CustomFiscalPrinterService"/> (Custom serial/TCP protocol).</description>
///   </item>
///   <item>
///     <term>Epson</term>
///     <description>Routes to <see cref="EpsonFiscalPrinterService"/> (Epson POS Printer WebAPI, ePOS-Print XML).</description>
///   </item>
/// </list>
/// <para>
/// Methods that do not require a printer ID (e.g. <see cref="TestTcpConnectionAsync"/>,
/// <see cref="ScanNetworkAsync"/>) are routed to the <see cref="CustomFiscalPrinterService"/>
/// as a neutral default unless the caller supplies explicit printer context.
/// For wizard flows that deal specifically with Epson printers, inject
/// <see cref="EpsonFiscalPrinterService"/> directly.
/// </para>
/// </remarks>
public sealed class FiscalPrinterServiceRouter(
    IServiceProvider serviceProvider,
    EventForgeDbContext context,
    ILogger<FiscalPrinterServiceRouter> logger) : IFiscalPrinterService
{
    // Cache of protocol type per printer ID (process lifetime, small bounded set)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string>
        _protocolCache = new();

    // -------------------------------------------------------------------------
    //  Protocol resolution
    // -------------------------------------------------------------------------

    private async Task<IFiscalPrinterService> ResolveAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        if (!_protocolCache.TryGetValue(printerId, out var protocol))
        {
            protocol = await context.Printers
                .AsNoTracking()
                .Where(p => p.Id == printerId && !p.IsDeleted)
                .Select(p => p.ProtocolType)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? "Custom";

            _protocolCache.TryAdd(printerId, protocol);
        }

        return ResolveByProtocol(protocol, printerId);
    }

    private IFiscalPrinterService ResolveByProtocol(string? protocol, Guid? printerId = null)
    {
        bool isEpson = string.Equals(protocol, "Epson", StringComparison.OrdinalIgnoreCase);

        if (isEpson)
        {
            logger.LogDebug("Router → EpsonFiscalPrinterService | PrinterId={Id}", printerId);
            return serviceProvider.GetRequiredService<EpsonFiscalPrinterService>();
        }

        logger.LogDebug("Router → CustomFiscalPrinterService | PrinterId={Id}", printerId);
        return serviceProvider.GetRequiredService<CustomFiscalPrinterService>();
    }

    // -------------------------------------------------------------------------
    //  IFiscalPrinterService – per-printer operations (require protocol lookup)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintReceiptAsync(
        Guid printerId, FiscalReceiptData receipt, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).PrintReceiptAsync(printerId, receipt, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> CancelCurrentReceiptAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).CancelCurrentReceiptAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintRefundReceiptAsync(
        Guid printerId, FiscalRefundData refund, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).PrintRefundReceiptAsync(printerId, refund, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintPartialRefundAsync(
        Guid printerId, FiscalRefundData refund, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).PrintPartialRefundAsync(printerId, refund, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).DailyClosureAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<FiscalPrinterStatus> GetStatusAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).GetStatusAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> OpenDrawerAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).OpenDrawerAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestConnectionAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).TestConnectionAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<DailyClosurePreCheckDto> GetDailyClosurePreCheckAsync(
        Guid printerId, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).GetDailyClosurePreCheckAsync(printerId, ct);

    /// <inheritdoc />
    public async Task<DailyClosureResultDto> ExecuteDailyClosureAsync(
        Guid printerId, string operatorName, CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct)).ExecuteDailyClosureAsync(printerId, operatorName, ct);

    /// <inheritdoc />
    public async Task<List<DailyClosureHistoryDto>> GetClosureHistoryAsync(
        Guid printerId, int page = 1, int pageSize = 20,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default)
        => await (await ResolveAsync(printerId, ct))
               .GetClosureHistoryAsync(printerId, page, pageSize, fromDate, toDate, ct);

    /// <inheritdoc />
    public async Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId, CancellationToken ct = default)
    {
        // ReprintZReport needs a closure ID, not a printer ID; look up the printer ID first
        var printerId = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .Select(r => (Guid?)r.PrinterId)
            .FirstOrDefaultAsync(ct);

        if (printerId is null)
            return new FiscalPrintResult { Success = false, ErrorMessage = $"Closure {closureId} not found" };

        return await (await ResolveAsync(printerId.Value, ct)).ReprintZReportAsync(closureId, ct);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GenerateZReportPdfAsync(
        Guid closureId, CancellationToken ct = default)
    {
        var printerId = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .Select(r => (Guid?)r.PrinterId)
            .FirstOrDefaultAsync(ct);

        if (printerId is null)
            return null;

        return await (await ResolveAsync(printerId.Value, ct)).GenerateZReportPdfAsync(closureId, ct);
    }

    // -------------------------------------------------------------------------
    //  IFiscalPrinterService – protocol-independent wizard operations
    //  Routed to Custom as neutral default; Epson wizard uses direct injection
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public Task<FiscalPrintResult> TestTcpConnectionAsync(
        string ipAddress, int port, CancellationToken ct = default)
        => serviceProvider.GetRequiredService<CustomFiscalPrinterService>()
               .TestTcpConnectionAsync(ipAddress, port, ct);

    /// <inheritdoc />
    public Task<FiscalPrintResult> TestSerialConnectionAsync(
        string serialPortName, int baudRate, CancellationToken ct = default)
        => serviceProvider.GetRequiredService<CustomFiscalPrinterService>()
               .TestSerialConnectionAsync(serialPortName, baudRate, ct);

    /// <inheritdoc />
    public Task<FiscalPrinterInfoDto> GetPrinterInfoByAddressAsync(
        string ipAddress, int port, CancellationToken ct = default)
        => serviceProvider.GetRequiredService<CustomFiscalPrinterService>()
               .GetPrinterInfoByAddressAsync(ipAddress, port, ct);

    /// <inheritdoc />
    public Task<List<NetworkScanResultDto>> ScanNetworkAsync(
        string subnetPrefix, int port = 9100, int timeoutMs = 300, CancellationToken ct = default)
        => serviceProvider.GetRequiredService<CustomFiscalPrinterService>()
               .ScanNetworkAsync(subnetPrefix, port, timeoutMs, ct);
}
