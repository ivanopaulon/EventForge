using EventForge.DTOs.Common;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Interface for client-side logging service that sends logs to the server.
    /// </summary>
    public interface IClientLogService
    {
        // Standard logging methods
        Task LogDebugAsync(string message, string? category = null, Dictionary<string, object>? properties = null);
        Task LogInformationAsync(string message, string? category = null, Dictionary<string, object>? properties = null);
        Task LogWarningAsync(string message, string? category = null, Dictionary<string, object>? properties = null);
        Task LogErrorAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null);
        Task LogCriticalAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null);

        // Advanced logging methods
        Task LogAsync(ClientLogDto clientLog);
        Task LogBatchAsync(List<ClientLogDto> logs);

        // Buffer management
        Task FlushAsync();
        Task<List<ClientLogDto>> GetLocalLogsAsync();
        Task ClearLocalLogsAsync();

        // Configuration
        void SetBatchSize(int batchSize);
        void SetFlushInterval(TimeSpan interval);
        void EnableOfflineMode(bool enabled);
    }

    /// <summary>
    /// Client-side logging service that sends logs to the server with offline support.
    /// </summary>
    public class ClientLogService : IClientLogService, IDisposable
    {
        private const string BaseUrl = "api/v1/logs/client";
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly IAuthService _authService;
        private readonly ILogger<ClientLogService> _logger;

        private readonly List<ClientLogDto> _logBuffer = new();
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);

        private int _batchSize = 10;
        private TimeSpan _flushInterval = TimeSpan.FromMinutes(1);
        private bool _offlineMode = true;
        private bool _disposed = false;

        private const string LOCAL_STORAGE_KEY = "EventForge_ClientLogs";
        private const int MAX_LOCAL_LOGS = 1000;

        public ClientLogService(
            IHttpClientFactory httpClientFactory,
            IJSRuntime jsRuntime,
            IAuthService authService,
            ILogger<ClientLogService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _jsRuntime = jsRuntime;
            _authService = authService;
            _logger = logger;

            // Initialize flush timer
            _flushTimer = new Timer(OnFlushTimer, null, _flushInterval, _flushInterval);

            // Load any pending logs from localStorage on startup
            _ = Task.Run(LoadPendingLogsAsync);
        }

        #region Public Logging Methods

        public async Task LogDebugAsync(string message, string? category = null, Dictionary<string, object>? properties = null)
        {
            await LogAsync(CreateClientLog("Debug", message, category, properties));
        }

        public async Task LogInformationAsync(string message, string? category = null, Dictionary<string, object>? properties = null)
        {
            await LogAsync(CreateClientLog("Information", message, category, properties));
        }

        public async Task LogWarningAsync(string message, string? category = null, Dictionary<string, object>? properties = null)
        {
            await LogAsync(CreateClientLog("Warning", message, category, properties));
        }

        public async Task LogErrorAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null)
        {
            await LogAsync(CreateClientLog("Error", message, category, properties, exception));
        }

        public async Task LogCriticalAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null)
        {
            await LogAsync(CreateClientLog("Critical", message, category, properties, exception));
        }

        public async Task LogAsync(ClientLogDto clientLog)
        {
            if (clientLog == null) return;

            try
            {
                // Enrich the log with current context
                await EnrichLogAsync(clientLog);

                // Add to buffer
                lock (_logBuffer)
                {
                    _logBuffer.Add(clientLog);
                }

                // Save to localStorage for offline support
                if (_offlineMode)
                {
                    await SaveToLocalStorageAsync(clientLog);
                }

                // Try immediate send for critical errors, otherwise batch
                if (clientLog.Level == "Critical" || clientLog.Level == "Error")
                {
                    await TrySendImmediatelyAsync(clientLog);
                }
                else if (_logBuffer.Count >= _batchSize)
                {
                    await FlushAsync();
                }
            }
            catch (Exception ex)
            {
                // Fallback to local console logging if service fails
                _logger?.LogError(ex, "Failed to process client log: {Message}", clientLog.Message);
            }
        }

        public async Task LogBatchAsync(List<ClientLogDto> logs)
        {
            if (logs == null || logs.Count == 0) return;

            try
            {
                // Enrich all logs
                foreach (var log in logs)
                {
                    await EnrichLogAsync(log);
                }

                // Send batch to server
                await SendBatchToServerAsync(logs);

                // Remove from local storage if successful
                if (_offlineMode)
                {
                    await RemoveFromLocalStorageAsync(logs);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send log batch with {Count} entries", logs.Count);

                // Save to localStorage for retry
                if (_offlineMode)
                {
                    foreach (var log in logs)
                    {
                        await SaveToLocalStorageAsync(log);
                    }
                }
            }
        }

        #endregion

        #region Buffer Management

        public async Task FlushAsync()
        {
            await _flushSemaphore.WaitAsync();
            try
            {
                List<ClientLogDto> logsToSend;
                lock (_logBuffer)
                {
                    if (_logBuffer.Count == 0) return;
                    logsToSend = new List<ClientLogDto>(_logBuffer);
                    _logBuffer.Clear();
                }

                await LogBatchAsync(logsToSend);
            }
            finally
            {
                _ = _flushSemaphore.Release();
            }
        }

        public async Task<List<ClientLogDto>> GetLocalLogsAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LOCAL_STORAGE_KEY);
                if (string.IsNullOrEmpty(json))
                    return new List<ClientLogDto>();

                return JsonSerializer.Deserialize<List<ClientLogDto>>(json) ?? new List<ClientLogDto>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to retrieve local logs");
                return new List<ClientLogDto>();
            }
        }

        public async Task ClearLocalLogsAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LOCAL_STORAGE_KEY);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear local logs");
            }
        }

        #endregion

        #region Configuration

        public void SetBatchSize(int batchSize)
        {
            _batchSize = Math.Max(1, Math.Min(batchSize, ClientLogBatchDto.MaxBatchSize));
        }

        public void SetFlushInterval(TimeSpan interval)
        {
            _flushInterval = interval;
        }

        public void EnableOfflineMode(bool enabled)
        {
            _offlineMode = enabled;
        }

        #endregion

        #region Private Methods

        private ClientLogDto CreateClientLog(string level, string message, string? category, Dictionary<string, object>? properties, Exception? exception = null)
        {
            return new ClientLogDto
            {
                Level = level,
                Message = message,
                Category = category,
                Exception = exception?.ToString(),
                Properties = properties != null ? JsonSerializer.Serialize(properties) : null,
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }

        private async Task EnrichLogAsync(ClientLogDto clientLog)
        {
            try
            {
                // Get current page URL
                var currentUrl = await _jsRuntime.InvokeAsync<string>("eval", "window.location.pathname + window.location.search");
                clientLog.Page = currentUrl;

                // Get user agent
                var userAgent = await _jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                clientLog.UserAgent = userAgent;

                // Get user ID if authenticated
                try
                {
                    var user = await _authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        clientLog.UserId = user.Id;
                    }
                }
                catch
                {
                    // Ignore authentication errors during logging
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to enrich client log");
            }
        }

        private async Task TrySendImmediatelyAsync(ClientLogDto clientLog)
        {
            try
            {
                await SendSingleLogToServerAsync(clientLog);

                // Remove from localStorage if successful
                if (_offlineMode)
                {
                    await RemoveFromLocalStorageAsync(new List<ClientLogDto> { clientLog });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send critical log immediately, will retry in batch");
            }
        }

        private async Task<HttpClient> GetAuthenticatedHttpClientAsync()
        {
            var httpClient = _httpClient;

            // Add authentication header if available
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                if (httpClient.DefaultRequestHeaders.Authorization?.Parameter != token)
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }

            return httpClient;
        }

        private async Task SendSingleLogToServerAsync(ClientLogDto clientLog)
        {
            try
            {
                var httpClient = await GetAuthenticatedHttpClientAsync();

                // Log the request details for debugging
                _logger?.LogTrace("Sending client log to api/ClientLogs");

                var response = await httpClient.PostAsJsonAsync("api/ClientLogs", clientLog);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning("Client log failed: {StatusCode} - {Content}", 
                        response.StatusCode, content);
                }

                _ = response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error sending client log to server");
                throw new Exception($"Failed to send log to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending client log to server");
                throw new Exception($"Failed to send log to server: {ex.Message}", ex);
            }
        }

        private async Task SendBatchToServerAsync(List<ClientLogDto> logs)
        {
            if (logs.Count == 0) return;

            try
            {
                var httpClient = await GetAuthenticatedHttpClientAsync();
                var batchRequest = new ClientLogBatchDto { Logs = logs };

                // Log the request details for debugging
                _logger?.LogTrace("Sending batch of {Count} client logs to server", logs.Count);

                var response = await httpClient.PostAsJsonAsync("api/ClientLogs/batch", batchRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning("Client log batch failed: {StatusCode} - {Content}", 
                        response.StatusCode, content);
                }

                _ = response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error sending client log batch ({Count} logs)", logs.Count);
                throw new Exception($"Failed to send log batch to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending client log batch ({Count} logs)", logs.Count);
                throw new Exception($"Failed to send log batch to server: {ex.Message}", ex);
            }
        }

        private async Task SaveToLocalStorageAsync(ClientLogDto clientLog)
        {
            try
            {
                var existingLogs = await GetLocalLogsAsync();
                existingLogs.Add(clientLog);

                // Keep only the most recent logs to prevent storage overflow
                if (existingLogs.Count > MAX_LOCAL_LOGS)
                {
                    existingLogs = existingLogs.OrderByDescending(l => l.Timestamp).Take(MAX_LOCAL_LOGS).ToList();
                }

                var json = JsonSerializer.Serialize(existingLogs);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LOCAL_STORAGE_KEY, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to save log to localStorage");
            }
        }

        private async Task RemoveFromLocalStorageAsync(List<ClientLogDto> logsToRemove)
        {
            try
            {
                var existingLogs = await GetLocalLogsAsync();
                var correlationIds = logsToRemove.Select(l => l.CorrelationId).ToHashSet();

                var filteredLogs = existingLogs.Where(l => !correlationIds.Contains(l.CorrelationId)).ToList();

                var json = JsonSerializer.Serialize(filteredLogs);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LOCAL_STORAGE_KEY, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to remove logs from localStorage");
            }
        }

        private async Task LoadPendingLogsAsync()
        {
            try
            {
                var pendingLogs = await GetLocalLogsAsync();
                if (pendingLogs.Count > 0)
                {
                    _logger?.LogInformation("Found {Count} pending logs to send", pendingLogs.Count);
                    await LogBatchAsync(pendingLogs);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load pending logs");
            }
        }

        private void OnFlushTimer(object? state)
        {
            _ = Task.Run(FlushAsync);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _flushTimer?.Dispose();
                _flushSemaphore?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}