using Prym.DTOs.FiscalPrinting;
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
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(protocol))
            {
                logger.LogWarning(
                    "Router: no ProtocolType found for printer {PrinterId}; falling back to Custom protocol. " +
                    "Set ProtocolType on the Printer entity to suppress this warning.",
                    printerId);
                protocol = "Custom";
            }

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
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).PrintReceiptAsync(printerId, receipt, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> CancelCurrentReceiptAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).CancelCurrentReceiptAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintRefundReceiptAsync(
        Guid printerId, FiscalRefundData refund, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).PrintRefundReceiptAsync(printerId, refund, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> PrintPartialRefundAsync(
        Guid printerId, FiscalRefundData refund, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).PrintPartialRefundAsync(printerId, refund, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> DailyClosureAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).DailyClosureAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrinterStatus> GetStatusAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).GetStatusAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> OpenDrawerAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).OpenDrawerAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestConnectionAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).TestConnectionAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// DB-only check – implemented directly in the router; does not delegate to
    /// printer-protocol-specific implementations.
    /// </remarks>
    public async Task<PreviousDayClosureStatusDto> GetPreviousDayClosureStatusAsync(
        Guid printerId, CancellationToken ct = default)
    {
        var previousDay = DateTime.UtcNow.Date.AddDays(-1);

        var lastClosure = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.PrinterId == printerId && !r.IsDeleted)
            .OrderByDescending(r => r.ClosedAt)
            .Select(r => (DateTime?)r.ClosedAt)
            .FirstOrDefaultAsync(ct);

        return new PreviousDayClosureStatusDto
        {
            PreviousBusinessDay = previousDay,
            LastClosureDate = lastClosure,
            IsPreviousDayClosureMissing = lastClosure == null || lastClosure.Value.Date < previousDay
        };
    }

    /// <inheritdoc />
    public async Task<DailyClosurePreCheckDto> GetDailyClosurePreCheckAsync(
        Guid printerId, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).GetDailyClosurePreCheckAsync(printerId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DailyClosureResultDto> ExecuteDailyClosureAsync(
        Guid printerId, string operatorName, CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct)).ExecuteDailyClosureAsync(printerId, operatorName, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DailyClosureHistoryDto>> GetClosureHistoryAsync(
        Guid printerId, int page = 1, int pageSize = 20,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            return await (await ResolveAsync(printerId, ct))
                       .GetClosureHistoryAsync(printerId, page, pageSize, fromDate, toDate, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Implemented directly in the router via the shared <see cref="EventForgeDbContext"/> so that
    /// records with <c>PrinterId == Guid.Empty</c> (non-fiscal closures) are included.
    /// </remarks>
    public async Task<List<DailyClosureHistoryDto>> GetAllClosureHistoryAsync(
        int page = 1, int pageSize = 50,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => !r.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(r => r.ClosedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.ClosedAt <= toDate.Value);

        var records = await query
            .OrderByDescending(r => r.ClosedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Batch-load printer names for all real printer IDs
        var printerIds = records
            .Select(r => r.PrinterId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var printerNames = printerIds.Count > 0
            ? await context.Printers
                .AsNoTracking()
                .Where(p => printerIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        return records.Select(r => new DailyClosureHistoryDto
        {
            Id = r.Id,
            PrinterId = r.PrinterId,
            PrinterName = r.PrinterId != Guid.Empty && printerNames.TryGetValue(r.PrinterId, out var name)
                ? name
                : "—",
            ZReportNumber = r.ZReportNumber,
            ClosedAt = r.ClosedAt,
            ReceiptCount = r.ReceiptCount,
            TotalAmount = r.TotalAmount,
            CashAmount = r.CashAmount,
            CardAmount = r.CardAmount,
            Operator = r.Operator,
            HasPdf = r.HasPdf,
            ClosureType = Enum.TryParse<ClosureType>(r.ClosureType, out var closureType)
                ? closureType
                : ClosureType.Fiscale,
            FiscalClosurePending = r.FiscalClosurePending
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> ReprintZReportAsync(
        Guid closureId, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GenerateZReportPdfAsync(
        Guid closureId, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
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

    // -------------------------------------------------------------------------
    //  RetryFiscalClosureAsync
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<DailyClosureResultDto> RetryFiscalClosureAsync(
        Guid closureId, CancellationToken ct = default)
    {
        var printerId = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(r => r.Id == closureId && !r.IsDeleted)
            .Select(r => (Guid?)r.PrinterId)
            .FirstOrDefaultAsync(ct);

        if (printerId is null)
            return new DailyClosureResultDto { Success = false, ErrorMessage = $"Closure {closureId} not found." };

        return await (await ResolveAsync(printerId.Value, ct)).RetryFiscalClosureAsync(closureId, ct);
    }

    // -------------------------------------------------------------------------
    //  No-printer daily closure (DB-only / NonFiscale)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes a daily closure when no fiscal printer is configured for the POS terminal.
    /// Aggregates today's session totals from the database and saves a <see cref="DailyClosureRecord"/>
    /// with <see cref="ClosureType.NonFiscale"/> and <see cref="DailyClosureRecord.FiscalClosurePending"/> = <c>false</c>.
    /// </summary>
    public async Task<DailyClosureResultDto> ExecuteNoPrinterDailyClosureAsync(
        Guid posId,
        string operatorName,
        CancellationToken ct = default)
    {
        logger.LogInformation("ExecuteNoPrinterDailyClosureAsync | PosId={PosId} Operator={Op}", posId, operatorName);

        var closedAt = DateTime.UtcNow;
        var todayStart = closedAt.Date;

        // Resolve tenant from the POS record
        var pos = await context.StorePoses
            .AsNoTracking()
            .Where(p => p.Id == posId && !p.IsDeleted)
            .Select(p => new { p.Id, p.TenantId })
            .FirstOrDefaultAsync(ct);

        if (pos is null)
            return new DailyClosureResultDto { Success = false, ErrorMessage = $"POS {posId} non trovato." };

        // Aggregate today's sessions for this POS
        var sessions = await context.SaleSessions
            .AsNoTracking()
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Where(s => s.PosId == posId
                     && !s.IsDeleted
                     && s.Status == EventForge.Server.Data.Entities.Sales.SaleSessionStatus.Closed
                     && s.ClosedAt.HasValue && s.ClosedAt.Value.Date == todayStart)
            .ToListAsync(ct);

        decimal totalAmount = 0m, cashAmount = 0m, cardAmount = 0m;
        int receiptCount = sessions.Count;
        totalAmount = sessions.Sum(s => s.FinalTotal);

        foreach (var session in sessions)
            foreach (var payment in session.Payments)
            {
                var code = payment.PaymentMethod?.Code?.ToUpperInvariant() ?? string.Empty;
                // FiscalCode 1 = cash; any other recognised code = card/electronic
                bool isCash = payment.PaymentMethod?.FiscalCode == 1
                           || code is "CASH" or "CONTANTI" or "CONTANTE";
                if (isCash) cashAmount += payment.Amount;
                else cardAmount += payment.Amount;
            }

        // There is no physical fiscal printer, so Z-number is 0 (not applicable)
        var record = new Data.Entities.FiscalPrinting.DailyClosureRecord
        {
            PrinterId = Guid.Empty,
            TenantId = pos.TenantId,
            ZReportNumber = 0,
            ClosedAt = closedAt,
            ReceiptCount = receiptCount,
            TotalAmount = totalAmount,
            CashAmount = cashAmount,
            CardAmount = cardAmount,
            Operator = operatorName,
            ClosureType = "NonFiscale",
            FiscalClosurePending = false,
            HasPdf = false,
            CreatedBy = operatorName
        };

        context.DailyClosureRecords.Add(record);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "NoPrinter DailyClosure saved | PosId={PosId} ClosureId={ClosureId} Type=NonFiscale Operator={Op}",
            posId, record.Id, operatorName);

        return new DailyClosureResultDto
        {
            Success = true,
            ClosureId = record.Id,
            ZReportNumber = null,
            ClosedAt = closedAt,
            ReceiptCount = receiptCount,
            TotalAmount = totalAmount,
            CashAmount = cashAmount,
            Operator = operatorName,
            ClosureType = ClosureType.NonFiscale,
            FiscalClosurePending = false
        };
    }
}
