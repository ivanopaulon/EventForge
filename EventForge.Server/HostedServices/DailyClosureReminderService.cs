using EventForge.Server.Data;
using EventForge.Server.Hubs;
using EventForge.Server.Services.FiscalPrinting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service that monitors fiscal printers for a missing daily closure and
/// sends SignalR notifications at configured times.
/// </summary>
/// <remarks>
/// The service polls every minute and triggers two escalation levels:
/// <list type="bullet">
///   <item>
///     At <c>NotificationTime</c> (default <c>23:00</c>) a <see cref="FiscalPrinterHub.ClosureRequired"/>
///     event is pushed to all connected clients via SignalR.
///   </item>
///   <item>
///     At <c>CriticalAlertTime</c> (default <c>08:00</c> next day), if the daily closure was still
///     not performed, a <see cref="FiscalPrinterHub.CriticalClosureMissing"/> event is pushed.
///     The Custom protocol blocks all printing after 24 h without a closure.
///   </item>
/// </list>
/// Configuration section (appsettings.json):
/// <code>
/// "DailyClosureReminder": {
///   "NotificationTime": "23:00:00",
///   "CriticalAlertTime": "08:00:00"
/// }
/// </code>
/// </remarks>
public class DailyClosureReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<FiscalPrinterHub> _hubContext;
    private readonly ILogger<DailyClosureReminderService> _logger;
    private readonly TimeOnly _notificationTime;
    private readonly TimeOnly _criticalAlertTime;

    // Track which printers already received a notification / critical alert today
    // so we do not spam the same event multiple times within the same day.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime>
        _lastNotificationSent = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime>
        _lastCriticalAlertSent = new();

    public DailyClosureReminderService(
        IServiceProvider serviceProvider,
        IHubContext<FiscalPrinterHub> hubContext,
        IConfiguration configuration,
        ILogger<DailyClosureReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;

        var section = configuration.GetSection("DailyClosureReminder");

        _notificationTime = TimeOnly.TryParse(
            section["NotificationTime"],
            out var notif) ? notif : new TimeOnly(23, 0);

        _criticalAlertTime = TimeOnly.TryParse(
            section["CriticalAlertTime"],
            out var critical) ? critical : new TimeOnly(8, 0);

        _logger.LogInformation(
            "DailyClosureReminderService configured | NotificationTime={Notif} CriticalAlertTime={Critical}",
            _notificationTime, _criticalAlertTime);
    }

    // -------------------------------------------------------------------------
    //  BackgroundService
    // -------------------------------------------------------------------------

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyClosureReminderService started.");

        // Align to the next whole minute to avoid burst at service start
        var delay = 60 - DateTime.Now.Second;
        await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllPrintersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyClosureReminderService: unhandled error during check cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("DailyClosureReminderService stopped.");
    }

    // -------------------------------------------------------------------------
    //  Check logic
    // -------------------------------------------------------------------------

    private async Task CheckAllPrintersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var timeNow = TimeOnly.FromDateTime(now);

        // Window check: only act within 1-minute windows around the target times
        bool isNotificationWindow = Math.Abs(
            (timeNow - _notificationTime).TotalMinutes) < 1.5;
        bool isCriticalWindow = Math.Abs(
            (timeNow - _criticalAlertTime).TotalMinutes) < 1.5;

        if (!isNotificationWindow && !isCriticalWindow)
            return;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var fiscalPrinters = await context.Printers
            .AsNoTracking()
            .Where(p => p.IsFiscalPrinter && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var statusCache = scope.ServiceProvider.GetRequiredService<FiscalPrinterStatusCache>();

        foreach (var printer in fiscalPrinters)
        {
            var status = statusCache.GetCachedStatus(printer.Id);
            if (status is null || !status.IsDailyClosureRequired)
                continue;

            if (isNotificationWindow)
                await CheckAndNotifyClosureRequiredAsync(printer.Id, printer.Name, now, cancellationToken);

            if (isCriticalWindow)
                await CheckAndAlertMissingClosureAsync(printer.Id, printer.Name, now, cancellationToken);
        }
    }

    /// <summary>
    /// Sends a <see cref="FiscalPrinterHub.ClosureRequired"/> SignalR notification for the
    /// specified printer (at <see cref="_notificationTime"/>).
    /// Deduplicates notifications within the same calendar day.
    /// </summary>
    private async Task CheckAndNotifyClosureRequiredAsync(
        Guid printerId,
        string printerName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (_lastNotificationSent.TryGetValue(printerId, out var lastSent)
            && lastSent.Date == now.Date)
        {
            return; // already sent today
        }

        _logger.LogWarning(
            "DailyClosureReminderService: closure required notification | PrinterId={PrinterId} Name={Name}",
            printerId, printerName);

        var group = FiscalPrinterHub.PrinterGroupName(printerId);
        await _hubContext.Clients.Group(group)
            .SendAsync(FiscalPrinterHub.ClosureRequired, printerId, printerName, cancellationToken);

        _lastNotificationSent[printerId] = now;
    }

    /// <summary>
    /// Sends a <see cref="FiscalPrinterHub.CriticalClosureMissing"/> SignalR alert for the
    /// specified printer (at <see cref="_criticalAlertTime"/>).
    /// Deduplicates alerts within the same calendar day.
    /// </summary>
    private async Task CheckAndAlertMissingClosureAsync(
        Guid printerId,
        string printerName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (_lastCriticalAlertSent.TryGetValue(printerId, out var lastSent)
            && lastSent.Date == now.Date)
        {
            return; // already sent today
        }

        _logger.LogError(
            "DailyClosureReminderService: CRITICAL – closure missing | PrinterId={PrinterId} Name={Name}",
            printerId, printerName);

        var group = FiscalPrinterHub.PrinterGroupName(printerId);
        await _hubContext.Clients.Group(group)
            .SendAsync(FiscalPrinterHub.CriticalClosureMissing, printerId, printerName, cancellationToken);

        _lastCriticalAlertSent[printerId] = now;
    }
}
