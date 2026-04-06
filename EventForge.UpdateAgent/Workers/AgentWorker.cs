using Microsoft.AspNetCore.SignalR.Client;

namespace EventForge.UpdateAgent.Workers;

/// <summary>
/// Main hosted service: manages the persistent SignalR connection to the UpdateHub,
/// sends heartbeats, and handles incoming update commands.
/// </summary>
public class AgentWorker(
    AgentOptions options,
    UpdateExecutorService updateExecutor,
    PendingInstallService pendingInstallService,
    AgentStatusService agentStatus,
    VersionDetectorService versionDetector,
    ILogger<AgentWorker> logger) : BackgroundService
{
    private HubConnection? _connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventForge UpdateAgent starting. InstallationId={Id} Name={Name}",
            options.InstallationId, options.InstallationName);

        // Restore persisted pending queue from previous runs.
        pendingInstallService.LoadFromDisk();

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
                agentStatus.HubConnectionState = "Disconnected";
                agentStatus.LastHeartbeatError = ex.Message;
                logger.LogError(ex, "Hub connection error. Reconnecting in 30 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        agentStatus.HubConnectionState = "Stopped";
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

        // ── StartUpdate: download immediately (resilient); install now or enqueue ──
        _connection.On<StartUpdateCommand>("StartUpdate", async command =>
        {
            logger.LogInformation("Received StartUpdate command: {Component} {Version} (PackageId={PackageId})",
                command.Component, command.Version, command.PackageId);

            try
            {
                // Phase 1+2 always run immediately, regardless of maintenance window.
                var zipPath = await updateExecutor.DownloadAndVerifyAsync(command, ct);

                if (pendingInstallService.IsInMaintenanceWindow())
                {
                    // Install right now in the current window.
                    logger.LogInformation("Maintenance window active — installing {Component} {Version} immediately.",
                        command.Component, command.Version);

                    try
                    {
                        await updateExecutor.InstallFromZipAsync(command, zipPath, ct);
                    }
                    catch (Exception ex)
                    {
                        // Block the queue: a failed direct install is just as dangerous for ordered migrations.
                        pendingInstallService.Block(command.PackageId,
                            $"Direct install failed for {command.Component} {command.Version}: {ex.Message}");
                    }
                }
                else
                {
                    // Enqueue for the next maintenance window.
                    pendingInstallService.Enqueue(command, zipPath);

                    var nextWindow = pendingInstallService.GetNextWindowStart();
                    logger.LogInformation(
                        "Outside maintenance window — {Component} {Version} queued. Next window: {Next}",
                        command.Component, command.Version, nextWindow?.ToString("u") ?? "unknown");

                    await ReportProgressAsync(command, UpdatePhase.AwaitingMaintenanceWindow,
                        isCompleted: false, isSuccess: true, errorMessage: null, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutdown in progress.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing StartUpdate for {Component} {Version}", command.Component, command.Version);
            }
        });

        // ── InstallNow: bypass the maintenance window for a specific queued package ──
        _connection.On<InstallNowCommand>("InstallNow", async command =>
        {
            logger.LogInformation("Received InstallNow for PackageId={PackageId}", command.PackageId);

            var pending = pendingInstallService.GetByPackageId(command.PackageId);
            if (pending is null)
            {
                logger.LogWarning("InstallNow: PackageId={PackageId} not found in queue.", command.PackageId);
                return;
            }

            // InstallNow bypasses the window but still respects queue order:
            // only allow if the requested package IS the head of the queue.
            var head = pendingInstallService.GetNext();
            if (head is null || head.PackageId != command.PackageId)
            {
                logger.LogWarning(
                    "InstallNow rejected: {PackageId} is not the queue head (head={HeadId}). Install must be sequential.",
                    command.PackageId, head?.PackageId.ToString() ?? "none");
                return;
            }

            if (!File.Exists(pending.LocalZipPath))
            {
                logger.LogError("InstallNow: zip file missing at {Path} — removing from queue.", pending.LocalZipPath);
                pendingInstallService.Remove(command.PackageId);
                return;
            }

            try
            {
                await updateExecutor.InstallFromZipAsync(pending.Command, pending.LocalZipPath, ct);
                pendingInstallService.Remove(command.PackageId);
            }
            catch (Exception ex)
            {
                pendingInstallService.Block(command.PackageId,
                    $"InstallNow failed for {pending.Command.Component} {pending.Command.Version}: {ex.Message}");
            }
        });

        // ── UnblockQueue: operator-initiated queue unblock (optionally skip failing entry) ──
        _connection.On<UnblockQueueCommand>("UnblockQueue", command =>
        {
            logger.LogWarning("Received UnblockQueue for PackageId={PackageId} SkipAndRemove={Skip}",
                command.PackageId, command.SkipAndRemove);
            pendingInstallService.Unblock(command.SkipAndRemove);
            return Task.CompletedTask;
        });

        // ── UpdateAvailable: informational only ──
        _connection.On<UpdateAvailableMessage>("UpdateAvailable", msg =>
        {
            logger.LogInformation("Update available: {Component} {Version}", msg.Component, msg.Version);
            return Task.CompletedTask;
        });

        // ── RequestStatus ──
        _connection.On<RequestStatusCommand>("RequestStatus", async _ =>
        {
            logger.LogDebug("Hub requested status");
            await SendHeartbeatAsync(ct);
        });

        _connection.Reconnecting += ex =>
        {
            agentStatus.HubConnectionState = "Reconnecting";
            logger.LogWarning("Reconnecting to hub: {Message}", ex?.Message);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            agentStatus.HubConnectionState = "Connected";
            logger.LogInformation("Reconnected to hub.");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(ct);
        agentStatus.HubConnectionState = "Connected";
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

        agentStatus.LastHeartbeatAt = DateTime.UtcNow;
        agentStatus.LastHeartbeatError = null;
    }

    private async Task ReportProgressAsync(StartUpdateCommand command, UpdatePhase phase,
        bool isCompleted, bool isSuccess, string? errorMessage, CancellationToken ct)
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        var msg = new UpdateProgressMessage(
            options.InstallationId,
            command.UpdateHistoryId,
            phase.ToString(),
            isCompleted,
            isSuccess,
            errorMessage);

        await _connection.InvokeAsync("ReportUpdateProgress", msg, ct);
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
