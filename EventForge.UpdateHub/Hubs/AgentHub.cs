using EventForge.UpdateHub.Models;
using EventForge.UpdateHub.Services;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.UpdateHub.Hubs;

/// <summary>
/// SignalR hub for communication between the Update Hub and distributed UpdateAgent services.
/// Agents authenticate via API key passed as query parameter during negotiation.
/// </summary>
public class AgentHub(
    ILogger<AgentHub> logger,
    IConnectionTracker connectionTracker,
    IInstallationService installationService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var installationId = GetInstallationId();
        if (installationId is null)
        {
            logger.LogWarning("Agent connected without valid InstallationId claim [{ConnectionId}]", Context.ConnectionId);
            Context.Abort();
            return;
        }

        connectionTracker.Register(Context.ConnectionId, installationId.Value);
        await installationService.UpdateLastSeenAsync(installationId.Value, null, null, InstallationStatus.Online);

        logger.LogInformation("Agent connected: Installation={InstallationId} Connection={ConnectionId}",
            installationId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var installationId = GetInstallationId();
        if (installationId.HasValue)
        {
            connectionTracker.Unregister(Context.ConnectionId);
            await installationService.UpdateLastSeenAsync(installationId.Value, null, null, InstallationStatus.Offline);
            logger.LogInformation("Agent disconnected: Installation={InstallationId}", installationId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Agent → Hub: Register or refresh installation details on connect.</summary>
    public async Task RegisterInstallation(RegisterInstallationMessage msg)
    {
        var installationId = GetInstallationId();
        if (installationId is null) return;

        await installationService.UpdateLastSeenAsync(
            installationId.Value,
            msg.VersionServer,
            msg.VersionClient,
            InstallationStatus.Online);

        logger.LogInformation("Installation registered: {Name} Server={VersionServer} Client={VersionClient}",
            msg.InstallationName, msg.VersionServer, msg.VersionClient);

        await Clients.Caller.SendAsync("RegistrationConfirmed", new { installationId });
    }

    /// <summary>Agent → Hub: Periodic heartbeat.</summary>
    public async Task Heartbeat(HeartbeatMessage msg)
    {
        var installationId = GetInstallationId();
        if (installationId is null) return;

        var status = Enum.TryParse<InstallationStatus>(msg.Status, true, out var s) ? s : InstallationStatus.Online;
        await installationService.UpdateLastSeenAsync(installationId.Value, msg.VersionServer, msg.VersionClient, status);

        logger.LogDebug("Heartbeat from Installation={InstallationId} Status={Status}", installationId, msg.Status);
    }

    /// <summary>Agent → Hub: Report update progress.</summary>
    public async Task ReportUpdateProgress(UpdateProgressMessage msg)
    {
        var installationId = GetInstallationId();
        if (installationId is null) return;

        if (msg.IsCompleted)
        {
            var historyStatus = msg.IsSuccess ? UpdateHistoryStatus.Succeeded : UpdateHistoryStatus.Failed;
            await installationService.CompleteUpdateHistoryAsync(
                msg.UpdateHistoryId, historyStatus, msg.ErrorMessage, !msg.IsSuccess && msg.Phase == "Rollback");

            var instStatus = msg.IsSuccess ? InstallationStatus.Online : InstallationStatus.Error;
            await installationService.UpdateLastSeenAsync(installationId.Value, null, null, instStatus);
        }
        else
        {
            await installationService.UpdateProgressPhaseAsync(msg.UpdateHistoryId, msg.Phase);
            await installationService.UpdateLastSeenAsync(installationId.Value, null, null, InstallationStatus.Updating);
        }

        logger.LogInformation("Update progress Installation={InstallationId} Phase={Phase} Completed={IsCompleted} Success={IsSuccess}",
            installationId, msg.Phase, msg.IsCompleted, msg.IsSuccess);
    }

    private Guid? GetInstallationId()
    {
        // The API key middleware puts the installation ID into Items
        if (Context.GetHttpContext()?.Items["InstallationId"] is Guid id)
            return id;
        return null;
    }
}
