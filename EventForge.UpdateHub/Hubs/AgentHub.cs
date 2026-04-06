using EventForge.UpdateHub.Configuration;
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
    IInstallationService installationService,
    IPackageService packageService,
    UpdateHubOptions hubOptions) : Hub
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

        var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

        await installationService.UpdateRegistrationInfoAsync(
            installationId.Value,
            new RegistrationInfo(
                Name:          msg.InstallationName,
                Location:      msg.Location,
                VersionServer: msg.VersionServer,
                VersionClient: msg.VersionClient,
                MachineName:   msg.MachineName,
                OSVersion:     msg.OSVersion,
                DotNetVersion: msg.DotNetVersion,
                AgentVersion:  msg.AgentVersion,
                IpAddress:     ip,
                Tags:          msg.Tags is { Count: > 0 } t ? string.Join(",", t) : null,
                Status:        InstallationStatus.Online));

        logger.LogInformation(
            "Installation registered: {Name} Machine={Machine} OS={OS} Agent={AgentVer} IP={IP}",
            msg.InstallationName, msg.MachineName, msg.OSVersion, msg.AgentVersion, ip);

        await Clients.Caller.SendAsync("RegistrationConfirmed", new { installationId });
    }

    /// <summary>Agent → Hub: Periodic heartbeat.</summary>
    public async Task Heartbeat(HeartbeatMessage msg)
    {
        var installationId = GetInstallationId();
        if (installationId is null) return;

        var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
        var status = Enum.TryParse<InstallationStatus>(msg.Status, true, out var s) ? s : InstallationStatus.Online;

        await installationService.UpdateRegistrationInfoAsync(
            installationId.Value,
            new RegistrationInfo(
                Name:          null,
                Location:      msg.Location,
                VersionServer: msg.VersionServer,
                VersionClient: msg.VersionClient,
                MachineName:   null,
                OSVersion:     null,
                DotNetVersion: null,
                AgentVersion:  msg.AgentVersion,
                IpAddress:     ip,
                Tags:          msg.Tags is { Count: > 0 } t ? string.Join(",", t) : null,
                Status:        status));

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

    /// <summary>
    /// Agent → Hub: Agent received an <see cref="UpdateAvailableMessage"/> broadcast and detected
    /// its installed version is older. The Hub creates a history record and sends back a
    /// <c>StartUpdate</c> command to this specific agent.
    /// </summary>
    public async Task RequestStartUpdate(Guid packageId)
    {
        var installationId = GetInstallationId();
        if (installationId is null) return;

        var pkg = await packageService.GetByIdAsync(packageId);
        if (pkg is null)
        {
            logger.LogWarning("RequestStartUpdate: Package {PackageId} not found.", packageId);
            return;
        }

        if (pkg.Status == PackageStatus.Archived)
        {
            logger.LogWarning("RequestStartUpdate: Package {PackageId} is archived — ignoring.", packageId);
            return;
        }

        var installation = await installationService.GetByIdAsync(installationId.Value);
        if (installation is null) return;

        var history = await installationService.StartUpdateHistoryAsync(
            installationId.Value, packageId,
            installation.InstalledVersionServer,
            installation.InstalledVersionClient);

        var httpContext = Context.GetHttpContext();
        var baseUrl = !string.IsNullOrWhiteSpace(hubOptions.BaseUrl)
            ? hubOptions.BaseUrl.TrimEnd('/')
            : $"{httpContext?.Request.Scheme}://{httpContext?.Request.Host}";

        var command = new StartUpdateCommand(
            history.Id,
            pkg.Id,
            pkg.Version,
            pkg.Component.ToString(),
            $"{baseUrl}/api/v1/packages/{pkg.Id}/download",
            pkg.Checksum,
            IsManualInstall: pkg.IsManualInstall || installation.UpdateMode == InstallationUpdateMode.Manual);

        await Clients.Caller.SendAsync("StartUpdate", command);
        await packageService.SetStatusAsync(packageId, PackageStatus.Deploying);

        logger.LogInformation(
            "RequestStartUpdate: StartUpdate sent to Installation={InstallationId} Package={PackageId} Version={Version}",
            installationId, packageId, pkg.Version);
    }

    private Guid? GetInstallationId()
    {
        // The API key middleware puts the installation ID into Items
        if (Context.GetHttpContext()?.Items["InstallationId"] is Guid id)
            return id;
        return null;
    }
}
