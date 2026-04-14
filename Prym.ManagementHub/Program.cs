using Prym.ManagementHub.Auth;
using Prym.ManagementHub.Configuration;
using Prym.ManagementHub.Hubs;
using Prym.ManagementHub.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

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

// ── ManagementHubOptions ──
var hubOptions = builder.Configuration
    .GetSection(ManagementHubOptions.SectionName)
    .Get<ManagementHubOptions>() ?? new ManagementHubOptions();
builder.Services.AddSingleton(hubOptions);

var logDir = !string.IsNullOrWhiteSpace(hubOptions.Logging.DirectoryPath)
    ? hubOptions.Logging.DirectoryPath
    : Path.Combine(AppContext.BaseDirectory, "logs");

// Ensure the log directory exists before Serilog tries to write to it.
Directory.CreateDirectory(logDir);

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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings so downstream consumers (EventForge.Server proxy) can
        // deserialize them into string properties of PackageSummaryDto / InstallationSummaryDto.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Prym ManagementHub API", Version = "v1" }));

builder.Services.AddDbContext<ManagementHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ManagementHub")
        ?? "Data Source=updatehub.db"));

builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IPackageBuildService, PackageBuildService>();
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();
builder.Services.AddSingleton<IUpdateThrottleService, UpdateThrottleService>();
builder.Services.AddHostedService<PackageWatcherService>();
builder.Services.AddHostedService<AgentStatusCheckService>();
builder.Services.AddHostedService<PackageCleanupService>();

// Rate limiting: protect the self-enrollment endpoint from brute-force token guessing.
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("enrollment", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ── Startup validation (folders + config checks) ──────────────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
StartupValidator.Run(hubOptions, startupLogger);

// ── Database migration ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ManagementHubDbContext>();
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
app.UseRateLimiter();
app.MapRazorPages();
app.MapControllers();
app.MapHub<AgentHub>("/hubs/update");

Log.Information("Prym ManagementHub starting. Endpoints: {Urls}",
    urls.Count > 0 ? string.Join(", ", urls) : "(managed by IIS/reverse-proxy)");

app.Run();
