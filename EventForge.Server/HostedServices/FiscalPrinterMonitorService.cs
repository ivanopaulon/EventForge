using EventForge.Server.Data;
using EventForge.Server.Hubs;
using EventForge.Server.Services.FiscalPrinting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service that periodically polls all active fiscal printers for their
/// current status (CMD_READ_STATUS "10") and stores the results in
/// <see cref="FiscalPrinterStatusCache"/>.
/// </summary>
/// <remarks>
/// Polling interval is configured via <c>appsettings.json</c>:
/// <code>
/// "FiscalPrinterMonitor": { "PollingIntervalSeconds": 10 }
/// </code>
/// Default is 10 seconds.
/// A new DI scope is created on each poll cycle so that scoped services
/// (e.g., <see cref="IFiscalPrinterService"/>) are resolved correctly.
/// </remarks>
public class FiscalPrinterMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FiscalPrinterStatusCache _statusCache;
    private readonly IHubContext<FiscalPrinterHub> _hubContext;
    private readonly ILogger<FiscalPrinterMonitorService> _logger;
    private readonly TimeSpan _pollingInterval;

    /// <summary>
    /// Initializes a new instance of <see cref="FiscalPrinterMonitorService"/>.
    /// </summary>
    public FiscalPrinterMonitorService(
        IServiceProvider serviceProvider,
        FiscalPrinterStatusCache statusCache,
        IHubContext<FiscalPrinterHub> hubContext,
        ILogger<FiscalPrinterMonitorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _statusCache = statusCache;
        _hubContext = hubContext;
        _logger = logger;

        int intervalSeconds = configuration
            .GetValue<int>("FiscalPrinterMonitor:PollingIntervalSeconds", 10);

        if (intervalSeconds < 5) intervalSeconds = 5; // enforce minimum
        _pollingInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    // -------------------------------------------------------------------------
    //  BackgroundService
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FiscalPrinterMonitorService started. Polling interval: {Interval}",
            _pollingInterval);

        // Initial delay to allow the application to fully start before the first poll
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollAllPrintersAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("FiscalPrinterMonitorService stopped.");
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private async Task PollAllPrintersAsync(CancellationToken stoppingToken)
    {
        List<(Guid Id, string Name)> fiscalPrinters;

        // Resolve DbContext in a new scope to avoid long-lived context issues
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();
            var raw = await db.Printers
                .AsNoTracking()
                .Where(p => p.IsFiscalPrinter)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(stoppingToken)
                .ConfigureAwait(false);

            fiscalPrinters = raw.Select(x => (x.Id, x.Name)).ToList();
        }

        if (fiscalPrinters.Count == 0)
        {
            _logger.LogDebug("FiscalPrinterMonitorService: no fiscal printers found.");
            return;
        }

        _logger.LogDebug(
            "FiscalPrinterMonitorService polling {Count} printer(s).", fiscalPrinters.Count);

        foreach (var (id, name) in fiscalPrinters)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await PollPrinterAsync(id, name, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PollPrinterAsync(Guid printerId, string printerName, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var fiscalService = scope.ServiceProvider.GetRequiredService<IFiscalPrinterService>();

            var status = await fiscalService
                .GetStatusAsync(printerId, stoppingToken)
                .ConfigureAwait(false);

            _statusCache.UpdateStatus(printerId, status);

            // ── Notify all SignalR clients subscribed to this printer ──────────
            var groupName = FiscalPrinterHub.PrinterGroupName(printerId);

            await _hubContext.Clients.Group(groupName)
                .SendAsync(FiscalPrinterHub.PrinterStatusUpdated, printerId, status, stoppingToken)
                .ConfigureAwait(false);

            // ── Log and emit specialised events ────────────────────────────────
            if (!status.IsOnline)
            {
                _logger.LogWarning(
                    "Fiscal printer {Name} ({Id}) is OFFLINE. Error: {Error}",
                    printerName, printerId, status.LastError);
            }
            else if (status.IsFiscalMemoryFull)
            {
                _logger.LogCritical(
                    "Fiscal printer {Name} ({Id}) – FISCAL MEMORY FULL. " +
                    "Printing is blocked. Requires authorised technical intervention.",
                    printerName, printerId);

                await _hubContext.Clients.Group(groupName)
                    .SendAsync(FiscalPrinterHub.CriticalClosureMissing, printerId, stoppingToken)
                    .ConfigureAwait(false);
            }
            else if (status.IsFiscalMemoryAlmostFull)
            {
                _logger.LogWarning(
                    "Fiscal printer {Name} ({Id}) – fiscal memory almost full (>90%).",
                    printerName, printerId);
            }
            else if (status.IsDailyClosureRequired)
            {
                _logger.LogWarning(
                    "Fiscal printer {Name} ({Id}) – daily fiscal closure required.",
                    printerName, printerId);

                await _hubContext.Clients.Group(groupName)
                    .SendAsync(FiscalPrinterHub.ClosureRequired, printerId, printerName, stoppingToken)
                    .ConfigureAwait(false);
            }
            else if (status.IsPaperOut)
            {
                _logger.LogWarning(
                    "Fiscal printer {Name} ({Id}) – paper OUT. Replace the roll.",
                    printerName, printerId);
            }
            else if (status.IsPaperLow)
            {
                _logger.LogWarning(
                    "Fiscal printer {Name} ({Id}) – paper LOW. Replace the roll soon.",
                    printerName, printerId);
            }
            else
            {
                _logger.LogDebug(
                    "Fiscal printer {Name} ({Id}) – OK.", printerName, printerId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "FiscalPrinterMonitorService: unexpected error polling printer {Name} ({Id}).",
                printerName, printerId);
        }
    }
}
