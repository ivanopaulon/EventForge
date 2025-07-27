using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface ILogsService
    {
        // Application Logs - using simple types for now
        Task<object> GetApplicationLogsAsync(Dictionary<string, object> queryParams);
        Task<object?> GetApplicationLogAsync(Guid id);
        Task<object> GetApplicationLogStatisticsAsync();
        Task<Stream> ExportApplicationLogsAsync(object exportDto);

        // Audit Logs  
        Task<object> GetAuditLogsAsync(Dictionary<string, object> queryParams);
        Task<object?> GetAuditLogAsync(Guid id);
        Task<object> GetAuditLogStatisticsAsync();
        Task<Stream> ExportAuditLogsAsync(object exportDto);

        // Real-time subscriptions
        Task SubscribeToApplicationLogsAsync(Func<object, Task> onLogReceived);
        Task SubscribeToAuditLogsAsync(Func<object, Task> onLogReceived);
        Task UnsubscribeFromLogsAsync();
    }

    public class LogsService : ILogsService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly SignalRService _signalRService;
        private readonly ILogger<LogsService> _logger;

        public LogsService(
            HttpClient httpClient, 
            IAuthService authService, 
            SignalRService signalRService,
            ILogger<LogsService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _signalRService = signalRService;
            _logger = logger;
        }

        private async Task EnsureAuthenticatedAsync()
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("User not authenticated");

            if (!_httpClient.DefaultRequestHeaders.Authorization?.Parameter?.Equals(token) == true)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        #region Application Logs

        public async Task<object> GetApplicationLogsAsync(Dictionary<string, object> queryParams)
        {
            await EnsureAuthenticatedAsync();
            
            var queryString = BuildQueryString(queryParams);
            var response = await _httpClient.GetAsync($"api/v1/ApplicationLog?{queryString}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<object>() ?? new object();
        }

        public async Task<object?> GetApplicationLogAsync(Guid id)
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.GetAsync($"api/v1/ApplicationLog/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        public async Task<object> GetApplicationLogStatisticsAsync()
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.GetAsync("api/v1/ApplicationLog/statistics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>() ?? new object();
        }

        public async Task<Stream> ExportApplicationLogsAsync(object exportDto)
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.PostAsJsonAsync("api/v1/ApplicationLog/export", exportDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        #endregion

        #region Audit Logs

        public async Task<object> GetAuditLogsAsync(Dictionary<string, object> queryParams)
        {
            await EnsureAuthenticatedAsync();
            
            var queryString = BuildQueryString(queryParams);
            var response = await _httpClient.GetAsync($"api/v1/AuditLog?{queryString}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<object>() ?? new object();
        }

        public async Task<object?> GetAuditLogAsync(Guid id)
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.GetAsync($"api/v1/AuditLog/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        public async Task<object> GetAuditLogStatisticsAsync()
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.GetAsync("api/v1/AuditLog/statistics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>() ?? new object();
        }

        public async Task<Stream> ExportAuditLogsAsync(object exportDto)
        {
            await EnsureAuthenticatedAsync();
            var response = await _httpClient.PostAsJsonAsync("api/v1/AuditLog/export", exportDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        #endregion

        #region Real-time Subscriptions

        public async Task SubscribeToApplicationLogsAsync(Func<object, Task> onLogReceived)
        {
            try
            {
                await _signalRService.StartConnectionAsync();
                // TODO: Implement proper SignalR subscription when available
                _logger.LogInformation("Application log subscription requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to application logs");
                throw;
            }
        }

        public async Task SubscribeToAuditLogsAsync(Func<object, Task> onLogReceived)
        {
            try
            {
                await _signalRService.StartConnectionAsync();
                // TODO: Implement proper SignalR subscription when available
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
                // TODO: Implement proper SignalR unsubscription when available
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