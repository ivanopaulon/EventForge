using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Client.Services
{
    public interface ILogsService
    {
        // Application Logs
        Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams, CancellationToken ct = default);
        Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id, CancellationToken ct = default);
        Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync(CancellationToken ct = default);
        Task<Stream> ExportApplicationLogsAsync(ExportRequestDto exportDto, CancellationToken ct = default);

        // Audit Logs  
        Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams, CancellationToken ct = default);
        Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id, CancellationToken ct = default);
        Task<EventForge.DTOs.Audit.AuditTrailStatisticsDto> GetAuditLogStatisticsAsync(CancellationToken ct = default);
        Task<Stream> ExportAuditLogsAsync(AuditLogExportDto exportDto, CancellationToken ct = default);

        // Real-time subscriptions
        Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived, CancellationToken ct = default);
        Task SubscribeToAuditLogsAsync(Func<EntityChangeLogDto, Task> onLogReceived, CancellationToken ct = default);
        Task UnsubscribeFromLogsAsync(CancellationToken ct = default);
    }

    public class LogsService(
        IHttpClientService httpClientService,
        IRealtimeService realtimeService,
        ILogger<LogsService> logger) : ILogsService
    {

        #region Application Logs

        public async Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams, CancellationToken ct = default)
        {
            try
            {
                var queryString = BuildQueryString(queryParams);
                return await httpClientService.GetAsync<PagedResult<ApplicationLogDto>>($"api/v1/application-logs?{queryString}") ??
                       new PagedResult<ApplicationLogDto>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving application logs");
                throw;
            }
        }

        public async Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<ApplicationLogDto>($"api/v1/application-logs/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving application log {Id}", id);
                throw;
            }
        }

        public async Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<ApplicationLogStatisticsDto>("api/v1/application-logs/statistics") ??
                       new ApplicationLogStatisticsDto();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving application log statistics");
                throw;
            }
        }

        public async Task<Stream> ExportApplicationLogsAsync(ExportRequestDto exportDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostStreamAsync("api/v1/application-logs/export", exportDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting application logs");
                throw;
            }
        }

        #endregion

        #region Audit Logs

        public async Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams, CancellationToken ct = default)
        {
            try
            {
                var queryString = BuildQueryString(queryParams);
                return await httpClientService.GetAsync<PagedResult<EntityChangeLogDto>>($"api/v1/audit-logs?{queryString}") ??
                       new PagedResult<EntityChangeLogDto>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving audit logs");
                throw;
            }
        }

        public async Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<EntityChangeLogDto>($"api/v1/audit-logs/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving audit log {Id}", id);
                throw;
            }
        }

        public async Task<EventForge.DTOs.Audit.AuditTrailStatisticsDto> GetAuditLogStatisticsAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<EventForge.DTOs.Audit.AuditTrailStatisticsDto>("api/v1/audit-logs/statistics") ??
                       new EventForge.DTOs.Audit.AuditTrailStatisticsDto();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving audit log statistics");
                throw;
            }
        }

        public async Task<Stream> ExportAuditLogsAsync(AuditLogExportDto exportDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostStreamAsync("api/v1/audit-logs/export", exportDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting audit logs");
                throw;
            }
        }

        #endregion

        #region Real-time Subscriptions

        public async Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived, CancellationToken ct = default)
        {
            try
            {
                await realtimeService.StartAuditConnectionAsync();
                // SignalR subscription will be implemented in future version
                logger.LogInformation("Application log subscription requested");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe to application logs");
                throw;
            }
        }

        public async Task SubscribeToAuditLogsAsync(Func<EntityChangeLogDto, Task> onLogReceived, CancellationToken ct = default)
        {
            try
            {
                await realtimeService.StartAuditConnectionAsync();
                // SignalR subscription will be implemented in future version
                logger.LogInformation("Audit log subscription requested");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe to audit logs");
                throw;
            }
        }

        public async Task UnsubscribeFromLogsAsync(CancellationToken ct = default)
        {
            try
            {
                // SignalR unsubscription will be implemented in future version
                logger.LogInformation("Log unsubscription requested");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unsubscribe from logs");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private static string BuildQueryString(Dictionary<string, object> queryParams)
        {
            var queryItems = new List<string>();

            foreach (var kvp in queryParams)
            {
                var value = kvp.Value;
                if (value is not null)
                {
                    if (value is DateTime dateTime && dateTime != default)
                    {
                        queryItems.Add($"{kvp.Key}={dateTime:yyyy-MM-ddTHH:mm:ss}");
                    }
                    else if (value is bool boolValue)
                    {
                        queryItems.Add($"{kvp.Key}={boolValue.ToString().ToLower()}");
                    }
                    else if (value is int intValue && intValue > 0)
                    {
                        queryItems.Add($"{kvp.Key}={intValue}");
                    }
                    else if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                    {
                        queryItems.Add($"{kvp.Key}={Uri.EscapeDataString(stringValue)}");
                    }
                    else if (value is Guid guidValue && guidValue != Guid.Empty)
                    {
                        queryItems.Add($"{kvp.Key}={guidValue}");
                    }
                }
            }

            return string.Join("&", queryItems);
        }

        #endregion
    }
}