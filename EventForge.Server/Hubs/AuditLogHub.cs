using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time audit log updates.
/// </summary>
[Authorize]
public class AuditLogHub : Hub
{
    /// <summary>
    /// Joins the audit log group for receiving real-time updates.
    /// Only SuperAdmin users can join this group.
    /// </summary>
    /// <returns></returns>
    public async Task JoinAuditLogGroup()
    {
        if (Context.User?.IsInRole("SuperAdmin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuditLogUpdates");
        }
        else
        {
            throw new HubException("Access denied. SuperAdmin role required.");
        }
    }

    /// <summary>
    /// Leaves the audit log group.
    /// </summary>
    /// <returns></returns>
    public async Task LeaveAuditLogGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuditLogUpdates");
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("SuperAdmin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuditLogUpdates");
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuditLogUpdates");
        await base.OnDisconnectedAsync(exception);
    }
}