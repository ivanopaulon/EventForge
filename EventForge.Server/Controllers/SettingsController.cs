using EventForge.DTOs.Settings;
using EventForge.Server.Auth;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using SystemFile = System.IO.File;

namespace EventForge.Server.Controllers;

/// <summary>
/// API controller for advanced settings management.
/// Requires SuperAdmin role for all operations.
/// </summary>
[Route("api/v1/settings")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireSuperAdmin)]
public class SettingsController : BaseApiController
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<SettingsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IWebHostEnvironment _environment;

    public SettingsController(
        EventForgeDbContext context,
        ILogger<SettingsController> logger,
        IConfiguration configuration,
        IHostApplicationLifetime appLifetime,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _appLifetime = appLifetime;
        _environment = environment;
    }

    /// <summary>
    /// Gets all configuration settings with versioning information.
    /// </summary>
    [HttpGet("configuration")]
    public async Task<ActionResult<List<ConfigurationValueDto>>> GetAllConfigurations()
    {
        var configs = await _context.SystemConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync();

        var dtos = configs.Select(c => new ConfigurationValueDto
        {
            Id = c.Id,
            Key = c.Key,
            Value = c.IsEncrypted ? "********" : c.Value,
            DefaultValue = c.DefaultValue,
            Category = c.Category,
            Description = c.Description,
            Version = c.Version,
            IsActive = c.IsActive,
            RequiresRestart = c.RequiresRestart,
            IsEncrypted = c.IsEncrypted,
            IsReadOnly = c.IsReadOnly,
            Source = ConfigurationSource.Database,
            CreatedAt = c.CreatedAt,
            CreatedBy = c.CreatedBy ?? "System",
            ModifiedAt = c.ModifiedAt,
            ModifiedBy = c.ModifiedBy
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific configuration setting by key.
    /// </summary>
    [HttpGet("configuration/{key}")]
    public async Task<ActionResult<ConfigurationValueDto>> GetConfiguration(string key)
    {
        var config = await _context.SystemConfigurations
            .Where(c => c.Key == key && c.IsActive)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync();

        if (config == null)
            return NotFound($"Configuration key '{key}' not found.");

        var dto = new ConfigurationValueDto
        {
            Id = config.Id,
            Key = config.Key,
            Value = config.IsEncrypted ? "********" : config.Value,
            DefaultValue = config.DefaultValue,
            Category = config.Category,
            Description = config.Description,
            Version = config.Version,
            IsActive = config.IsActive,
            RequiresRestart = config.RequiresRestart,
            IsEncrypted = config.IsEncrypted,
            IsReadOnly = config.IsReadOnly,
            Source = ConfigurationSource.Database,
            CreatedAt = config.CreatedAt,
            CreatedBy = config.CreatedBy ?? "System",
            ModifiedAt = config.ModifiedAt,
            ModifiedBy = config.ModifiedBy
        };

        return Ok(dto);
    }

    /// <summary>
    /// Gets database connection status.
    /// </summary>
    [HttpGet("database/status")]
    public async Task<ActionResult<DatabaseConnectionStatusDto>> GetDatabaseStatus()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            var provider = _configuration["DatabaseProvider"] ?? "SqlServer";
            
            return Ok(new DatabaseConnectionStatusDto
            {
                IsConnected = true,
                Provider = provider,
                DatabaseName = _context.Database.GetDbConnection().Database,
                ConnectionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                CheckedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return Ok(new DatabaseConnectionStatusDto
            {
                IsConnected = false,
                Provider = _configuration["DatabaseProvider"] ?? "Unknown",
                ConnectionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                ErrorMessage = ex.Message,
                CheckedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Gets server restart status.
    /// </summary>
    [HttpGet("server/restart/status")]
    public ActionResult<RestartStatusDto> GetRestartStatus()
    {
        var pendingChanges = new List<string>();
        
        // Check for pending configuration changes that require restart
        // This is a simplified version - in production you'd track this in the database
        
        var environment = DetectEnvironment();
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        return Ok(new RestartStatusDto
        {
            RestartRequired = pendingChanges.Any(),
            PendingChanges = pendingChanges,
            Environment = environment,
            Uptime = uptime
        });
    }

    /// <summary>
    /// Gets system operation logs (audit trail).
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult<List<SystemOperationLogDto>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _context.SystemOperationLogs
            .OrderByDescending(l => l.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = logs.Select(l => new SystemOperationLogDto
        {
            Id = l.Id,
            OperationType = l.OperationType,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Action = l.Action,
            Description = l.Description,
            OldValue = l.OldValue,
            NewValue = l.NewValue,
            Details = l.Details,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            ExecutedAt = l.ExecutedAt,
            ExecutedBy = l.ExecutedBy,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Detects the hosting environment (IIS, Kestrel, Docker).
    /// </summary>
    private RestartEnvironment DetectEnvironment()
    {
        // Check if running under IIS
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH")))
            return RestartEnvironment.IIS;

        // Check if running in Docker
        if (SystemFile.Exists("/.dockerenv"))
            return RestartEnvironment.Docker;

        // Default to Kestrel
        return RestartEnvironment.Kestrel;
    }
}
