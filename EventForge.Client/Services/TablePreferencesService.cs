using Microsoft.JSInterop;
using System.Text.Json;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Implementation of ITablePreferencesService for managing table preferences in localStorage.
    /// </summary>
    public class TablePreferencesService : ITablePreferencesService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IAuthService _authService;
        private readonly ILogger<TablePreferencesService> _logger;
        private const string PREFIX = "ef.tableprefs";

        public TablePreferencesService(
            IJSRuntime jsRuntime,
            IAuthService authService,
            ILogger<TablePreferencesService> logger)
        {
            _jsRuntime = jsRuntime;
            _authService = authService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<T?> GetPreferencesAsync<T>(string componentKey) where T : class
        {
            try
            {
                var storageKey = await BuildStorageKeyAsync(componentKey);
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogDebug("No preferences found for key: {StorageKey}", storageKey);
                    return null;
                }

                var preferences = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogDebug("Retrieved preferences for key: {StorageKey}", storageKey);
                return preferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving preferences for component: {ComponentKey}", componentKey);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SavePreferencesAsync<T>(string componentKey, T preferences) where T : class
        {
            try
            {
                var storageKey = await BuildStorageKeyAsync(componentKey);
                var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json);
                _logger.LogDebug("Saved preferences for key: {StorageKey}", storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving preferences for component: {ComponentKey}", componentKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ClearPreferencesAsync(string componentKey)
        {
            try
            {
                var storageKey = await BuildStorageKeyAsync(componentKey);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);
                _logger.LogDebug("Cleared preferences for key: {StorageKey}", storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing preferences for component: {ComponentKey}", componentKey);
                throw;
            }
        }

        /// <summary>
        /// Builds the storage key scoped to the current user (if authenticated) and component.
        /// Format: ef.tableprefs.{userId}.{componentKey} or ef.tableprefs.anonymous.{componentKey}
        /// </summary>
        private async Task<string> BuildStorageKeyAsync(string componentKey)
        {
            var userId = "anonymous";

            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser?.Id != null && currentUser.Id != Guid.Empty)
                {
                    userId = currentUser.Id.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current user for storage key, using anonymous");
            }

            return $"{PREFIX}.{userId}.{componentKey}";
        }
    }
}
