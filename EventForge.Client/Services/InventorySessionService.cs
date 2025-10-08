using Microsoft.JSInterop;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Represents persisted inventory session state.
/// </summary>
public class InventorySessionState
{
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public DateTime SessionStartTime { get; set; }
}

/// <summary>
/// Service for managing inventory session persistence using localStorage.
/// </summary>
public interface IInventorySessionService
{
    /// <summary>
    /// Saves the current inventory session state.
    /// </summary>
    Task SaveSessionAsync(InventorySessionState state);

    /// <summary>
    /// Loads the current inventory session state.
    /// </summary>
    Task<InventorySessionState?> LoadSessionAsync();

    /// <summary>
    /// Clears the current inventory session state.
    /// </summary>
    Task ClearSessionAsync();

    /// <summary>
    /// Checks if there's an active inventory session.
    /// </summary>
    Task<bool> HasActiveSessionAsync();
}

/// <summary>
/// Implementation of inventory session service using localStorage for persistence.
/// </summary>
public class InventorySessionService : IInventorySessionService
{
    private readonly IJSRuntime _jsRuntime;
    private const string SessionKey = "eventforge-inventory-session";

    public InventorySessionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveSessionAsync(InventorySessionState state)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving inventory session: {ex.Message}");
        }
    }

    public async Task<InventorySessionState?> LoadSessionAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<InventorySessionState>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading inventory session: {ex.Message}");
            return null;
        }
    }

    public async Task ClearSessionAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing inventory session: {ex.Message}");
        }
    }

    public async Task<bool> HasActiveSessionAsync()
    {
        var session = await LoadSessionAsync();
        return session != null;
    }
}
