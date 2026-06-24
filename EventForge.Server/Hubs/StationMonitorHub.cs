using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Hubs;

[Authorize]
public class StationMonitorHub : Hub
{
    public async Task JoinStation(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{stationId}");
    }

    public async Task LeaveStation(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station-{stationId}");
    }
}
