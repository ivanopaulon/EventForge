using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for StockAlert entities and DTOs.
/// </summary>
public static class StockAlertMapper
{
    /// <summary>
    /// Maps a StockAlert entity to a StockAlertDto.
    /// </summary>
    public static StockAlertDto ToStockAlertDto(this StockAlert alert)
    {
        return new StockAlertDto
        {
            Id = alert.Id,
            TenantId = alert.TenantId,
            StockId = alert.StockId,
            ProductName = alert.Stock?.Product?.Name,
            ProductCode = alert.Stock?.Product?.Code,
            LocationCode = alert.Stock?.StorageLocation?.Code,
            WarehouseName = alert.Stock?.StorageLocation?.Warehouse?.Name,
            LotCode = alert.Stock?.Lot?.Code,
            AlertType = alert.AlertType.ToString(),
            Severity = alert.Severity.ToString(),
            CurrentLevel = alert.CurrentLevel,
            Threshold = alert.Threshold,
            Message = alert.Message,
            Status = alert.Status.ToString(),
            TriggeredDate = alert.TriggeredDate,
            AcknowledgedDate = alert.AcknowledgedDate,
            AcknowledgedBy = alert.AcknowledgedBy,
            ResolvedDate = alert.ResolvedDate,
            ResolvedBy = alert.ResolvedBy,
            ResolutionNotes = alert.ResolutionNotes,
            SendEmailNotifications = alert.SendEmailNotifications,
            NotificationEmails = alert.NotificationEmails,
            LastNotificationDate = alert.LastNotificationDate,
            NotificationCount = alert.NotificationCount,
            CreatedAt = alert.CreatedAt,
            CreatedBy = alert.CreatedBy
        };
    }
}
