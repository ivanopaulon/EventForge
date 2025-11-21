using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Client.Services
{
    public interface ILogsService
    {
        // Application Logs
        Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams);
        Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id);
        Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync();
        Task<Stream> ExportApplicationLogsAsync(ExportRequestDto exportDto);

        // Audit Logs  
        Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams);
        Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id);
        Task<AuditTrailStatisticsDto> GetAuditLogStatisticsAsync();
        Task<Stream> ExportAuditLogsAsync(AuditLogExportDto exportDto);

        // Real-time subscriptions
        Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived);
        Task SubscribeToAuditLogsAsync(Func<EntityChangeLogDto, Task> onLogReceived);
        Task UnsubscribeFromLogsAsync();
    }

    public class LogsService : ILogsService
    {
        private const string BaseUrl = "api/v1/logs";
        private readonly IHttpClientService _httpClientService;
        private readonly IRealtimeService _realtimeService;
        private readonly ILogger<LogsService> _logger;

        public LogsService(
            IHttpClientService httpClientService,
            IRealtimeService realtimeService,
            ILogger<LogsService> logger)
        {
            _httpClientService = httpClientService;
            _realtimeService = realtimeService;
            _logger = logger;
        }

        #region Application Logs

        public async Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams)
        {
            var queryString = BuildQueryString(queryParams);
            return await _httpClientService.GetAsync<PagedResult<ApplicationLogDto>>($"api/v1/application-logs?{queryString}") ??
                   new PagedResult<ApplicationLogDto>();
        }

        public async Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id)
        {
            return await _httpClientService.GetAsync<ApplicationLogDto>($"api/v1/application-logs/{id}");
        }

        public async Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync()
        {
            return await _httpClientService.GetAsync<ApplicationLogStatisticsDto>("api/v1/application-logs/statistics") ??
                   new ApplicationLogStatisticsDto();
        }

        public async Task<Stream> ExportApplicationLogsAsync(ExportRequestDto exportDto)
        {
            return await _httpClientService.PostStreamAsync("api/v1/application-logs/export", exportDto);
        }

        #endregion

        #region Audit Logs

        public async Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams)
        {
            var queryString = BuildQueryString(queryParams);
            return await _httpClientService.GetAsync<PagedResult<EntityChangeLogDto>>($"api/v1/audit-logs?{queryString}") ??
                   new PagedResult<EntityChangeLogDto>();
        }

        public async Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id)
        {
            return await _httpClientService.GetAsync<EntityChangeLogDto>($"api/v1/audit-logs/{id}");
        }

        public async Task<AuditTrailStatisticsDto> GetAuditLogStatisticsAsync()
        {
            return await _httpClientService.GetAsync<AuditTrailStatisticsDto>("api/v1/audit-logs/statistics") ??
                   new AuditTrailStatisticsDto();
        }

        public async Task<Stream> ExportAuditLogsAsync(AuditLogExportDto exportDto)
        {
            return await _httpClientService.PostStreamAsync("api/v1/audit-logs/export", exportDto);
        }

        #endregion

        #region Real-time Subscriptions

        public async Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived)
        {
            try
            {
                await _realtimeService.StartAuditConnectionAsync();
                // SignalR subscription will be implemented in future version
                _logger.LogInformation("Application log subscription requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to application logs");
                throw;
            }
        }

        public async Task SubscribeToAuditLogsAsync(Func<EntityChangeLogDto, Task> onLogReceived)
        {
            try
            {
                await _realtimeService.StartAuditConnectionAsync();
                // SignalR subscription will be implemented in future version
                _logger.LogInformation("Audit log subscription requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to audit logs");
                throw;
            }
        }

        public async Task UnsubscribeFromLogsAsync()
        {
            try
            {
                // SignalR unsubscription will be implemented in future version
                _logger.LogInformation("Log unsubscription requested");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from logs");
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
                if (value != null)
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