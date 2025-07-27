using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface ILogsService
    {
        // Application Logs
        Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams);
        Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id);
        Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync();
        Task<Stream> ExportApplicationLogsAsync(object exportDto);

        // Audit Logs  
        Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams);
        Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id);
        Task<AuditLogStatisticsDto> GetAuditLogStatisticsAsync();
        Task<Stream> ExportAuditLogsAsync(object exportDto);

        // Real-time subscriptions
        Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived);
        Task SubscribeToAuditLogsAsync(Func<EntityChangeLogDto, Task> onLogReceived);
        Task UnsubscribeFromLogsAsync();
    }

    public class LogsService : ILogsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthService _authService;
        private readonly SignalRService _signalRService;
        private readonly ILogger<LogsService> _logger;

        public LogsService(
            IHttpClientFactory httpClientFactory,
            IAuthService authService,
            SignalRService signalRService,
            ILogger<LogsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _signalRService = signalRService;
            _logger = logger;
        }

        private async Task<HttpClient> CreateAuthenticatedHttpClientAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            _logger.LogDebug("LogsService: Using HttpClient with BaseAddress: {BaseAddress}", httpClient.BaseAddress);
            
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
            return httpClient;
        }

        #region Application Logs

        public async Task<PagedResult<ApplicationLogDto>> GetApplicationLogsAsync(Dictionary<string, object> queryParams)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();

            var queryString = BuildQueryString(queryParams);
            var response = await httpClient.GetAsync($"api/v1/ApplicationLog?{queryString}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PagedResult<ApplicationLogDto>>() ??
                   new PagedResult<ApplicationLogDto>();
        }

        public async Task<ApplicationLogDto?> GetApplicationLogAsync(Guid id)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.GetAsync($"api/v1/ApplicationLog/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApplicationLogDto>();
        }

        public async Task<ApplicationLogStatisticsDto> GetApplicationLogStatisticsAsync()
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.GetAsync("api/v1/ApplicationLog/statistics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApplicationLogStatisticsDto>() ??
                   new ApplicationLogStatisticsDto();
        }

        public async Task<Stream> ExportApplicationLogsAsync(object exportDto)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/v1/ApplicationLog/export", exportDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        #endregion

        #region Audit Logs

        public async Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(Dictionary<string, object> queryParams)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();

            var queryString = BuildQueryString(queryParams);
            var response = await httpClient.GetAsync($"api/v1/AuditLog?{queryString}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PagedResult<EntityChangeLogDto>>() ??
                   new PagedResult<EntityChangeLogDto>();
        }

        public async Task<EntityChangeLogDto?> GetAuditLogAsync(Guid id)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.GetAsync($"api/v1/AuditLog/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EntityChangeLogDto>();
        }

        public async Task<AuditLogStatisticsDto> GetAuditLogStatisticsAsync()
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.GetAsync("api/v1/AuditLog/statistics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuditLogStatisticsDto>() ??
                   new AuditLogStatisticsDto();
        }

        public async Task<Stream> ExportAuditLogsAsync(object exportDto)
        {
            var httpClient = await CreateAuthenticatedHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/v1/AuditLog/export", exportDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        #endregion

        #region Real-time Subscriptions

        public async Task SubscribeToApplicationLogsAsync(Func<ApplicationLogDto, Task> onLogReceived)
        {
            try
            {
                await _signalRService.StartConnectionAsync();
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
                await _signalRService.StartConnectionAsync();
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