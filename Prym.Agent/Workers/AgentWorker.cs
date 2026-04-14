using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prym.Agent.Workers;

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
        logger.LogInformation("Prym Agent starting. InstallationId={Id} Name={Name}",
            options.InstallationId, options.InstallationName);

        // Standalone mode: printer-proxy only — skip Hub connection entirely.
        if (options.StandaloneMode || string.IsNullOrWhiteSpace(options.HubUrl))
        {
            agentStatus.HubConnectionState = "Standalone";
            if (options.StandaloneMode)
                logger.LogInformation(
                    "Agent running in standalone (printer-proxy-only) mode. Hub connection disabled.");
            else
                logger.LogWarning(
                    "HubUrl is not configured — Agent running without Hub connection. " +
                    "Set PrymAgent:HubUrl or enable PrymAgent:StandaloneMode to suppress this warning.");

            // Keep the hosted service alive while the application is running.
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            agentStatus.HubConnectionState = "Stopped";
            return;
        }

        // Restore persisted pending queue from previous runs.
        pendingInstallService.LoadFromDisk();

        updateExecutor.OnProgress += async msg =>
        {
            // Invalidate the version cache on successful install so the next heartbeat
            // reports the freshly deployed version without waiting for the 30 s TTL.
            if (msg.Phase == UpdatePhase.Completed.ToString() && msg.IsSuccess)
                versionDetector.InvalidateVersionCache();

            if (_connection?.State != HubConnectionState.Connected) return;
            try
            {
                await _connection.InvokeAsync("ReportUpdateProgress", msg, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to report update progress to Hub (phase={Phase}) — install continues.",
                    msg.Phase);
            }
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

                    // If enrollment failed (Hub down, 409, etc.) skip the doomed connection
                    // attempt and wait before the next retry, rather than logging a
                    // misleading 401/403 connection error.
                    if (string.IsNullOrWhiteSpace(options.ApiKey) ||
                        options.ApiKey == "REPLACE_WITH_INSTALLATION_API_KEY")
                    {
                        await Task.Delay(TimeSpan.FromSeconds(options.ReconnectDelaySeconds), stoppingToken);
                        continue;
                    }
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
                // Connection-refused means ManagementHub is not running — log at Warning (not Error)
                // to avoid flooding the log with stack traces during development.
                if (IsConnectionRefused(ex))
                    logger.LogWarning(
                        "ManagementHub not reachable at {Url} (connection refused). " +
                        "Make sure Prym.ManagementHub is running. Reconnecting in {Delay}s...",
                        options.HubUrl, options.ReconnectDelaySeconds);
                else
                    logger.LogError(ex, "Hub connection error. Reconnecting in {Delay}s...", options.ReconnectDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(options.ReconnectDelaySeconds), stoppingToken);
            }
        }

        agentStatus.HubConnectionState = "Stopped";
        logger.LogInformation("Prym Agent stopped.");
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
                "Configure PrymAgent:ApiKey or set PrymAgent:EnrollmentToken to enable auto-enrollment.");
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
                LocalIpAddress = systemInfo.LocalIpAddress,
                Tags          = options.Tags
            };

            var response = await http.PostAsJsonAsync(enrollUrl, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // 409: Hub already has a record for this InstallationCode (idempotent re-enrollment).
                    // This happens when the ApiKey was not persisted on a previous run (e.g. power loss
                    // after enrollment but before PersistEnrollmentAsync completed).
                    // The Hub never re-issues the ApiKey for security; an admin must reissue it via
                    // POST /api/v1/enrollments/{id}/reissue, then set PrymAgent:ApiKey in appsettings.json.
                    logger.LogWarning(
                        "Enrollment conflict: InstallationCode {Code} is already registered on the Hub " +
                        "but no ApiKey was found locally. Ask the Hub administrator to reissue the API key " +
                        "via the Hub UI (Installations → Reissue Key), then restart this Agent.",
                        options.InstallationCode);
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(ct);
                    logger.LogError("Enrollment failed ({Status}): {Error}", response.StatusCode, err);
                }
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<EnrollmentResult>(cancellationToken: ct);
            if (result is null || string.IsNullOrWhiteSpace(result.ApiKey))
            {
                logger.LogError("Enrollment response was empty or missing ApiKey.");
                return;
            }

            // Apply in-memory after persisting to disk.
            // Persisting first ensures the key survives even if the process crashes between the two steps.
            await PersistEnrollmentAsync(result.ApiKey, result.InstallationId);

            options.ApiKey = result.ApiKey;
            options.InstallationId = result.InstallationId.ToString();

            logger.LogInformation("Enrollment successful. InstallationId={Id} Code={Code}",
                result.InstallationId, options.InstallationCode);
            agentStatus.EnrollmentStatus = "Enrolled";
        }
        catch (Exception ex)
        {
            if (IsConnectionRefused(ex))
                logger.LogWarning(
                    "Hub not reachable at {Url} (connection refused) — enrollment skipped. " +
                    "Make sure Prym.ManagementHub is running.",
                    enrollUrl);
            else
                logger.LogError(ex, "Enrollment request failed.");
        }
    }

    private static readonly string IdentityFilePath =
        Path.Combine(AppContext.BaseDirectory, "agent-identity.json");

    private async Task PersistEnrollmentAsync(string apiKey, Guid installationId)
    {
        try
        {
            // agent-identity.json lives in AppContext.BaseDirectory (build output), never in the
            // project source tree — so VS build never overwrites it on rebuild.
            JsonNode root;
            if (File.Exists(IdentityFilePath))
            {
                var existing = await File.ReadAllTextAsync(IdentityFilePath);
                root = JsonNode.Parse(existing) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var section = root[AgentOptions.SectionName] as JsonObject ?? new JsonObject();
            section["ApiKey"] = apiKey;
            section["InstallationId"] = installationId.ToString();
            root[AgentOptions.SectionName] = section;

            var tmpPath = IdentityFilePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmpPath, IdentityFilePath, overwrite: true);
            logger.LogInformation("Enrollment credentials persisted to agent-identity.json.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist enrollment credentials to agent-identity.json.");
        }
    }

    private int MapComponents()
    {
        var server = options.Components.Server.Enabled;
        var client = options.Components.Client.Enabled;
        var result = (server, client) switch
        {
            (true, true)  => 3,  // Both
            (true, false) => 1,  // Server
            (false, true) => 2,  // Client
            _             => 0   // Neither
        };
        if (result == 0 && !options.StandaloneMode)
            logger.LogWarning(
                "MapComponents: both Server and Client components are disabled and StandaloneMode is false — " +
                "no components will be reported to the Hub.");
        return result;
    }

    private record EnrollmentResult(Guid InstallationId, string InstallationCode, string ApiKey);

    private async Task ConnectAndRunAsync(CancellationToken ct)
    {
        // Dispose any HubConnection left over from a previous failed iteration.
        // Without this, the old connection continues to hold resources and may keep
        // attempting automatic reconnects while the new one is being created.
        var previous = _connection;
        _connection = null;
        if (previous is not null)
            await previous.DisposeAsync();

        // Reset per-connection registration flag so RegisterInstallation is re-sent
        // on every new outer-loop reconnection (not just the very first lifetime start).
        var registeredForThisConnection = false;

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

                // Notify connected clients BEFORE download so the snackbar shows what's incoming.
                var isInWindow = pendingInstallService.IsInMaintenanceWindow();
                await updateExecutor.NotifyPackageReceivedAsync(command, isInWindow);

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
                    await updateExecutor.ReportAsync(command, UpdatePhase.AwaitingMaintenanceWindow, false, true, null, ct);
                    // Notify connected clients that the package is queued so the snackbar updates.
                    await updateExecutor.NotifyAwaitingInstallAsync(command);
                }
                else if (command.Component.Equals("Agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Agent self-update: ALWAYS route through the queue (never direct-install).
                    //
                    // Rationale:
                    //   • The ScheduledInstallWorker processes the queue sequentially — this guarantees
                    //     the Agent install never races with a concurrently running Server/Client install.
                    //   • GetNext() gives Agent packages absolute priority, so even if Server/Client
                    //     packages are already queued, the Agent update goes first.
                    //   • TriggerImmediateInstall bypasses the maintenance-window wait so the Agent
                    //     update runs as soon as any in-progress install finishes.
                    pendingInstallService.Enqueue(command, zipPath);
                    pendingInstallService.TriggerImmediateInstall(command.PackageId);
                    logger.LogInformation(
                        "Agent self-update v{Version} enqueued with priority trigger — will run after any in-progress install.",
                        command.Version);
                    await updateExecutor.ReportAsync(command, UpdatePhase.AwaitingMaintenanceWindow, false, true, null, ct);
                    await updateExecutor.NotifyAwaitingInstallAsync(command);
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
                    }
                    catch (Exception ex)
                    {
                        commandTracking.SetState(command.PackageId, CommandState.Failed, ex.Message);
                        // Block the queue: a failed direct install is just as dangerous for ordered migrations.
                        var downgradedToManual = pendingInstallService.Block(command.PackageId,
                            $"Direct install failed for {command.Component} {command.Version}: {ex.Message}");
                        if (downgradedToManual)
                        {
                            var downgradedCommand = command with { IsManualInstall = true };
                            _ = Task.Run(async () =>
                            {
                                try { await updateExecutor.NotifyAwaitingInstallAsync(downgradedCommand); }
                                catch (Exception notifyEx)
                                {
                                    logger.LogWarning(notifyEx,
                                        "Failed to notify clients of manual-downgrade for {Component} {Version}",
                                        command.Component, command.Version);
                                }
                            });
                        }
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

                    await updateExecutor.ReportAsync(command, UpdatePhase.AwaitingMaintenanceWindow,
                        isCompleted: false, isSuccess: true, errorMessage: null, ct);
                    // Notify connected clients that the package is queued so the snackbar updates.
                    await updateExecutor.NotifyAwaitingInstallAsync(command);
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
                commandTracking.SetState(command.PackageId, CommandState.Installing);
                await updateExecutor.InstallFromZipAsync(pending.Command, pending.LocalZipPath, ct);
                pendingInstallService.Remove(command.PackageId);
                commandTracking.SetState(command.PackageId, CommandState.Installed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "InstallNow failed for {Component} {Version} (PackageId={PackageId})",
                    pending.Command.Component, pending.Command.Version, command.PackageId);
                commandTracking.SetState(command.PackageId, CommandState.Failed, ex.Message);
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
                // Check if this is an Agent self-update.
                var isAgent = msg.Component.Equals("Agent", StringComparison.OrdinalIgnoreCase);

                if (!isAgent)
                {
                    logger.LogWarning("UpdateAvailable: Unknown component '{Component}' — skipping.", msg.Component);
                    commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                    return;
                }

                // Agent self-update: compare offered version with our running version.
                var runningVersion = versionDetector.GetAgentVersion();
                if (!IsNewerVersion(msg.Version, runningVersion))
                {
                    logger.LogInformation(
                        "UpdateAvailable (Agent): Already up-to-date (installed={Installed}, offered={Offered}) — skipping.",
                        runningVersion, msg.Version);
                    commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: false);
                    return;
                }

                logger.LogInformation(
                    "UpdateAvailable (Agent): Newer version detected (offered={Offered} > installed={Installed}). Requesting self-update from Hub.",
                    msg.Version, runningVersion);

                try
                {
                    await _connection.InvokeAsync("RequestStartUpdate", msg.PackageId, ct);
                    commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: true);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogError(ex, "UpdateAvailable (Agent): Failed to invoke RequestStartUpdate on Hub.");
                    commandTracking.TrackNotified(msg.PackageId, msg.Component, msg.Version, msg.ReleaseNotes, wasNewer: true);
                }
                return;
            }

            // Compare the offered version with the currently installed version.
            var installedVersion = isServer
                ? await versionDetector.GetServerVersionAsync()
                : await versionDetector.GetClientVersionAsync();

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
            if (ex is not null)
                logger.LogWarning(ex, "Reconnecting to hub at {Url} — reason: {Message} (inner: {Inner})",
                    options.HubUrl, ex.Message, ex.InnerException?.Message ?? "none");
            else
                logger.LogWarning("Reconnecting to hub at {Url} — reason unknown.", options.HubUrl);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async _ =>
        {
            agentStatus.HubConnectionState = "Connected";
            logger.LogInformation("Reconnected to hub at {Url}.", options.HubUrl);
            // Send a heartbeat so the Hub receives up-to-date state after reconnection
            // without repeating the full registration sequence.
            try { await SendHeartbeatAsync(CancellationToken.None); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send heartbeat after reconnection to {Url}.", options.HubUrl);
            }
        };

        logger.LogInformation("Attempting to connect to Hub at {Url} (InstallationId={Id})",
            options.HubUrl, options.InstallationId);
        try
        {
            await _connection.StartAsync(ct);
        }
        catch (HttpRequestException httpEx)
        {
            var statusCode = httpEx.StatusCode.HasValue
                ? $"{(int)httpEx.StatusCode.Value} {httpEx.StatusCode.Value}"
                : "no HTTP status";
            throw new InvalidOperationException(
                $"Hub connection failed ({statusCode}) at {options.HubUrl} — {httpEx.Message}",
                httpEx);
        }
        agentStatus.HubConnectionState = "Connected";
        logger.LogInformation("Connected to UpdateHub at {Url}", options.HubUrl);

        // Register on first connect — send full identity so Hub gets the complete picture.
        // On subsequent reconnects the Reconnected handler sends only a heartbeat.
        if (!registeredForThisConnection)
        {
            registeredForThisConnection = true;
            await SendRegisterInstallationAsync(ct);
            // After registering, check if a previous self-update is pending completion.
            await ProcessSelfUpdateMarkerAsync(ct);
        }
        else
        {
            // New connection after a full reconnect cycle: send heartbeat so Hub has fresh state.
            await SendHeartbeatAsync(ct);
        }

        // Heartbeat loop
        while (!ct.IsCancellationRequested && _connection.State == HubConnectionState.Connected)
        {
            await Task.Delay(TimeSpan.FromSeconds(options.HeartbeatIntervalSeconds), ct);

            // Reconnect requested from UI (HubUrl/ApiKey changed) — break to trigger new connection.
            if (agentStatus.ConsumeReconnectRequest())
            {
                logger.LogInformation("Reconnect requested — dropping connection to apply updated settings.");
                break;
            }

            // Re-registration requested from UI (name/location/tags changed) — send full identity.
            if (agentStatus.ConsumeReRegisterRequest())
                await SendRegisterInstallationAsync(ct);
            else
                await SendHeartbeatAsync(ct);
        }
    }

    private async Task SendRegisterInstallationAsync(CancellationToken ct)
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        var serverVerTask = versionDetector.GetServerVersionAsync();
        var clientVerTask = versionDetector.GetClientVersionAsync();
        await Task.WhenAll(serverVerTask, clientVerTask, systemInfo.GetPublicIpAddressAsync());

        agentStatus.LastKnownServerVersion = serverVerTask.Result ?? agentStatus.LastKnownServerVersion;
        agentStatus.LastKnownClientVersion = clientVerTask.Result ?? agentStatus.LastKnownClientVersion;

        await _connection.InvokeAsync("RegisterInstallation", new RegisterInstallationMessage(
            InstallationId:   options.InstallationId,
            InstallationName: options.InstallationName,
            VersionServer:    serverVerTask.Result,
            VersionClient:    clientVerTask.Result,
            Components:       new InstallationComponentsDto(
                                  options.Components.Server.Enabled,
                                  options.Components.Client.Enabled),
            InstallationCode: options.InstallationCode,
            Location:         options.Location,
            Tags:             options.Tags.Count > 0 ? options.Tags : null,
            MachineName:      systemInfo.MachineName,
            OSVersion:        systemInfo.OSVersion,
            DotNetVersion:    systemInfo.DotNetVersion,
            AgentVersion:     versionDetector.GetAgentVersion(),
            LocalIpAddress:   systemInfo.LocalIpAddress,
            PublicIpAddress:  await systemInfo.GetPublicIpAddressAsync()),
            ct);
    }

    private async Task SendHeartbeatAsync(CancellationToken ct)
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        var serverVerTask  = versionDetector.GetServerVersionAsync();
        var clientVerTask  = versionDetector.GetClientVersionAsync();
        var publicIpTask   = systemInfo.GetPublicIpAddressAsync();
        await Task.WhenAll(serverVerTask, clientVerTask, publicIpTask);

        agentStatus.LastKnownServerVersion = serverVerTask.Result ?? agentStatus.LastKnownServerVersion;
        agentStatus.LastKnownClientVersion = clientVerTask.Result ?? agentStatus.LastKnownClientVersion;

        await _connection.InvokeAsync("Heartbeat", new HeartbeatMessage(
            InstallationId:  options.InstallationId,
            VersionServer:   serverVerTask.Result,
            VersionClient:   clientVerTask.Result,
            Status:          "Online",
            Timestamp:       DateTime.UtcNow,
            AgentVersion:    versionDetector.GetAgentVersion(),
            Location:        options.Location,
            Tags:            options.Tags.Count > 0 ? options.Tags : null,
            PublicIpAddress: publicIpTask.Result),
            ct);

        agentStatus.LastHeartbeatAt = DateTime.UtcNow;
        agentStatus.LastHeartbeatError = null;
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

    /// <summary>
    /// Checks for a <c>self-update.json</c> marker written by a previous instance just before
    /// it stopped itself to allow the external Updater to replace its binaries.
    /// Reports the outcome (success/failure) to the Hub via <c>ReportUpdateProgress</c>,
    /// then deletes the marker.
    /// </summary>
    private async Task ProcessSelfUpdateMarkerAsync(CancellationToken ct)
    {
        var markerPath = Path.Combine(AppContext.BaseDirectory, "self-update.json");
        if (!File.Exists(markerPath)) return;

        SelfUpdateMarker? marker = null;
        try
        {
            var json = await File.ReadAllTextAsync(markerPath, ct);
            marker = JsonSerializer.Deserialize<SelfUpdateMarker>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read self-update marker — deleting it to avoid repeat processing.");
        }
        finally
        {
            // Always delete the marker so we don't reprocess it on the next restart.
            try { File.Delete(markerPath); } catch (Exception ex) { logger.LogWarning(ex, "Could not delete self-update marker at '{Path}'.", markerPath); }
        }

        if (marker is null) return;

        // Determine success by comparing the running assembly version with the expected version.
        var runningVersion = versionDetector.GetAgentVersion();
        var plusIdx = runningVersion.IndexOf('+');
        var runningSemVer = plusIdx >= 0 ? runningVersion[..plusIdx] : runningVersion;
        var isSuccess = string.Equals(runningSemVer, marker.NewVersion, StringComparison.OrdinalIgnoreCase);

        logger.LogInformation(
            "Self-update marker found: expected v{Expected}, running v{Running} — success={Success}",
            marker.NewVersion, runningSemVer, isSuccess);

        if (_connection?.State != HubConnectionState.Connected) return;

        try
        {
            var progressMsg = new UpdateProgressMessage(
                options.InstallationId,
                marker.HistoryId,
                UpdatePhase.Completed.ToString(),
                IsCompleted: true,
                IsSuccess:   isSuccess,
                ErrorMessage: isSuccess
                    ? null
                    : $"Versione Agent dopo self-update non corrispondente: attesa {marker.NewVersion}, in esecuzione {runningSemVer}");

            await updateExecutor.ReportAsync(progressMsg);

            if (isSuccess)
                versionDetector.InvalidateVersionCache();

            logger.LogInformation("Self-update completion reported to Hub (HistoryId={HistoryId} Success={Success}).",
                marker.HistoryId, isSuccess);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to report self-update completion to Hub for HistoryId={HistoryId}.", marker.HistoryId);
        }
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

    /// <summary>
    /// Returns true when <paramref name="ex"/> (or any inner exception) is a TCP
    /// connection-refused / host-unreachable error — i.e. the ManagementHub is simply not running.
    /// </summary>
    private static bool IsConnectionRefused(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is System.Net.Sockets.SocketException se &&
                (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.NetworkUnreachable ||
                 se.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut))
                return true;
            ex = ex.InnerException;
        }
        return false;
    }
}
