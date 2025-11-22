using EventForge.DTOs.Alerts;
using EventForge.Server.Data;
using AlertEntity = EventForge.Server.Data.Entities.Alerts.SupplierPriceAlert;
using AlertType = EventForge.Server.Data.Entities.Alerts.AlertType;
using AlertSeverity = EventForge.Server.Data.Entities.Alerts.AlertSeverity;
using AlertStatus = EventForge.Server.Data.Entities.Alerts.AlertStatus;
using AlertConfiguration = EventForge.Server.Data.Entities.Alerts.AlertConfiguration;
using AlertFrequency = EventForge.Server.Data.Entities.Alerts.AlertFrequency;
using EventForge.Server.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using EventForge.Server.Hubs;

namespace EventForge.Server.Services.Alerts;

/// <summary>
/// Implementation of supplier price alert service.
/// Part of FASE 5: Price Alerts System.
/// </summary>
public class SupplierPriceAlertService : ISupplierPriceAlertService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SupplierPriceAlertService> _logger;
    private readonly IHubContext<AlertHub>? _hubContext;

    public SupplierPriceAlertService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SupplierPriceAlertService> logger,
        IHubContext<AlertHub>? hubContext = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _hubContext = hubContext;
    }

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
    }

    public async Task<Guid> CreateAlertAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");

        // Parse enums
        if (!Enum.TryParse<AlertType>(request.AlertType, true, out var alertType))
        {
            throw new ArgumentException($"Invalid alert type: {request.AlertType}");
        }

        if (!Enum.TryParse<AlertSeverity>(request.Severity, true, out var severity))
        {
            throw new ArgumentException($"Invalid severity: {request.Severity}");
        }

        var alert = new AlertEntity
        {
            TenantId = tenantId,
            ProductId = request.ProductId,
            SupplierId = request.SupplierId,
            AlertType = alertType,
            Severity = severity,
            Status = AlertStatus.New,
            OldPrice = request.OldPrice,
            NewPrice = request.NewPrice,
            PriceChangePercentage = request.PriceChangePercentage,
            Currency = request.Currency,
            PotentialSavings = request.PotentialSavings,
            AlertTitle = request.AlertTitle,
            AlertMessage = request.AlertMessage,
            RecommendedAction = request.RecommendedAction,
            BetterSupplierSuggestionId = request.BetterSupplierSuggestionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId()
        };

        _context.SupplierPriceAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created alert {AlertId} for tenant {TenantId}", alert.Id, tenantId);

        // Broadcast via SignalR
        try
        {
            var alertDto = await MapToDto(alert, cancellationToken);
            if (_hubContext != null && alertDto != null)
            {
                await _hubContext.Clients.Group($"tenant-{tenantId}")
                    .SendAsync("NewAlert", alertDto, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast alert via SignalR");
        }

        return alert.Id;
    }

    public async Task<List<Guid>> GenerateAlertsForPriceChangeAsync(
        Guid productId, 
        Guid supplierId, 
        decimal oldPrice, 
        decimal newPrice, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var alertIds = new List<Guid>();

        if (oldPrice == 0)
        {
            return alertIds; // No alert for initial price
        }

        var priceChangePercentage = ((newPrice - oldPrice) / oldPrice) * 100;

        // Get product and supplier info
        var product = await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => new { p.Name, p.Code })
            .FirstOrDefaultAsync(cancellationToken);

        var supplier = await _context.BusinessParties
            .Where(s => s.Id == supplierId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null || supplier == null)
        {
            return alertIds;
        }

        // Get user configurations for this tenant
        var configs = await _context.AlertConfigurations
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // If no configurations exist, use default thresholds
        if (!configs.Any())
        {
            configs.Add(new AlertConfiguration { TenantId = tenantId, UserId = "default" });
        }

        foreach (var config in configs)
        {
            AlertType? alertType = null;
            AlertSeverity severity = AlertSeverity.Info;

            // Check for price increase
            if (priceChangePercentage > 0 && config.AlertOnPriceIncrease)
            {
                if (Math.Abs(priceChangePercentage) >= config.PriceIncreaseThresholdPercentage)
                {
                    alertType = AlertType.PriceIncrease;
                    severity = Math.Abs(priceChangePercentage) >= 20 ? AlertSeverity.Critical :
                               Math.Abs(priceChangePercentage) >= 10 ? AlertSeverity.High :
                               Math.Abs(priceChangePercentage) >= 5 ? AlertSeverity.Warning : AlertSeverity.Info;
                }
            }
            // Check for price decrease
            else if (priceChangePercentage < 0 && config.AlertOnPriceDecrease)
            {
                if (Math.Abs(priceChangePercentage) >= config.PriceDecreaseThresholdPercentage)
                {
                    alertType = AlertType.PriceDecrease;
                    severity = AlertSeverity.Info; // Price decrease is good news
                }
            }

            if (alertType.HasValue)
            {
                var alert = new AlertEntity
                {
                    TenantId = tenantId,
                    ProductId = productId,
                    SupplierId = supplierId,
                    AlertType = alertType.Value,
                    Severity = severity,
                    Status = AlertStatus.New,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PriceChangePercentage = priceChangePercentage,
                    Currency = "EUR",
                    AlertTitle = alertType == AlertType.PriceIncrease 
                        ? $"Price Increase: {product.Name}"
                        : $"Price Decrease: {product.Name}",
                    AlertMessage = $"The price for {product.Name} ({product.Code}) from supplier {supplier} has {(priceChangePercentage > 0 ? "increased" : "decreased")} by {Math.Abs(priceChangePercentage):F2}% (from €{oldPrice:F2} to €{newPrice:F2}).",
                    RecommendedAction = alertType == AlertType.PriceIncrease 
                        ? "Review supplier options and consider alternatives."
                        : "Consider ordering additional stock at the lower price.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                _context.SupplierPriceAlerts.Add(alert);
                alertIds.Add(alert.Id);
            }
        }

        if (alertIds.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} alerts for price change on product {ProductId}", 
                alertIds.Count, productId);
        }

        return alertIds;
    }

    public async Task<List<Guid>> GenerateAlertsForBetterSupplierAsync(
        Guid productId, 
        Guid currentSupplierId, 
        Guid betterSupplierId, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var alertIds = new List<Guid>();

        // Get product info
        var product = await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => new { p.Name, p.Code })
            .FirstOrDefaultAsync(cancellationToken);

        // Get supplier info
        var suppliers = await _context.BusinessParties
            .Where(s => s.Id == currentSupplierId || s.Id == betterSupplierId)
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        if (product == null || suppliers.Count != 2)
        {
            return alertIds;
        }

        // Get prices to calculate potential savings
        var currentPrice = await _context.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.SupplierId == currentSupplierId)
            .Select(ps => ps.UnitCost)
            .FirstOrDefaultAsync(cancellationToken);

        var betterPrice = await _context.ProductSuppliers
            .Where(ps => ps.ProductId == productId && ps.SupplierId == betterSupplierId)
            .Select(ps => ps.UnitCost)
            .FirstOrDefaultAsync(cancellationToken);

        var potentialSavings = currentPrice > betterPrice ? currentPrice - betterPrice : 0;

        // Check user configurations
        var configs = await _context.AlertConfigurations
            .Where(c => c.TenantId == tenantId && c.AlertOnBetterSupplier)
            .ToListAsync(cancellationToken);

        if (!configs.Any())
        {
            configs.Add(new AlertConfiguration { TenantId = tenantId, UserId = "default", AlertOnBetterSupplier = true });
        }

        foreach (var config in configs.Where(c => c.AlertOnBetterSupplier))
        {
            var alert = new AlertEntity
            {
                TenantId = tenantId,
                ProductId = productId,
                SupplierId = currentSupplierId,
                BetterSupplierSuggestionId = betterSupplierId,
                AlertType = AlertType.BetterSupplierAvailable,
                Severity = potentialSavings > 10 ? AlertSeverity.High : AlertSeverity.Warning,
                Status = AlertStatus.New,
                OldPrice = currentPrice,
                NewPrice = betterPrice,
                PotentialSavings = potentialSavings,
                Currency = "EUR",
                AlertTitle = $"Better Supplier Available: {product.Name}",
                AlertMessage = $"A better supplier option is available for {product.Name} ({product.Code}). " +
                              $"Consider switching from {suppliers[currentSupplierId]} to {suppliers[betterSupplierId]}. " +
                              $"Potential savings: €{potentialSavings:F2} per unit.",
                RecommendedAction = "Review the supplier suggestion details and consider switching suppliers.",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };

            _context.SupplierPriceAlerts.Add(alert);
            alertIds.Add(alert.Id);
        }

        if (alertIds.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} alerts for better supplier on product {ProductId}", 
                alertIds.Count, productId);
        }

        return alertIds;
    }

    public async Task<SupplierPriceAlertDto?> GetAlertByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");

        var alert = await _context.SupplierPriceAlerts
            .Include(a => a.Product)
            .Include(a => a.Supplier)
            .Where(a => a.Id == alertId && a.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        return alert == null ? null : await MapToDto(alert, cancellationToken);
    }

    public async Task<PaginatedResult<SupplierPriceAlertDto>> GetAlertsAsync(
        AlertFilterRequest filter, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");

        var query = _context.SupplierPriceAlerts
            .Include(a => a.Product)
            .Include(a => a.Supplier)
            .Where(a => a.TenantId == tenantId);

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
        {
            if (Enum.TryParse<AlertStatus>(filter.Status, true, out var status))
            {
                query = query.Where(a => a.Status == status);
            }
        }

        if (!string.IsNullOrEmpty(filter.Severity))
        {
            if (Enum.TryParse<AlertSeverity>(filter.Severity, true, out var severity))
            {
                query = query.Where(a => a.Severity == severity);
            }
        }

        if (!string.IsNullOrEmpty(filter.AlertType))
        {
            if (Enum.TryParse<AlertType>(filter.AlertType, true, out var alertType))
            {
                query = query.Where(a => a.AlertType == alertType);
            }
        }

        if (filter.ProductId.HasValue)
        {
            query = query.Where(a => a.ProductId == filter.ProductId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(a => a.SupplierId == filter.SupplierId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = (filter.SortBy?.ToLower()) switch
        {
            "severity" => filter.SortOrder?.ToLower() == "asc" 
                ? query.OrderBy(a => a.Severity) 
                : query.OrderByDescending(a => a.Severity),
            "status" => filter.SortOrder?.ToLower() == "asc" 
                ? query.OrderBy(a => a.Status) 
                : query.OrderByDescending(a => a.Status),
            "createdat" => filter.SortOrder?.ToLower() == "asc" 
                ? query.OrderBy(a => a.CreatedAt) 
                : query.OrderByDescending(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt) // Default sort
        };

        // Apply pagination
        var alerts = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<SupplierPriceAlertDto>();
        foreach (var alert in alerts)
        {
            var dto = await MapToDto(alert, cancellationToken);
            if (dto != null)
            {
                dtos.Add(dto);
            }
        }

        return new PaginatedResult<SupplierPriceAlertDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AlertStatistics> GetAlertStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");

        var alerts = await _context.SupplierPriceAlerts
            .Where(a => a.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return new AlertStatistics
        {
            TotalAlerts = alerts.Count,
            NewAlerts = alerts.Count(a => a.Status == AlertStatus.New),
            AcknowledgedAlerts = alerts.Count(a => a.Status == AlertStatus.Acknowledged),
            ResolvedAlerts = alerts.Count(a => a.Status == AlertStatus.Resolved),
            DismissedAlerts = alerts.Count(a => a.Status == AlertStatus.Dismissed),
            
            CriticalAlerts = alerts.Count(a => a.Severity == AlertSeverity.Critical && a.Status == AlertStatus.New),
            HighPriorityAlerts = alerts.Count(a => a.Severity == AlertSeverity.High && a.Status == AlertStatus.New),
            WarningAlerts = alerts.Count(a => a.Severity == AlertSeverity.Warning && a.Status == AlertStatus.New),
            InfoAlerts = alerts.Count(a => a.Severity == AlertSeverity.Info && a.Status == AlertStatus.New),
            
            TotalPotentialSavings = alerts.Where(a => a.PotentialSavings.HasValue).Sum(a => a.PotentialSavings ?? 0),
            Currency = "EUR",
            
            PriceIncreaseAlerts = alerts.Count(a => a.AlertType == AlertType.PriceIncrease && a.Status == AlertStatus.New),
            PriceDecreaseAlerts = alerts.Count(a => a.AlertType == AlertType.PriceDecrease && a.Status == AlertStatus.New),
            BetterSupplierAlerts = alerts.Count(a => a.AlertType == AlertType.BetterSupplierAvailable && a.Status == AlertStatus.New),
            VolatilityAlerts = alerts.Count(a => a.AlertType == AlertType.PriceVolatility && a.Status == AlertStatus.New),
            
            LastAlertDate = alerts.Any() ? alerts.Max(a => a.CreatedAt) : null,
            OldestUnreadAlertDate = alerts.Any(a => a.Status == AlertStatus.New) 
                ? alerts.Where(a => a.Status == AlertStatus.New).Min(a => a.CreatedAt) 
                : null
        };
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var userId = GetCurrentUserId();

        var alert = await _context.SupplierPriceAlerts
            .Where(a => a.Id == alertId && a.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (alert == null)
        {
            return false;
        }

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedByUserId = userId;
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResolveAlertAsync(Guid alertId, string? notes, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var userId = GetCurrentUserId();

        var alert = await _context.SupplierPriceAlerts
            .Where(a => a.Id == alertId && a.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (alert == null)
        {
            return false;
        }

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.ResolvedByUserId = userId;
        alert.ResolutionNotes = notes;
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DismissAlertAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var userId = GetCurrentUserId();

        var alert = await _context.SupplierPriceAlerts
            .Where(a => a.Id == alertId && a.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (alert == null)
        {
            return false;
        }

        alert.Status = AlertStatus.Dismissed;
        alert.ModifiedAt = DateTime.UtcNow;
        alert.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DismissMultipleAlertsAsync(List<Guid> alertIds, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var userId = GetCurrentUserId();

        var alerts = await _context.SupplierPriceAlerts
            .Where(a => alertIds.Contains(a.Id) && a.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            alert.Status = AlertStatus.Dismissed;
            alert.ModifiedAt = DateTime.UtcNow;
            alert.ModifiedBy = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return alerts.Count;
    }

    public async Task<int> GetUnreadAlertCountAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");

        return await _context.SupplierPriceAlerts
            .Where(a => a.TenantId == tenantId && a.Status == AlertStatus.New)
            .CountAsync(cancellationToken);
    }

    public async Task<AlertConfiguration> GetUserConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context not available");
        var userId = GetCurrentUserId();

        var config = await _context.AlertConfigurations
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            // Create default configuration
            config = new AlertConfiguration
            {
                TenantId = tenantId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.AlertConfigurations.Add(config);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return config;
    }

    public async Task<AlertConfiguration> UpdateUserConfigurationAsync(
        UpdateAlertConfigRequest request, 
        CancellationToken cancellationToken = default)
    {
        var config = await GetUserConfigurationAsync(cancellationToken);
        var userId = GetCurrentUserId();

        config.PriceIncreaseThresholdPercentage = request.PriceIncreaseThresholdPercentage;
        config.PriceDecreaseThresholdPercentage = request.PriceDecreaseThresholdPercentage;
        config.VolatilityThresholdPercentage = request.VolatilityThresholdPercentage;
        config.DaysWithoutUpdateThreshold = request.DaysWithoutUpdateThreshold;
        config.EnableEmailNotifications = request.EnableEmailNotifications;
        config.EnableBrowserNotifications = request.EnableBrowserNotifications;
        config.AlertOnPriceIncrease = request.AlertOnPriceIncrease;
        config.AlertOnPriceDecrease = request.AlertOnPriceDecrease;
        config.AlertOnBetterSupplier = request.AlertOnBetterSupplier;
        config.AlertOnVolatility = request.AlertOnVolatility;

        if (Enum.TryParse<AlertFrequency>(request.NotificationFrequency, true, out var frequency))
        {
            config.NotificationFrequency = frequency;
        }

        config.ModifiedAt = DateTime.UtcNow;
        config.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    private async Task<SupplierPriceAlertDto?> MapToDto(AlertEntity alert, CancellationToken cancellationToken)
    {
        return new SupplierPriceAlertDto
        {
            Id = alert.Id,
            TenantId = alert.TenantId,
            ProductId = alert.ProductId,
            ProductName = alert.Product?.Name,
            ProductCode = alert.Product?.Code,
            SupplierId = alert.SupplierId,
            SupplierName = alert.Supplier?.Name,
            AlertType = alert.AlertType.ToString(),
            Severity = alert.Severity.ToString(),
            Status = alert.Status.ToString(),
            OldPrice = alert.OldPrice,
            NewPrice = alert.NewPrice,
            PriceChangePercentage = alert.PriceChangePercentage,
            Currency = alert.Currency,
            PotentialSavings = alert.PotentialSavings,
            AlertTitle = alert.AlertTitle,
            AlertMessage = alert.AlertMessage,
            RecommendedAction = alert.RecommendedAction,
            BetterSupplierSuggestionId = alert.BetterSupplierSuggestionId,
            CreatedAt = alert.CreatedAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedByUserId = alert.AcknowledgedByUserId,
            ResolvedAt = alert.ResolvedAt,
            ResolvedByUserId = alert.ResolvedByUserId,
            ResolutionNotes = alert.ResolutionNotes,
            EmailSent = alert.EmailSent,
            EmailSentAt = alert.EmailSentAt
        };
    }
}
