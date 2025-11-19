using System.Threading.Tasks;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Service for managing table preferences (column order, visibility, grouping) in localStorage.
    /// Preferences are scoped per user (if authenticated) and per component key.
    /// </summary>
    public interface ITablePreferencesService
    {
        /// <summary>
        /// Retrieves table preferences for the specified component key.
        /// Key is automatically scoped to the current user if authenticated.
        /// </summary>
        /// <typeparam name="T">Type of preferences object to deserialize</typeparam>
        /// <param name="componentKey">Unique identifier for the component (e.g., "VatRateManagement")</param>
        /// <returns>Preferences object if found, otherwise null</returns>
        Task<T?> GetPreferencesAsync<T>(string componentKey) where T : class;

        /// <summary>
        /// Saves table preferences for the specified component key.
        /// Key is automatically scoped to the current user if authenticated.
        /// </summary>
        /// <typeparam name="T">Type of preferences object to serialize</typeparam>
        /// <param name="componentKey">Unique identifier for the component (e.g., "VatRateManagement")</param>
        /// <param name="preferences">Preferences object to save</param>
        Task SavePreferencesAsync<T>(string componentKey, T preferences) where T : class;

        /// <summary>
        /// Clears table preferences for the specified component key.
        /// Key is automatically scoped to the current user if authenticated.
        /// </summary>
        /// <param name="componentKey">Unique identifier for the component (e.g., "VatRateManagement")</param>
        Task ClearPreferencesAsync(string componentKey);
    }
}
