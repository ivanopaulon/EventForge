using Microsoft.JSInterop;

namespace EventForge.Client.Services;

/// <summary>
/// Represents persisted inventory session state.
/// This state is saved to localStorage for recovery after page refresh.
/// </summary>
public class InventorySessionState
{
    /// <summary>
    /// Version of the state schema for migration support
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// ID of the current inventory document
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Document number for display
    /// </summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// ID of the selected storage facility (warehouse)
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Name of the selected storage facility
    /// </summary>
    public string? WarehouseName { get; set; }

    /// <summary>
    /// Timestamp when the session was started
    /// </summary>
    public DateTime SessionStartTime { get; set; }

    /// <summary>
    /// Timestamp of the last activity in this session
    /// </summary>
    public DateTime LastActivityTime { get; set; }

    /// <summary>
    /// Fast confirm mode enabled/disabled
    /// </summary>
    public bool FastConfirmEnabled { get; set; } = true;

    /// <summary>
    /// Filter to show only adjustments in the grid
    /// </summary>
    public bool ShowOnlyAdjustments { get; set; } = false;

    /// <summary>
    /// Updates the last activity timestamp
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates if the session state is still valid
    /// </summary>
    public bool IsValid()
    {
        return DocumentId != Guid.Empty && 
               WarehouseId.HasValue && 
               !string.IsNullOrWhiteSpace(DocumentNumber);
    }

    /// <summary>
    /// Checks if the session has been inactive for too long
    /// </summary>
    public bool IsExpired(TimeSpan maxInactivity)
    {
        return (DateTime.UtcNow - LastActivityTime) > maxInactivity;
    }
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
