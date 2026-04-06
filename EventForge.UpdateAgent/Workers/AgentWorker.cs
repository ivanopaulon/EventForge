using Microsoft.AspNetCore.SignalR.Client;

namespace EventForge.UpdateAgent.Workers;

/// <summary>
/// Main hosted service: manages the persistent SignalR connection to the UpdateHub,
/// sends heartbeats, and handles incoming update commands.
/// </summary>
public class AgentWorker(
    AgentOptions options,
    UpdateExecutorService updateExecutor,
    VersionDetectorService versionDetector,
    ILogger<AgentWorker> logger) : BackgroundService
{
    private HubConnection? _connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventForge UpdateAgent starting. InstallationId={Id} Name={Name}",
            options.InstallationId, options.InstallationName);

        updateExecutor.OnProgress += async msg =>
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("ReportUpdateProgress", msg, stoppingToken);
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndRunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hub connection error. Reconnecting in 30 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        logger.LogInformation("EventForge UpdateAgent stopped.");
    }

    private async Task ConnectAndRunAsync(CancellationToken ct)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(options.HubUrl, opts =>
            {
                opts.Headers["X-Api-Key"] = options.ApiKey;
            })
            .WithAutomaticReconnect()
            .Build();

        // Register handlers for hub → agent messages
        _connection.On<StartUpdateCommand>("StartUpdate", async command =>
        {
            logger.LogInformation("Received StartUpdate command: {Component} {Version}", command.Component, command.Version);
            await updateExecutor.ExecuteAsync(command, ct);
        });

        _connection.On<UpdateAvailableMessage>("UpdateAvailable", msg =>
        {
            logger.LogInformation("Update available: {Component} {Version}", msg.Component, msg.Version);
            return Task.CompletedTask;
        });

        _connection.On<RequestStatusCommand>("RequestStatus", async _ =>
        {
            logger.LogDebug("Hub requested status");
            await SendHeartbeatAsync(ct);
        });

        _connection.Reconnecting += ex =>
        {
            logger.LogWarning("Reconnecting to hub: {Message}", ex?.Message);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            logger.LogInformation("Reconnected to hub.");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(ct);
        logger.LogInformation("Connected to UpdateHub at {Url}", options.HubUrl);

        // Register on connect
        await _connection.InvokeAsync("RegisterInstallation", new RegisterInstallationMessage(
            options.InstallationId,
            options.InstallationName,
            versionDetector.GetServerVersion(),
            versionDetector.GetClientVersion(),
            new InstallationComponentsDto(
                options.Components.Server.Enabled,
                options.Components.Client.Enabled)),
            ct);

        // Heartbeat loop
        while (!ct.IsCancellationRequested && _connection.State == HubConnectionState.Connected)
        {
            await Task.Delay(TimeSpan.FromSeconds(options.HeartbeatIntervalSeconds), ct);
            await SendHeartbeatAsync(ct);
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken ct)
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        await _connection.InvokeAsync("Heartbeat", new HeartbeatMessage(
            options.InstallationId,
            versionDetector.GetServerVersion(),
            versionDetector.GetClientVersion(),
            "Online",
            DateTime.UtcNow),
            ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
