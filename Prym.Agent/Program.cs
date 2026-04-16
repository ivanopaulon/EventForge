using Prym.Agent.Middleware;
using Prym.Agent.Workers;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json.Nodes;

// ── Read config early for Serilog + URL binding ───────────────────────────
// Single-file config: base values from appsettings.json, then override with
// "Environments:{env}" section — mirrors the same pattern used by Prym.Web.
var earlyEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var baseConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var earlyEnvSection = baseConfig.GetSection($"Environments:{earlyEnv}");
var earlyConfig = earlyEnvSection.Exists()
    ? new ConfigurationBuilder()
        .AddConfiguration(baseConfig)
        .AddInMemoryCollection(
            earlyEnvSection.AsEnumerable(makePathsRelative: true)
                           .Where(kvp => kvp.Value is not null)
                           .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)))
        .Build()
    : baseConfig;

var earlyAgent = earlyConfig.GetSection(AgentOptions.SectionName).Get<AgentOptions>() ?? new AgentOptions();

var logDir = !string.IsNullOrWhiteSpace(earlyAgent.Logging.DirectoryPath)
    ? earlyAgent.Logging.DirectoryPath
    : Path.Combine(AppContext.BaseDirectory, "logs");

// Ensure the log directory exists before Serilog tries to write to it.
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(earlyConfig)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDir, "agent-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: earlyAgent.Logging.RetentionDays)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Apply environment-specific overrides from "Environments:{env}" in appsettings.json ──
    var envSection = builder.Configuration.GetSection($"Environments:{builder.Environment.EnvironmentName}");
    if (envSection.Exists())
    {
        builder.Configuration.AddInMemoryCollection(
            envSection.AsEnumerable(makePathsRelative: true)
                      .Where(kvp => kvp.Value is not null)
                      .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
    }

    // ── Windows Service support ───────────────────────────────────────────
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "Prym Agent";
    });

    // ── Serilog ───────────────────────────────────────────────────────────
    // Use the delegate overload so the Server ingest sink can receive the
    // IHttpClientFactory from DI once all services have been registered.
    builder.Host.UseSerilog((_, services, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDir, "agent-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: earlyAgent.Logging.RetentionDays);

        // Conditionally forward logs to EventForge.Server
        var opts = services.GetRequiredService<AgentOptions>();
        if (opts.Logging.ServerIngestEnabled)
        {
            var serverBase = !string.IsNullOrWhiteSpace(opts.Logging.ServerIngestUrl)
                ? opts.Logging.ServerIngestUrl
                : opts.Components.Server.NotificationBaseUrl;

            if (!string.IsNullOrWhiteSpace(serverBase))
                {
                    loggerConfig.WriteTo.Sink(new AgentServerSink(opts));
                }
        }
    });

    // ── Options ───────────────────────────────────────────────────────────
    builder.Services.Configure<AgentOptions>(
        builder.Configuration.GetSection(AgentOptions.SectionName));
    builder.Services.AddSingleton(sp =>
        sp.GetRequiredService<IOptions<AgentOptions>>().Value);

    // ── Core services ─────────────────────────────────────────────────────
    builder.Services.AddSingleton<InstallationCodeGenerator>();
    builder.Services.AddSingleton<SystemInfoService>();
    builder.Services.AddSingleton<AgentStatusService>();
    builder.Services.AddSingleton<DownloadProgressService>();
    builder.Services.AddSingleton<PendingInstallService>();
    builder.Services.AddSingleton<VersionDetectorService>();
    builder.Services.AddSingleton<BackupService>();
    builder.Services.AddSingleton<IisManagerService>();
    builder.Services.AddSingleton<MigrationRunnerService>();
    builder.Services.AddSingleton<UpdateExecutorService>();
    builder.Services.AddSingleton<CommandTrackingService>();

    // ── Printer proxy ─────────────────────────────────────────────────────
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<IAgentPrinterService, AgentPrinterService>();

    // ── Background workers ────────────────────────────────────────────────
    builder.Services.AddHostedService<AgentWorker>();
    builder.Services.AddHostedService<ScheduledInstallWorker>();

    // ── Web UI ────────────────────────────────────────────────────────────
    builder.Services.AddRazorPages();
    builder.Services.AddAntiforgery();
    builder.Services.AddControllers();

    // Bind only to localhost on the configured port
    builder.WebHost.UseUrls($"http://localhost:{earlyAgent.UI.Port}");

    var app = builder.Build();

    // ── Basic Auth middleware ─────────────────────────────────────────────
    app.UseMiddleware<BasicAuthMiddleware>();

    // ── Static files + Razor Pages ────────────────────────────────────────
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAntiforgery();
    app.MapRazorPages();
    app.MapControllers();

    // ── Minimal API endpoints ─────────────────────────────────────────────
    // Hub connection + download status (used by sidebar.js and dashboard polling)
    app.MapGet("/api/agent/status", (AgentStatusService svc) =>
        Results.Ok(new { svc.HubConnectionState, svc.LastHeartbeatAt }));

    // Lightweight unauthenticated health probe — used by EventForge.Server to include
    // Agent status in its own /api/v1/health/detailed response. Safe because the Agent
    // binds to localhost only; external requests cannot reach this endpoint.
    app.MapGet("/api/agent/health", async (AgentStatusService svc, AgentOptions opts, VersionDetectorService versionDetector) =>
    {
        var serverVerTask = versionDetector.GetServerVersionAsync();
        var clientVerTask = versionDetector.GetClientVersionAsync();
        await Task.WhenAll(serverVerTask, clientVerTask);
        return Results.Ok(new
        {
            Status = "Online",
            InstallationName = opts.InstallationName,
            AgentVersion = versionDetector.GetAgentVersion(),
            ServerVersion = serverVerTask.Result,
            ClientVersion = clientVerTask.Result,
            svc.HubConnectionState,
            svc.LastHeartbeatAt,
            ProbeTime = DateTime.UtcNow
        });
    });

    app.MapGet("/api/agent/download-status", (DownloadProgressService svc) =>
    {
        var snap = svc.Current;
        if (snap is null) return Results.Ok((object?)null);
        return Results.Ok(new
        {
            snap.PackageId,
            snap.Component,
            snap.Version,
            snap.BytesDownloaded,
            snap.TotalBytes,
            snap.PercentComplete,
            snap.FormattedDownloaded,
            snap.FormattedTotal,
            snap.FormattedSpeed,
            Eta = snap.Eta?.ToString(@"hh\:mm\:ss")
        });
    });

    // ── Update queue management (called by EventForge.Server agent-proxy) ────
    // These endpoints are localhost-only and unauthenticated (same trust model
    // as the health endpoint above — the Agent binds to localhost only).

    app.MapGet("/api/agent/pending-installs", (
        PendingInstallService pendingSvc,
        AgentOptions opts) =>
    {
        var all = pendingSvc.GetAll();
        var headId = pendingSvc.GetNext()?.PackageId;
        return Results.Ok(all.Select(p => new
        {
            InstallationId   = opts.InstallationId,
            InstallationName = opts.InstallationName,
            p.PackageId,
            Component        = p.Command.Component,
            Version          = p.Command.Version,
            p.IsManualInstall,
            p.QueuedAt,
            IsQueueHead      = p.PackageId == headId,
            pendingSvc.IsBlocked,
            BlockedReason    = pendingSvc.IsBlocked ? pendingSvc.BlockedReason : null,
            FileExists       = File.Exists(p.LocalZipPath)
        }));
    });

    app.MapPost("/api/agent/install-now", (
        AgentInstallNowRequest req,
        PendingInstallService pendingSvc) =>
    {
        var pending = pendingSvc.GetByPackageId(req.PackageId);
        if (pending is null)
            return Results.NotFound(new { Error = "Package not found in queue." });

        var head = pendingSvc.GetNext();
        if (head?.PackageId != req.PackageId)
            return Results.Conflict(new { Error = "Package is not head of queue — install must be sequential." });

        if (pendingSvc.IsBlocked)
            return Results.Conflict(new { Error = $"Queue is blocked: {pendingSvc.BlockedReason}" });

        if (!File.Exists(pending.LocalZipPath))
            return Results.UnprocessableEntity(new { Error = "Package zip file not found on disk." });

        if (!pendingSvc.TriggerImmediateInstall(req.PackageId))
            return Results.Conflict(new { Error = "An install trigger is already pending." });

        return Results.Accepted();
    });

    app.MapPost("/api/agent/unblock-queue", (
        AgentUnblockQueueRequest req,
        PendingInstallService pendingSvc) =>
    {
        pendingSvc.Unblock(req.SkipAndRemove);
        return Results.Ok(new { Unblocked = true, req.SkipAndRemove });
    });

    Log.Information("Prym Agent starting. UI at http://localhost:{Port}", earlyAgent.UI.Port);

    // ── Restore persisted identity (survives project rebuilds) ───────────────
    // agent-identity.json lives in AppContext.BaseDirectory (build output dir), NOT in the
    // project source dir — VS build never touches it. Stores InstallationCode, ApiKey,
    // InstallationId so the agent keeps its identity across rebuilds and restarts.
    var agentOpts = app.Services.GetRequiredService<AgentOptions>();
    var identityPath = Path.Combine(AppContext.BaseDirectory, "agent-identity.json");
    if (File.Exists(identityPath))
    {
        try
        {
            var idSection = (JsonNode.Parse(File.ReadAllText(identityPath))
                ?[AgentOptions.SectionName]) as JsonObject;
            if (idSection is not null)
            {
                if (idSection["InstallationCode"]?.GetValue<string>() is { Length: > 0 } code)
                    agentOpts.InstallationCode = code;
                if (idSection["ApiKey"]?.GetValue<string>() is { Length: > 0 } key)
                    agentOpts.ApiKey = key;
                if (idSection["InstallationId"]?.GetValue<string>() is { Length: > 0 } installId
                    && installId != "00000000-0000-0000-0000-000000000000")
                    agentOpts.InstallationId = installId;
                Log.Debug("Agent identity loaded from agent-identity.json. Code={Code}", agentOpts.InstallationCode);
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Could not load agent-identity.json — starting with empty identity."); }
    }

    // ── Startup validation + InstallationCode generation ─────────────────
    // Both steps use the same DI scope — no need to create two separate scopes.
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        StartupValidator.Run(sp.GetRequiredService<AgentOptions>(), sp.GetRequiredService<ILogger<Program>>());
        sp.GetRequiredService<InstallationCodeGenerator>().EnsureInstallationCode();
    }

    // ── Cleanup stale Updater temp directories from previous self-updates ──
    // The Updater is launched from a copy in %TEMP%/prym-upd-{PackageId}/ so the original
    // binary in the install directory can be overwritten while the Updater runs.
    // The Agent stops immediately after launching the Updater, so cleanup must happen here.
    try
    {
        foreach (var dir in Directory.EnumerateDirectories(Path.GetTempPath(), "prym-upd-*"))
        {
            try
            {
                Directory.Delete(dir, recursive: true);
                Log.Debug("Deleted stale Updater temp directory: {Dir}", dir);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not delete stale Updater temp directory: {Dir}", dir);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error enumerating prym-upd-* temp directories for cleanup.");
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Agent terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public record AgentInstallNowRequest(Guid PackageId);
public record AgentUnblockQueueRequest(Guid PackageId, bool SkipAndRemove);
