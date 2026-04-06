using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;

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
    SystemInfoService systemInfo,
    CommandTrackingService commandTracking,
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
                // If ApiKey is missing, attempt self-enrollment before connecting.
                if (string.IsNullOrWhiteSpace(options.ApiKey) ||
                    options.ApiKey == "REPLACE_WITH_INSTALLATION_API_KEY")
                {
                    await TryEnrollAsync(stoppingToken);
                }

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
                logger.LogError(ex, "Hub connection error. Reconnecting in {Delay}s...", options.ReconnectDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(options.ReconnectDelaySeconds), stoppingToken);
            }
        }

        agentStatus.HubConnectionState = "Stopped";
        logger.LogInformation("EventForge UpdateAgent stopped.");
    }

    /// <summary>
    /// Calls POST {HubBaseUrl}/api/v1/enrollments to request a new API key.
    /// On success the key and InstallationId are persisted to appsettings.json.
    /// </summary>
    private async Task TryEnrollAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.EnrollmentToken))
        {
            logger.LogWarning(
                "ApiKey is not set and EnrollmentToken is empty. " +
                "Configure UpdateAgent:ApiKey or set UpdateAgent:EnrollmentToken to enable auto-enrollment.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.InstallationCode))
        {
            logger.LogError("InstallationCode is not set. Cannot enroll without a stable identity. " +
                            "Ensure InstallationCodeGenerator ran before enrollment.");
            return;
        }

        var baseUrl = string.IsNullOrWhiteSpace(options.HubBaseUrl) ? options.HubUrl : options.HubBaseUrl;
        var enrollUrl = baseUrl.TrimEnd('/').Replace("/hubs/update", "") + "/api/v1/enrollments";

        logger.LogInformation("ApiKey not set — requesting enrollment from {Url} (Code={Code})",
            enrollUrl, options.InstallationCode);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            var body = new
            {
                EnrollmentToken = options.EnrollmentToken,
                InstallationCode = options.InstallationCode,
                InstallationName = options.InstallationName,
                HintInstallationId = string.IsNullOrWhiteSpace(options.InstallationId) ||
                                     options.InstallationId == "00000000-0000-0000-0000-000000000000"
                    ? (Guid?)null
                    : Guid.Parse(options.InstallationId),
                Location = options.Location,
                Components = (int)MapComponents(),
                MachineName   = systemInfo.MachineName,
                OSVersion     = systemInfo.OSVersion,
                DotNetVersion = systemInfo.DotNetVersion,
                AgentVersion  = versionDetector.GetAgentVersion(),
                Tags          = options.Tags
            };

            var response = await http.PostAsJsonAsync(enrollUrl, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Enrollment failed ({Status}): {Error}", response.StatusCode, err);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<EnrollmentResult>(cancellationToken: ct);
            if (result is null || string.IsNullOrWhiteSpace(result.ApiKey))
            {
                logger.LogError("Enrollment response was empty or missing ApiKey.");
                return;
            }

            // Apply in-memory immediately
            options.ApiKey = result.ApiKey;
            options.InstallationId = result.InstallationId.ToString();

            // Persist to appsettings.json so the key survives restarts
            await PersistEnrollmentAsync(result.ApiKey, result.InstallationId);

            logger.LogInformation("Enrollment successful. InstallationId={Id} Code={Code}",
                result.InstallationId, options.InstallationCode);
            agentStatus.EnrollmentStatus = "Enrolled";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Enrollment request failed.");
        }
    }

    private async Task PersistEnrollmentAsync(string apiKey, Guid installationId)
    {
        try
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(appSettingsPath)) return;

            var json = await File.ReadAllTextAsync(appSettingsPath);
            var doc = JsonDocument.Parse(json);

            // Rebuild the JSON updating only ApiKey and InstallationId
            using var stream = new MemoryStream();
            var writerOpts = new JsonWriterOptions { Indented = true };
            using (var writer = new Utf8JsonWriter(stream, writerOpts))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name == "UpdateAgent")
                    {
                        writer.WritePropertyName("UpdateAgent");
                        writer.WriteStartObject();
                        foreach (var agentProp in prop.Value.EnumerateObject())
                        {
                            if (agentProp.Name == "ApiKey")
                                writer.WriteString("ApiKey", apiKey);
                            else if (agentProp.Name == "InstallationId")
                                writer.WriteString("InstallationId", installationId.ToString());
                            else
                                agentProp.WriteTo(writer);
                        }
                        // Ensure EnrollmentToken is cleared after successful enrollment
                        // (keeps it only if it was already absent — we don't clear it so re-enrollment stays possible)
                        writer.WriteEndObject();
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }

            await File.WriteAllBytesAsync(appSettingsPath, stream.ToArray());
            logger.LogInformation("Enrollment credentials persisted to appsettings.json.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist enrollment credentials to appsettings.json.");
        }
    }

    private int MapComponents()
    {
        var server = options.Components.Server.Enabled;
        var client = options.Components.Client.Enabled;
        return (server, client) switch
        {
            (true, true) => 3,   // Both
            (true, false) => 1,  // Server
            (false, true) => 2,  // Client
            _ => 3               // default Both
        };
    }

    private record EnrollmentResult(Guid InstallationId, string InstallationCode, string ApiKey);

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

            commandTracking.Track(command);

            try
            {
                // Phase 1+2 always run immediately, regardless of maintenance window.
                commandTracking.SetState(command.PackageId, CommandState.Downloading);
                string zipPath;
                try
                {
                    zipPath = await updateExecutor.DownloadAndVerifyAsync(command, ct);
                    commandTracking.SetState(command.PackageId, CommandState.Downloaded);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    commandTracking.SetState(command.PackageId, CommandState.DownloadFailed, ex.Message);
                    throw;
                }

                if (command.IsManualInstall)
                {
                    // Manual mode: always enqueue, never auto-install
                    pendingInstallService.Enqueue(command, zipPath);
                    logger.LogInformation("Manual mode: {Component} {Version} queued for operator approval.", command.Component, command.Version);
                    await ReportProgressAsync(command, UpdatePhase.AwaitingMaintenanceWindow, false, true, null, ct);
                }
                else if (pendingInstallService.IsInMaintenanceWindow())
                {
                    // Auto mode within window: install immediately
                    logger.LogInformation("Maintenance window active — installing {Component} {Version} immediately.",
                        command.Component, command.Version);

                    commandTracking.SetState(command.PackageId, CommandState.Installing);
                    try
                    {
                        await updateExecutor.InstallFromZipAsync(command, zipPath, ct);
                        commandTracking.SetState(command.PackageId, CommandState.Installed);
                        commandTracking.Remove(command.PackageId);
                    }
                    catch (Exception ex)
                    {
                        commandTracking.SetState(command.PackageId, CommandState.Failed, ex.Message);
                        // Block the queue: a failed direct install is just as dangerous for ordered migrations.
                        pendingInstallService.Block(command.PackageId,
                            $"Direct install failed for {command.Component} {command.Version}: {ex.Message}");
                    }
                }
                else
                {
                    // Auto mode outside window: enqueue
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

        // ── UpdateAvailable: check if newer version, then request update from Hub ──
        _connection.On<UpdateAvailableMessage>("UpdateAvailable", async msg =>
        {
            logger.LogInformation("UpdateAvailable received: {Component} {Version} (PackageId={PackageId})",
                msg.Component, msg.Version, msg.PackageId);

            // Check whether this agent manages the offered component.
            var isServer = msg.Component.Equals("Server", StringComparison.OrdinalIgnoreCase);
            var isClient = msg.Component.Equals("Client", StringComparison.OrdinalIgnoreCase);

            if (isServer && !options.Components.Server.Enabled)
            {
                logger.LogDebug("UpdateAvailable: Server component not managed by this agent — skipping.");
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                return;
            }

            if (isClient && !options.Components.Client.Enabled)
            {
                logger.LogDebug("UpdateAvailable: Client component not managed by this agent — skipping.");
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                return;
            }

            if (!isServer && !isClient)
            {
                logger.LogWarning("UpdateAvailable: Unknown component '{Component}' — skipping.", msg.Component);
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                return;
            }

            // Compare the offered version with the currently installed version.
            var installedVersion = isServer
                ? versionDetector.GetServerVersion()
                : versionDetector.GetClientVersion();

            if (!IsNewerVersion(msg.Version, installedVersion))
            {
                logger.LogInformation(
                    "UpdateAvailable: Already up-to-date (installed={Installed}, offered={Offered}) — skipping.",
                    installedVersion ?? "none", msg.Version);
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                return;
            }

            logger.LogInformation(
                "UpdateAvailable: Newer version detected (offered={Offered} > installed={Installed}). Requesting update from Hub.",
                msg.Version, installedVersion ?? "none");

            try
            {
                await _connection.InvokeAsync("RequestStartUpdate", msg.PackageId, ct);
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: true);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "UpdateAvailable: Failed to invoke RequestStartUpdate on Hub.");
                commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: true);
            }
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

        // Register on connect — send full identity so Hub stays up-to-date
        await _connection.InvokeAsync("RegisterInstallation", new RegisterInstallationMessage(
            InstallationId:   options.InstallationId,
            InstallationName: options.InstallationName,
            VersionServer:    versionDetector.GetServerVersion(),
            VersionClient:    versionDetector.GetClientVersion(),
            Components:       new InstallationComponentsDto(
                                  options.Components.Server.Enabled,
                                  options.Components.Client.Enabled),
            InstallationCode: options.InstallationCode,
            Location:         options.Location,
            Tags:             options.Tags.Count > 0 ? options.Tags : null,
            MachineName:      systemInfo.MachineName,
            OSVersion:        systemInfo.OSVersion,
            DotNetVersion:    systemInfo.DotNetVersion,
            AgentVersion:     versionDetector.GetAgentVersion()),
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
            InstallationId: options.InstallationId,
            VersionServer:  versionDetector.GetServerVersion(),
            VersionClient:  versionDetector.GetClientVersion(),
            Status:         "Online",
            Timestamp:      DateTime.UtcNow,
            AgentVersion:   versionDetector.GetAgentVersion(),
            Location:       options.Location,
            Tags:           options.Tags.Count > 0 ? options.Tags : null),
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

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="offered"/> is strictly greater than
    /// <paramref name="installed"/>. Returns <see langword="true"/> when <paramref name="installed"/>
    /// is null/empty (component not yet installed on this machine).
    /// Falls back to ordinal string comparison for non-parseable version strings.
    /// </summary>
    private static bool IsNewerVersion(string offered, string? installed)
    {
        if (string.IsNullOrWhiteSpace(installed)) return true;

        // Nerdbank.GitVersioning may emit "1.2.3+g1a2b3c4" — strip the build metadata.
        // Strip Nerdbank.GitVersioning build metadata (e.g. "1.2.3+g1a2b3c4" → "1.2.3")
        // so that Version.TryParse can handle the string correctly.
        static string Strip(string v)
        {
            var plus = v.IndexOf('+');
            return plus >= 0 ? v[..plus] : v;
        }

        if (Version.TryParse(Strip(offered), out var o) &&
            Version.TryParse(Strip(installed), out var i))
            return o > i;

        // Fallback: ordinal string compare.
        return string.Compare(offered, installed, StringComparison.OrdinalIgnoreCase) > 0;
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
