using EventForge.UpdateHub.Auth;
using EventForge.UpdateHub.Configuration;
using EventForge.UpdateHub.Hubs;
using EventForge.UpdateHub.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Apply environment-specific overrides from "Environments:{env}" in appsettings.json ──
// Mirrors the same single-file pattern used by EventForge.Client.
var envSection = builder.Configuration.GetSection($"Environments:{builder.Environment.EnvironmentName}");
if (envSection.Exists())
{
    builder.Configuration.AddInMemoryCollection(
        envSection.AsEnumerable(makePathsRelative: true)
                  .Where(kvp => kvp.Value is not null)
                  .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
}

// ── UpdateHubOptions ──
var hubOptions = builder.Configuration
    .GetSection(UpdateHubOptions.SectionName)
    .Get<UpdateHubOptions>() ?? new UpdateHubOptions();
builder.Services.AddSingleton(hubOptions);

var logDir = !string.IsNullOrWhiteSpace(hubOptions.Logging.DirectoryPath)
    ? hubOptions.Logging.DirectoryPath
    : Path.Combine(AppContext.BaseDirectory, "logs");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDir, "hub-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: hubOptions.Logging.RetentionDays)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Kestrel endpoint binding (standalone / non-IIS) ──────────────────────
// Under IIS in-process, UseUrls is overridden by the IIS module — safe to call unconditionally.
var urls = new List<string>();
if (hubOptions.UI.HttpPort > 0)  urls.Add($"http://*:{hubOptions.UI.HttpPort}");
if (hubOptions.UI.HttpsPort > 0) urls.Add($"https://*:{hubOptions.UI.HttpsPort}");
if (urls.Count > 0) builder.WebHost.UseUrls([.. urls]);

// ── Services ──
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "EventForge UpdateHub API", Version = "v1" }));

builder.Services.AddDbContext<UpdateHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("UpdateHub")
        ?? "Data Source=updatehub.db"));

builder.Services.AddSignalR();
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IPackageBuildService, PackageBuildService>();
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
builder.Services.AddHostedService<PackageWatcherService>();

var app = builder.Build();

// ── Database migration ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UpdateHubDbContext>();
    db.Database.EnsureCreated();
    db.EnsureSchemaUpToDate(); // adds new columns to existing databases
}

// ── Middleware ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic auth protects Razor Pages UI; API/SignalR endpoints use their own auth
app.UseMiddleware<HubBasicAuthMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHub<AgentHub>("/hubs/update");

Log.Information("EventForge UpdateHub starting. Endpoints: {Urls}",
    urls.Count > 0 ? string.Join(", ", urls) : "(managed by IIS/reverse-proxy)");

app.Run();
