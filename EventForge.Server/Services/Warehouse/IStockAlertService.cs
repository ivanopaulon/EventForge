using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing stock alerts and notifications.
/// </summary>
public interface IStockAlertService
{
    /// <summary>
    /// Gets all stock alerts with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<StockAlertDto>> GetAlertsAsync(
        int page = 1,
        int pageSize = 20,
        string? alertType = null,
        string? severity = null,
        string? status = null,
        Guid? productId = null,
        Guid? locationId = null,
        bool? acknowledged = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stock alert by ID.
    /// </summary>
    Task<StockAlertDto?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active alerts for a specific product.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> GetActiveAlertsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active alerts for a specific location.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> GetActiveAlertsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a stock alert.
    /// </summary>
    Task<StockAlertDto> CreateAlertAsync(CreateStockAlertDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert.
    /// </summary>
    Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string? resolutionNotes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses an alert.
    /// </summary>
    Task<bool> DismissAlertAsync(Guid alertId, string dismissedBy, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks and creates low stock alerts.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> CheckLowStockAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks and creates overstock alerts.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> CheckOverstockAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks and creates expiry alerts for lots.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> CheckExpiryAlertsAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all automatic alert checks.
    /// </summary>
    Task<AlertCheckSummaryDto> RunAlertChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert statistics.
    /// </summary>
    Task<AlertStatisticsDto> GetAlertStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends alert notifications via email.
    /// </summary>
    Task<bool> SendAlertNotificationsAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts that need notifications.
    /// </summary>
    Task<IEnumerable<StockAlertDto>> GetAlertsForNotificationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk acknowledge multiple alerts.
    /// </summary>
    Task<int> BulkAcknowledgeAlertsAsync(IEnumerable<Guid> alertIds, string acknowledgedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes resolved alerts older than specified days.
    /// </summary>
    Task<int> CleanupOldAlertsAsync(int olderThanDays = 90, CancellationToken cancellationToken = default);
}