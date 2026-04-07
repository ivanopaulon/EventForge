using EventForge.DTOs.Alerts;
using AlertConfiguration = EventForge.Server.Data.Entities.Alerts.AlertConfiguration;

namespace EventForge.Server.Services.Alerts;

/// <summary>
/// Service for managing supplier price alerts.
/// Part of FASE 5: Price Alerts System.
/// </summary>
public interface ISupplierPriceAlertService
{
    /// <summary>
    /// Creates a new alert.
    /// </summary>
    Task<Guid> CreateAlertAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates alerts for a price change if thresholds are exceeded.
    /// </summary>
    Task<List<Guid>> GenerateAlertsForPriceChangeAsync(Guid productId, Guid supplierId, decimal oldPrice, decimal newPrice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates alerts when a better supplier is available.
    /// </summary>
    Task<List<Guid>> GenerateAlertsForBetterSupplierAsync(Guid productId, Guid currentSupplierId, Guid betterSupplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an alert by ID.
    /// </summary>
    Task<SupplierPriceAlertDto?> GetAlertByIdAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts with filtering and pagination.
    /// </summary>
    Task<PaginatedResult<SupplierPriceAlertDto>> GetAlertsAsync(AlertFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert statistics for the current user.
    /// </summary>
    Task<AlertStatistics> GetAlertStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert with notes.
    /// </summary>
    Task<bool> ResolveAlertAsync(Guid alertId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses an alert.
    /// </summary>
    Task<bool> DismissAlertAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses multiple alerts.
    /// </summary>
    Task<int> DismissMultipleAlertsAsync(List<Guid> alertIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of unread alerts for the current user.
    /// </summary>
    Task<int> GetUnreadAlertCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the alert configuration for the current user.
    /// </summary>
    Task<AlertConfiguration> GetUserConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the alert configuration for the current user.
    /// </summary>
    Task<AlertConfiguration> UpdateUserConfigurationAsync(UpdateAlertConfigRequest request, CancellationToken cancellationToken = default);
}
