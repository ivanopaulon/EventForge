using EventForge.UpdateHub.Auth;
using EventForge.UpdateHub.Hubs;
using EventForge.UpdateHub.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

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
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
builder.Services.AddHostedService<PackageWatcherService>();

var app = builder.Build();

// ── Database migration ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UpdateHubDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapHub<AgentHub>("/hubs/update");

app.Run();
