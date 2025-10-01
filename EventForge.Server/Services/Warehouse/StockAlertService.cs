using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock alerts and notifications.
/// </summary>
public class StockAlertService : IStockAlertService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StockAlertService> _logger;

    public StockAlertService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<StockAlertService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StockAlertDto>> GetAlertsAsync(
        int page = 1,
        int pageSize = 20,
        string? alertType = null,
        string? severity = null,
        string? status = null,
        Guid? productId = null,
        Guid? locationId = null,
        bool? acknowledged = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.StockAlerts
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.Product)
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.StorageLocation)
                .Where(sa => sa.TenantId == currentTenantId.Value);

            // Apply filters
            if (!string.IsNullOrEmpty(alertType) && Enum.TryParse<StockAlertType>(alertType, true, out var type))
            {
                query = query.Where(sa => sa.AlertType == type);
            }

            if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AlertSeverity>(severity, true, out var sev))
            {
                query = query.Where(sa => sa.Severity == sev);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AlertStatus>(status, true, out var stat))
            {
                query = query.Where(sa => sa.Status == stat);
            }

            if (productId.HasValue)
            {
                query = query.Where(sa => sa.Stock!.ProductId == productId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(sa => sa.Stock!.StorageLocationId == locationId.Value);
            }

            if (acknowledged.HasValue)
            {
                if (acknowledged.Value)
                {
                    query = query.Where(sa => sa.AcknowledgedDate.HasValue);
                }
                else
                {
                    query = query.Where(sa => !sa.AcknowledgedDate.HasValue);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var alerts = await query
                .OrderByDescending(sa => sa.TriggeredDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var alertDtos = alerts.Select(a => a.ToStockAlertDto()).ToList();

            return new PagedResult<StockAlertDto>
            {
                Items = alertDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock alerts with filters");
            throw;
        }
    }

    public async Task<StockAlertDto?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var alert = await _context.StockAlerts
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.Product)
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.StorageLocation)
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.TenantId == currentTenantId.Value, cancellationToken);

            return alert?.ToStockAlertDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock alert by ID: {AlertId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> GetActiveAlertsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var alerts = await _context.StockAlerts
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.Product)
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.StorageLocation)
            .Where(sa => sa.TenantId == currentTenantId
                      && sa.Stock!.ProductId == productId
                      && sa.Status == AlertStatus.Active)
            .OrderByDescending(sa => sa.TriggeredDate)
            .ToListAsync(cancellationToken);

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<IEnumerable<StockAlertDto>> GetActiveAlertsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var alerts = await _context.StockAlerts
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.Product)
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.StorageLocation)
            .Where(sa => sa.TenantId == currentTenantId
                      && sa.Stock!.StorageLocationId == locationId
                      && sa.Status == AlertStatus.Active)
            .OrderByDescending(sa => sa.TriggeredDate)
            .ToListAsync(cancellationToken);

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<StockAlertDto> CreateAlertAsync(CreateStockAlertDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var alert = new StockAlert
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            StockId = createDto.StockId,
            AlertType = Enum.Parse<StockAlertType>(createDto.AlertType),
            Severity = Enum.Parse<AlertSeverity>(createDto.Severity),
            CurrentLevel = createDto.CurrentLevel,
            Threshold = createDto.Threshold,
            Message = createDto.Message,
            Status = AlertStatus.Active,
            TriggeredDate = DateTime.UtcNow,
            SendEmailNotifications = createDto.SendEmailNotifications,
            NotificationEmails = createDto.NotificationEmails,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        _context.StockAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);


        return (await GetAlertByIdAsync(alert.Id, cancellationToken))!;
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        var alert = await _context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
        if (alert == null)
        {
            return false;
        }

        alert.AcknowledgedDate = DateTime.UtcNow;
        alert.AcknowledgedBy = acknowledgedBy;
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = acknowledgedBy;

        if (!string.IsNullOrEmpty(notes))
        {
            alert.ResolutionNotes = notes;
        }

        await _context.SaveChangesAsync(cancellationToken);


        return true;
    }

    public async Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string? resolutionNotes = null, CancellationToken cancellationToken = default)
    {
        var alert = await _context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
        if (alert == null)
        {
            return false;
        }

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedDate = DateTime.UtcNow;
        alert.ResolvedBy = resolvedBy;
        alert.ResolutionNotes = resolutionNotes;
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = resolvedBy;

        await _context.SaveChangesAsync(cancellationToken);


        return true;
    }

    public async Task<bool> DismissAlertAsync(Guid alertId, string dismissedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        var alert = await _context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
        if (alert == null)
        {
            return false;
        }

        alert.Status = AlertStatus.Dismissed;
        alert.ResolutionNotes = reason ?? "Alert dismissed";
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = dismissedBy;

        await _context.SaveChangesAsync(cancellationToken);


        return true;
    }

    public async Task<IEnumerable<StockAlertDto>> CheckLowStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var lowStockItems = await _context.Stocks
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
            .Where(s => s.TenantId == currentTenantId
                     && s.MinimumLevel.HasValue
                     && s.AvailableQuantity <= s.MinimumLevel.Value)
            .ToListAsync(cancellationToken);

        var alerts = new List<StockAlert>();

        foreach (var stock in lowStockItems)
        {
            // Check if alert already exists
            var existingAlert = await _context.StockAlerts
                .AnyAsync(sa => sa.StockId == stock.Id
                             && sa.AlertType == StockAlertType.LowStock
                             && sa.Status == AlertStatus.Active,
                          cancellationToken);

            if (!existingAlert)
            {
                var alert = new StockAlert
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId,
                    StockId = stock.Id,
                    AlertType = StockAlertType.LowStock,
                    Severity = stock.AvailableQuantity == 0 ? AlertSeverity.Critical : AlertSeverity.Warning,
                    CurrentLevel = stock.AvailableQuantity,
                    Threshold = stock.MinimumLevel ?? 0,
                    Message = $"Low stock alert for {stock.Product?.Name} at {stock.StorageLocation?.Code}. Current: {stock.AvailableQuantity}, Minimum: {stock.MinimumLevel}",
                    Status = AlertStatus.Active,
                    TriggeredDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                alerts.Add(alert);
            }
        }

        if (alerts.Any())
        {
            _context.StockAlerts.AddRange(alerts);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<IEnumerable<StockAlertDto>> CheckOverstockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var overstockItems = await _context.Stocks
            .Include(s => s.Product)
            .Include(s => s.StorageLocation)
            .Where(s => s.TenantId == currentTenantId
                     && s.MaximumLevel.HasValue
                     && s.Quantity >= s.MaximumLevel.Value)
            .ToListAsync(cancellationToken);

        var alerts = new List<StockAlert>();

        foreach (var stock in overstockItems)
        {
            // Check if alert already exists
            var existingAlert = await _context.StockAlerts
                .AnyAsync(sa => sa.StockId == stock.Id
                             && sa.AlertType == StockAlertType.HighStock
                             && sa.Status == AlertStatus.Active,
                          cancellationToken);

            if (!existingAlert)
            {
                var alert = new StockAlert
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId,
                    StockId = stock.Id,
                    AlertType = StockAlertType.HighStock,
                    Severity = AlertSeverity.Warning,
                    CurrentLevel = stock.Quantity,
                    Threshold = stock.MaximumLevel ?? 0,
                    Message = $"Overstock alert for {stock.Product?.Name} at {stock.StorageLocation?.Code}. Current: {stock.Quantity}, Maximum: {stock.MaximumLevel}",
                    Status = AlertStatus.Active,
                    TriggeredDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                alerts.Add(alert);
            }
        }

        if (alerts.Any())
        {
            _context.StockAlerts.AddRange(alerts);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<IEnumerable<StockAlertDto>> CheckExpiryAlertsAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var expiringDate = DateTime.UtcNow.AddDays(daysAhead);

        var expiringLots = await _context.Lots
            .Include(l => l.Product)
            .Where(l => l.TenantId == currentTenantId
                     && l.ExpiryDate.HasValue
                     && l.ExpiryDate.Value <= expiringDate
                     && l.Status == LotStatus.Active)
            .ToListAsync(cancellationToken);

        var alerts = new List<StockAlert>();

        foreach (var lot in expiringLots)
        {
            var stocks = await _context.Stocks
                .Include(s => s.StorageLocation)
                .Where(s => s.LotId == lot.Id && s.AvailableQuantity > 0)
                .ToListAsync(cancellationToken);

            foreach (var stock in stocks)
            {
                // Check if alert already exists
                var existingAlert = await _context.StockAlerts
                    .AnyAsync(sa => sa.StockId == stock.Id
                                 && sa.AlertType == StockAlertType.Expiry
                                 && sa.Status == AlertStatus.Active,
                              cancellationToken);

                if (!existingAlert)
                {
                    var daysUntilExpiry = (lot.ExpiryDate!.Value - DateTime.UtcNow).Days;
                    var alert = new StockAlert
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId,
                        StockId = stock.Id,
                        AlertType = StockAlertType.Expiry,
                        Severity = daysUntilExpiry <= 7 ? AlertSeverity.Critical : AlertSeverity.Warning,
                        CurrentLevel = daysUntilExpiry,
                        Threshold = daysAhead,
                        Message = $"Expiry alert for {lot.Product?.Name} (Lot {lot.Code}) at {stock.StorageLocation?.Code}. Expires in {daysUntilExpiry} days on {lot.ExpiryDate:yyyy-MM-dd}",
                        Status = AlertStatus.Active,
                        TriggeredDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    alerts.Add(alert);
                }
            }
        }

        if (alerts.Any())
        {
            _context.StockAlerts.AddRange(alerts);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<AlertCheckSummaryDto> RunAlertChecksAsync(CancellationToken cancellationToken = default)
    {
        var lowStockAlerts = await CheckLowStockAlertsAsync(cancellationToken);
        var overstockAlerts = await CheckOverstockAlertsAsync(cancellationToken);
        var expiryAlerts = await CheckExpiryAlertsAsync(30, cancellationToken);

        return new AlertCheckSummaryDto
        {
            CheckDateTime = DateTime.UtcNow,
            LowStockAlertsCreated = lowStockAlerts.Count(),
            OverstockAlertsCreated = overstockAlerts.Count(),
            ExpiryAlertsCreated = expiryAlerts.Count()
        };
    }

    public async Task<AlertStatisticsDto> GetAlertStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var alerts = await _context.StockAlerts
            .Where(sa => sa.TenantId == currentTenantId)
            .ToListAsync(cancellationToken);

        return new AlertStatisticsDto
        {
            TotalActiveAlerts = alerts.Count(a => a.Status == AlertStatus.Active),
            TotalAcknowledgedAlerts = alerts.Count(a => a.AcknowledgedDate.HasValue),
            TotalResolvedAlerts = alerts.Count(a => a.Status == AlertStatus.Resolved),
            TotalDismissedAlerts = alerts.Count(a => a.Status == AlertStatus.Dismissed),
            CriticalAlerts = alerts.Count(a => a.Severity == AlertSeverity.Critical && a.Status == AlertStatus.Active),
            WarningAlerts = alerts.Count(a => a.Severity == AlertSeverity.Warning && a.Status == AlertStatus.Active),
            InfoAlerts = alerts.Count(a => a.Severity == AlertSeverity.Info && a.Status == AlertStatus.Active),
            LowStockAlerts = alerts.Count(a => a.AlertType == StockAlertType.LowStock && a.Status == AlertStatus.Active),
            ExpiryAlerts = alerts.Count(a => a.AlertType == StockAlertType.Expiry && a.Status == AlertStatus.Active),
            OverstockAlerts = alerts.Count(a => a.AlertType == StockAlertType.HighStock && a.Status == AlertStatus.Active)
        };
    }

    public async Task<bool> SendAlertNotificationsAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
        if (alert == null || !alert.SendEmailNotifications || string.IsNullOrEmpty(alert.NotificationEmails))
        {
            return false;
        }

        // TODO: Integrate with email service
        // For now, just update the notification tracking
        alert.LastNotificationDate = DateTime.UtcNow;
        alert.NotificationCount++;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email notification sent for alert {AlertId} to {Emails}", alertId, alert.NotificationEmails);

        return true;
    }

    public async Task<IEnumerable<StockAlertDto>> GetAlertsForNotificationAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var alerts = await _context.StockAlerts
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.Product)
            .Include(sa => sa.Stock)
                .ThenInclude(s => s!.StorageLocation)
            .Where(sa => sa.TenantId == currentTenantId
                      && sa.Status == AlertStatus.Active
                      && sa.SendEmailNotifications
                      && (!sa.LastNotificationDate.HasValue || sa.LastNotificationDate.Value < DateTime.UtcNow.AddHours(-24)))
            .ToListAsync(cancellationToken);

        return alerts.Select(a => a.ToStockAlertDto());
    }

    public async Task<int> BulkAcknowledgeAlertsAsync(IEnumerable<Guid> alertIds, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var alertId in alertIds)
        {
            if (await AcknowledgeAlertAsync(alertId, acknowledgedBy, null, cancellationToken))
            {
                count++;
            }
        }

        return count;
    }

    public async Task<int> CleanupOldAlertsAsync(int olderThanDays = 90, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var oldAlerts = await _context.StockAlerts
            .Where(sa => sa.TenantId == currentTenantId
                      && sa.Status == AlertStatus.Resolved
                      && sa.ResolvedDate.HasValue
                      && sa.ResolvedDate.Value < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.StockAlerts.RemoveRange(oldAlerts);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} old resolved alerts", oldAlerts.Count);

        return oldAlerts.Count;
    }
}
