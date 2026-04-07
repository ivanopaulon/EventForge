using EventForge.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub that propagates maintenance and update events to connected browser clients.
///
/// Groups:
///   "all_clients"  — all authenticated users (receives MaintenanceStarted / MaintenanceEnded / ClientUpdateDeployed / UpdateProgress)
///   "superadmin"   — SuperAdmin users only (additionally receives UpdatesAvailable)
/// </summary>
[Authorize]
public class UpdateNotificationHub(
    ILogger<UpdateNotificationHub> logger,
    UpdatesAvailableRefreshService refreshService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_clients");

        if (Context.User?.IsInRole("SuperAdmin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "superadmin");
            logger.LogDebug("SuperAdmin {UserId} joined update-notification groups", GetUserId());

            // Push the current package count immediately so the FAB badge is up-to-date
            // without waiting for the next periodic broadcast.
            _ = Task.Run(async () =>
            {
                try { await refreshService.BroadcastCountAsync(); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to push initial UpdatesAvailable count."); }
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_clients");

        if (Context.User?.IsInRole("SuperAdmin") == true)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "superadmin");

        await base.OnDisconnectedAsync(exception);
    }

    private string? GetUserId()
        => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
