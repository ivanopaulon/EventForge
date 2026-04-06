using EventForge.UpdateAgent.Middleware;
using EventForge.UpdateAgent.Workers;
using Microsoft.Extensions.Options;
using Serilog;

// ── Read config early for Serilog + URL binding ───────────────────────────
var earlyConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var earlyAgent = earlyConfig.GetSection(AgentOptions.SectionName).Get<AgentOptions>() ?? new AgentOptions();

var logDir = !string.IsNullOrWhiteSpace(earlyAgent.Logging.DirectoryPath)
    ? earlyAgent.Logging.DirectoryPath
    : Path.Combine(AppContext.BaseDirectory, "logs");

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

    // ── Windows Service support ───────────────────────────────────────────
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "EventForge Update Agent";
    });

    // ── Serilog ───────────────────────────────────────────────────────────
    builder.Host.UseSerilog();

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

    // ── Background workers ────────────────────────────────────────────────
    builder.Services.AddHostedService<AgentWorker>();
    builder.Services.AddHostedService<ScheduledInstallWorker>();

    // ── Web UI ────────────────────────────────────────────────────────────
    builder.Services.AddRazorPages();
    builder.Services.AddAntiforgery();

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

    // ── Minimal API endpoints ─────────────────────────────────────────────
    // Hub connection + download status (used by sidebar.js and dashboard polling)
    app.MapGet("/api/agent/status", (AgentStatusService svc) =>
        Results.Ok(new { svc.HubConnectionState, svc.LastHeartbeatAt }));

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

    Log.Information("EventForge Update Agent starting. UI at http://localhost:{Port}", earlyAgent.UI.Port);

    // ── Generate InstallationCode on first startup (before workers start) ──
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<InstallationCodeGenerator>().EnsureInstallationCode();
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
