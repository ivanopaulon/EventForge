using EventForge.UpdateAgent.Workers;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "EventForge Update Agent";
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "agent-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Services.AddSerilog();

// Options
builder.Services.Configure<AgentOptions>(
    builder.Configuration.GetSection(AgentOptions.SectionName));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<AgentOptions>>().Value);

// Services
builder.Services.AddSingleton<VersionDetectorService>();
builder.Services.AddSingleton<BackupService>();
builder.Services.AddSingleton<IisManagerService>();
builder.Services.AddSingleton<MigrationRunnerService>();
builder.Services.AddSingleton<UpdateExecutorService>();
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
await host.RunAsync();
