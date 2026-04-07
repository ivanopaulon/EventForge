using Microsoft.JSInterop;
using System.Text.Json;

namespace Prym.Client.Services
{
    /// <summary>
    /// Implementation of ITablePreferencesService for managing table preferences in localStorage.
    /// </summary>
    public class TablePreferencesService(
        IJSRuntime jsRuntime,
        IAuthService authService,
        ILogger<TablePreferencesService> logger) : ITablePreferencesService
    {
        private const string PREFIX = "ef.tableprefs";

        /// <inheritdoc />
        public async Task<T?> GetPreferencesAsync<T>(string componentKey) where T : class
        {
            try
            {
                var storageKey = await BuildStorageKeyAsync(componentKey);
                var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);

                if (string.IsNullOrWhiteSpace(json))
                {
                    logger.LogDebug("No preferences found for key: {StorageKey}", storageKey);
                    return null;
                }

                var preferences = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                logger.LogDebug("Retrieved preferences for key: {StorageKey}", storageKey);
                return preferences;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving preferences for component: {ComponentKey}", componentKey);
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

                await jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json);
                logger.LogDebug("Saved preferences for key: {StorageKey}", storageKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving preferences for component: {ComponentKey}", componentKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ClearPreferencesAsync(string componentKey)
        {
            try
            {
                var storageKey = await BuildStorageKeyAsync(componentKey);
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);
                logger.LogDebug("Cleared preferences for key: {StorageKey}", storageKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing preferences for component: {ComponentKey}", componentKey);
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
                var currentUser = await authService.GetCurrentUserAsync();
                if (currentUser?.Id is not null && currentUser.Id != Guid.Empty)
                {
                    userId = currentUser.Id.ToString();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error getting current user for storage key, using anonymous");
            }

            return $"{PREFIX}.{userId}.{componentKey}";
        }
    }
}
