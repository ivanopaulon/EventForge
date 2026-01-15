using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.Services;

/// <summary>
/// Represents persisted filter state for a specific page.
/// This state is saved to localStorage to maintain user preferences across sessions.
/// </summary>
public class PageFilterState
{
    /// <summary>
    /// Search term entered by the user
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary of toggle states (e.g., "showOnlyFiscal" -> true)
    /// </summary>
    public Dictionary<string, bool> Toggles { get; set; } = new();

    /// <summary>
    /// Dictionary of dropdown selections (e.g., "status" -> "Active")
    /// </summary>
    public Dictionary<string, string> Selections { get; set; } = new();

    /// <summary>
    /// Timestamp of the last update
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Updates the last updated timestamp
    /// </summary>
    public void Touch()
    {
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents persisted panel/collapse state for a specific page.
/// This state is saved to localStorage to maintain user preferences across sessions.
/// </summary>
public class PagePanelState
{
    /// <summary>
    /// Dictionary of panel expansion states (e.g., "detailsPanel" -> true)
    /// </summary>
    public Dictionary<string, bool> PanelStates { get; set; } = new();

    /// <summary>
    /// Timestamp of the last update
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Updates the last updated timestamp
    /// </summary>
    public void Touch()
    {
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Service for managing filter and panel state persistence using localStorage.
/// </summary>
public interface IFilterStateService
{
    /// <summary>
    /// Saves filter state for a specific page.
    /// </summary>
    Task SaveFilterStateAsync(string pageKey, PageFilterState state);

    /// <summary>
    /// Loads filter state for a specific page.
    /// </summary>
    Task<PageFilterState?> LoadFilterStateAsync(string pageKey);

    /// <summary>
    /// Clears filter state for a specific page.
    /// </summary>
    Task ClearFilterStateAsync(string pageKey);

    /// <summary>
    /// Saves panel state for a specific page.
    /// </summary>
    Task SavePanelStateAsync(string pageKey, PagePanelState state);

    /// <summary>
    /// Loads panel state for a specific page.
    /// </summary>
    Task<PagePanelState?> LoadPanelStateAsync(string pageKey);

    /// <summary>
    /// Clears panel state for a specific page.
    /// </summary>
    Task ClearPanelStateAsync(string pageKey);
}

/// <summary>
/// Implementation of filter state service using localStorage for persistence.
/// </summary>
public class FilterStateService : IFilterStateService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<FilterStateService> _logger;
    private const string FilterKeyPrefix = "eventforge-filter-";
    private const string PanelKeyPrefix = "eventforge-panel-";

    public FilterStateService(IJSRuntime jsRuntime, ILogger<FilterStateService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task SaveFilterStateAsync(string pageKey, PageFilterState state)
    {
        try
        {
            state.Touch();
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"{FilterKeyPrefix}{pageKey}", json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving filter state for {PageKey}", pageKey);
        }
    }

    public async Task<PageFilterState?> LoadFilterStateAsync(string pageKey)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"{FilterKeyPrefix}{pageKey}");
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<PageFilterState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading filter state for {PageKey}", pageKey);
            return null;
        }
    }

    public async Task ClearFilterStateAsync(string pageKey)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{FilterKeyPrefix}{pageKey}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing filter state for {PageKey}", pageKey);
        }
    }

    public async Task SavePanelStateAsync(string pageKey, PagePanelState state)
    {
        try
        {
            state.Touch();
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"{PanelKeyPrefix}{pageKey}", json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving panel state for {PageKey}", pageKey);
        }
    }

    public async Task<PagePanelState?> LoadPanelStateAsync(string pageKey)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"{PanelKeyPrefix}{pageKey}");
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<PagePanelState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading panel state for {PageKey}", pageKey);
            return null;
        }
    }

    public async Task ClearPanelStateAsync(string pageKey)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{PanelKeyPrefix}{pageKey}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing panel state for {PageKey}", pageKey);
        }
    }
}
