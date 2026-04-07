using EventForge.Server.Services.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time price alert notifications.
/// Part of FASE 5: Price Alerts System.
/// </summary>
[Authorize]
public class AlertHub : Hub
{
    private readonly ILogger<AlertHub> _logger;
    private readonly ISupplierPriceAlertService _alertService;

    public AlertHub(
        ILogger<AlertHub> logger,
        ISupplierPriceAlertService alertService)
    {
        _logger = logger;
        _alertService = alertService;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically joins user to their tenant alert group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (userId.HasValue && tenantId.HasValue)
        {
            // Join tenant-wide alert group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId.Value}");

            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId.Value}");

            _logger.LogInformation("User {UserId} connected to alert hub for tenant {TenantId}",
                userId.Value, tenantId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (userId.HasValue && tenantId.HasValue)
        {
            _logger.LogInformation("User {UserId} disconnected from alert hub for tenant {TenantId}",
                userId.Value, tenantId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to alerts for the current user.
    /// </summary>
    public async Task SubscribeToAlerts()
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (userId.HasValue && tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId.Value}");
            _logger.LogDebug("User {UserId} subscribed to alerts", userId.Value);
        }
    }

    /// <summary>
    /// Get current unread alert count for the connected user.
    /// </summary>
    public async Task<int> GetUnreadCount()
    {
        try
        {
            return await _alertService.GetUnreadAlertCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user");
            return 0;
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }
}
