using Prym.DTOs.Warehouse;
using EventForge.Server.Services.Configuration;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock alerts and notifications.
/// </summary>
public class StockAlertService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IConfigurationService configurationService,
    ILogger<StockAlertService> logger) : IStockAlertService
{

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
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = context.StockAlerts
                .AsNoTracking()
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
            throw;
        }
    }

    public async Task<StockAlertDto?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var alert = await context.StockAlerts
                .AsNoTracking()
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.Product)
                .Include(sa => sa.Stock)
                    .ThenInclude(s => s!.StorageLocation)
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.TenantId == currentTenantId.Value, cancellationToken);

            return alert?.ToStockAlertDto();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> GetActiveAlertsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var alerts = await context.StockAlerts
                .AsNoTracking()
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> GetActiveAlertsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var alerts = await context.StockAlerts
                .AsNoTracking()
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<StockAlertDto> CreateAlertAsync(CreateStockAlertDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

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

            _ = context.StockAlerts.Add(alert);
            _ = await context.SaveChangesAsync(cancellationToken);

            return (await GetAlertByIdAsync(alert.Id, cancellationToken))!;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert is null)
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

            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string? resolutionNotes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert is null)
            {
                return false;
            }

            alert.Status = AlertStatus.Resolved;
            alert.ResolvedDate = DateTime.UtcNow;
            alert.ResolvedBy = resolvedBy;
            alert.ResolutionNotes = resolutionNotes;
            alert.ModifiedAt = DateTime.UtcNow;
            alert.ModifiedBy = resolvedBy;

            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DismissAlertAsync(Guid alertId, string dismissedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert is null)
            {
                return false;
            }

            alert.Status = AlertStatus.Dismissed;
            alert.ResolutionNotes = reason ?? "Alert dismissed";
            alert.ModifiedAt = DateTime.UtcNow;
            alert.ModifiedBy = dismissedBy;

            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> CheckLowStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var lowStockItems = await context.Stocks
                .AsNoTracking()
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
                var existingAlert = await context.StockAlerts
                    .AsNoTracking()
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
                context.StockAlerts.AddRange(alerts);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            return alerts.Select(a => a.ToStockAlertDto());
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> CheckOverstockAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var overstockItems = await context.Stocks
                .AsNoTracking()
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
                var existingAlert = await context.StockAlerts
                    .AsNoTracking()
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
                context.StockAlerts.AddRange(alerts);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            return alerts.Select(a => a.ToStockAlertDto());
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> CheckExpiryAlertsAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var expiringDate = DateTime.UtcNow.AddDays(daysAhead);

            var expiringLots = await context.Lots
                .AsNoTracking()
                .Include(l => l.Product)
                .Where(l => l.TenantId == currentTenantId
                         && l.ExpiryDate.HasValue
                         && l.ExpiryDate.Value <= expiringDate
                         && l.Status == LotStatus.Active)
                .ToListAsync(cancellationToken);

            var alerts = new List<StockAlert>();

            foreach (var lot in expiringLots)
            {
                var stocks = await context.Stocks
                    .AsNoTracking()
                    .Include(s => s.StorageLocation)
                    .Where(s => s.LotId == lot.Id && s.AvailableQuantity > 0)
                    .ToListAsync(cancellationToken);

                foreach (var stock in stocks)
                {
                    // Check if alert already exists
                    var existingAlert = await context.StockAlerts
                        .AsNoTracking()
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
                context.StockAlerts.AddRange(alerts);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            return alerts.Select(a => a.ToStockAlertDto());
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<AlertCheckSummaryDto> RunAlertChecksAsync(CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<AlertStatisticsDto> GetAlertStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var alerts = await context.StockAlerts
                .AsNoTracking()
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> SendAlertNotificationsAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await context.StockAlerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert is null || !alert.SendEmailNotifications || string.IsNullOrEmpty(alert.NotificationEmails))
            {
                return false;
            }

            // Read SMTP settings from SystemConfigurations (same keys as ConfigurationService.TestSmtpAsync)
            var smtpServer = await configurationService.GetValueAsync("SMTP_Server", "localhost", cancellationToken);
            var smtpPortStr = await configurationService.GetValueAsync("SMTP_Port", "587", cancellationToken);
            var smtpPort = int.TryParse(smtpPortStr, out var parsedPort) ? parsedPort : 587;
            var smtpUsername = await configurationService.GetValueAsync("SMTP_Username", "", cancellationToken);
            var smtpPassword = await configurationService.GetValueAsync("SMTP_Password", "", cancellationToken);
            var smtpEnableSslStr = await configurationService.GetValueAsync("SMTP_EnableSSL", "true", cancellationToken);
            var smtpEnableSsl = !bool.TryParse(smtpEnableSslStr, out var parsedSsl) || parsedSsl;
            var smtpFromEmail = await configurationService.GetValueAsync("SMTP_FromEmail", "noreply@eventforge.com", cancellationToken);
            var smtpFromName = await configurationService.GetValueAsync("SMTP_FromName", "EventForge", cancellationToken);

            var subject = $"[Stock Alert] {alert.AlertType} - {alert.Message}";
            var body = $"""
                Stock Alert Notification

                Alert Type  : {alert.AlertType}
                Severity    : {alert.Severity}
                Message     : {alert.Message}
                Current Level: {alert.CurrentLevel}
                Threshold   : {alert.Threshold}
                Triggered   : {alert.TriggeredDate:yyyy-MM-dd HH:mm:ss} UTC
                Notification: #{alert.NotificationCount + 1}
                """;

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = smtpEnableSsl;
            if (!string.IsNullOrEmpty(smtpUsername))
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(smtpFromEmail, smtpFromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            // Support comma-separated email addresses
            foreach (var email in alert.NotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(email))
                    message.To.Add(email);
            }

            if (message.To.Count == 0)
                return false;

            await client.SendMailAsync(message, cancellationToken);

            alert.LastNotificationDate = DateTime.UtcNow;
            alert.NotificationCount++;
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Stock alert email notification #{Count} sent for alert {AlertId} to {Emails}",
                alert.NotificationCount, alertId, alert.NotificationEmails);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email notification for stock alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> GetAlertsForNotificationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var alerts = await context.StockAlerts
                .AsNoTracking()
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<int> BulkAcknowledgeAlertsAsync(IEnumerable<Guid> alertIds, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<int> CleanupOldAlertsAsync(int olderThanDays = 90, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var oldAlerts = await context.StockAlerts
                .Where(sa => sa.TenantId == currentTenantId
                          && sa.Status == AlertStatus.Resolved
                          && sa.ResolvedDate.HasValue
                          && sa.ResolvedDate.Value < cutoffDate)
                .ToListAsync(cancellationToken);

            context.StockAlerts.RemoveRange(oldAlerts);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cleaned up {Count} old resolved alerts", oldAlerts.Count);

            return oldAlerts.Count;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
