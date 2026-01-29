using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using EventForge.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Implementation of server status service.
/// </summary>
public class ServerStatusService : IServerStatusService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerStatusService> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public ServerStatusService(
        EventForgeDbContext dbContext,
        IConfiguration configuration,
        ILogger<ServerStatusService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new ServerStatus
        {
            Uptime = DateTime.UtcNow - _startTime,
            MachineName = Environment.MachineName,
            OperatingSystem = GetOperatingSystem(),
            RuntimeVersion = Environment.Version.ToString(),
            CpuCores = Environment.ProcessorCount,
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
        };

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        status.Version = version?.ToString() ?? "1.0.0";

        try
        {
            var process = Process.GetCurrentProcess();
            status.UsedMemoryMB = process.WorkingSet64 / 1024 / 1024;
            
            var gcInfo = GC.GetGCMemoryInfo();
            status.TotalMemoryMB = gcInfo.TotalAvailableMemoryBytes / 1024 / 1024;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory information");
        }

        try
        {
            status.DatabaseConnected = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check database connection");
            status.DatabaseConnected = false;
        }

        try
        {
            var maintenanceMode = await _dbContext.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == "System.MaintenanceMode", cancellationToken);
            
            if (maintenanceMode != null && maintenanceMode.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                status.Status = "Maintenance";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check maintenance mode");
        }

        try
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            status.ActiveUsers = await _dbContext.Users
                .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value > fiveMinutesAgo)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to count active users");
        }

        try
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentLogs = await _dbContext.SystemOperationLogs
                .Where(l => l.CreatedAt > oneMinuteAgo)
                .CountAsync(cancellationToken);
            
            status.RequestsPerMinute = recentLogs;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to calculate requests per minute");
        }

        status.CacheType = "Memory";

        return status;
    }

    private string GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"Windows {Environment.OSVersion.Version}";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"Linux {Environment.OSVersion.Version}";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"macOS {Environment.OSVersion.Version}";

        return "Unknown";
    }
}
