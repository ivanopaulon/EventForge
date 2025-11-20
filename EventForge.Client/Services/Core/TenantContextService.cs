using EventForge.DTOs.Tenants;
using Microsoft.JSInterop;

namespace EventForge.Client.Services.Core;

/// <summary>
/// Interface for tenant context service.
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Gets the current selected tenant ID.
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Gets the current selected tenant information.
    /// </summary>
    TenantResponseDto? CurrentTenant { get; }

    /// <summary>
    /// Event triggered when tenant selection changes.
    /// </summary>
    event EventHandler<Guid?>? TenantChanged;

    /// <summary>
    /// Sets the current tenant context.
    /// </summary>
    /// <param name="tenantId">Tenant ID to set as current</param>
    /// <param name="tenant">Optional tenant details</param>
    Task SetCurrentTenantAsync(Guid? tenantId, TenantResponseDto? tenant = null);

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    Task ClearCurrentTenantAsync();

    /// <summary>
    /// Initializes the service and loads persisted tenant selection.
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Service for managing tenant context across the application.
/// This service tracks the currently selected tenant in management pages
/// and provides it to drawers and other components that need pre-selection.
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TenantContextService> _logger;
    private const string TENANT_STORAGE_KEY = "eventforge_current_tenant";

    public Guid? CurrentTenantId { get; private set; }
    public TenantResponseDto? CurrentTenant { get; private set; }
    public event EventHandler<Guid?>? TenantChanged;

    public TenantContextService(IJSRuntime jsRuntime, ILogger<TenantContextService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the service and loads any persisted tenant selection from browser storage.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var savedTenantId = await GetSavedTenantAsync();
            if (savedTenantId.HasValue)
            {
                CurrentTenantId = savedTenantId;
                _logger.LogDebug("Loaded saved tenant context: {TenantId}", savedTenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing tenant context service");
        }
    }

    /// <summary>
    /// Sets the current tenant context and persists it to browser storage.
    /// </summary>
    /// <param name="tenantId">Tenant ID to set as current</param>
    /// <param name="tenant">Optional tenant details</param>
    public async Task SetCurrentTenantAsync(Guid? tenantId, TenantResponseDto? tenant = null)
    {
        try
        {
            var previousTenantId = CurrentTenantId;
            CurrentTenantId = tenantId;
            CurrentTenant = tenant;

            // Persist to browser storage
            if (tenantId.HasValue)
            {
                await SaveTenantAsync(tenantId.Value);
            }
            else
            {
                await ClearSavedTenantAsync();
            }

            // Notify subscribers of the change
            if (previousTenantId != tenantId)
            {
                TenantChanged?.Invoke(this, tenantId);
                _logger.LogDebug("Tenant context changed from {PreviousTenant} to {NewTenant}",
                    previousTenantId, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tenant context to {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    public async Task ClearCurrentTenantAsync()
    {
        await SetCurrentTenantAsync(null);
    }

    /// <summary>
    /// Saves tenant preference to local storage.
    /// </summary>
    private async Task SaveTenantAsync(Guid tenantId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TENANT_STORAGE_KEY, tenantId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tenant preference");
        }
    }

    /// <summary>
    /// Gets saved tenant preference from local storage.
    /// </summary>
    private async Task<Guid?> GetSavedTenantAsync()
    {
        try
        {
            var savedValue = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TENANT_STORAGE_KEY);
            if (!string.IsNullOrEmpty(savedValue) && Guid.TryParse(savedValue, out var tenantId))
            {
                return tenantId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved tenant preference");
        }
        return null;
    }

    /// <summary>
    /// Clears saved tenant preference from local storage.
    /// </summary>
    private async Task ClearSavedTenantAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TENANT_STORAGE_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing saved tenant preference");
        }
    }
}