using EventForge.DTOs.Settings;
using EventForge.Server.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
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
    /// Gets current configuration from appsettings.json file.
    /// </summary>
    [HttpGet("file")]
    public IActionResult GetCurrentConfigurationFromFile()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                            ?? _configuration.GetConnectionString("SqlServer");

        if (string.IsNullOrEmpty(connectionString))
        {
            return Ok(new { Configured = false });
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            return Ok(new
            {
                Configured = true,
                Database = new
                {
                    ServerAddress = builder.DataSource,
                    DatabaseName = builder.InitialCatalog,
                    AuthenticationType = builder.IntegratedSecurity ? "Windows" : "SQL",
                    Username = builder.IntegratedSecurity ? null : builder.UserID,
                    TrustServerCertificate = builder.TrustServerCertificate
                    // Password NOT returned for security
                },
                Jwt = new
                {
                    Issuer = _configuration["Authentication:Jwt:Issuer"],
                    Audience = _configuration["Authentication:Jwt:Audience"],
                    ExpirationMinutes = _configuration.GetValue<int>("Authentication:Jwt:ExpirationMinutes", 60)
                    // SecretKey NOT returned for security
                },
                Security = new
                {
                    EnforceHttps = _configuration.GetValue<bool>("Security:EnforceHttps", true),
                    EnableHsts = _configuration.GetValue<bool>("Security:EnableHsts", true)
                },
                FilePath = Path.Combine(_environment.ContentRootPath, "appsettings.json")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse connection string");
            return StatusCode(500, "Failed to parse connection string");
        }
    }

    /// <summary>
    /// Tests database connection without saving.
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestDatabaseConnection([FromBody] TestConnectionRequest request)
    {
        try
        {
            var connectionString = BuildConnectionString(request);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Ok(new
            {
                Success = true,
                Message = "Connection successful",
                ServerVersion = version?.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed");
            return BadRequest(new
            {
                Success = false,
                Error = "Connection failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Saves configuration to appsettings.json file.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> SaveConfigurationToFile([FromBody] SaveConfigurationRequest request)
    {
        try
        {
            // 1. Validate
            if (string.IsNullOrEmpty(request.ServerAddress) || string.IsNullOrEmpty(request.DatabaseName))
            {
                return BadRequest("Server address and database name are required");
            }

            // 2. Build and test new connection string
            var newConnectionString = BuildConnectionString(new TestConnectionRequest
            {
                ServerAddress = request.ServerAddress,
                DatabaseName = request.DatabaseName,
                AuthenticationType = request.AuthenticationType,
                Username = request.Username,
                Password = request.Password,
                TrustServerCertificate = request.TrustServerCertificate
            });

            using var testConn = new SqlConnection(newConnectionString);
            await testConn.OpenAsync();
            await testConn.CloseAsync();

            // 3. Read current appsettings.json
            var appsettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");

            if (!SystemFile.Exists(appsettingsPath))
            {
                return StatusCode(500, "appsettings.json not found");
            }

            var currentJson = await SystemFile.ReadAllTextAsync(appsettingsPath);

            // 4. Parse and update configuration
            using var doc = JsonDocument.Parse(currentJson);
            var root = doc.RootElement;

            var updatedConfig = new Dictionary<string, object>();

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "ConnectionStrings")
                {
                    // Update ConnectionStrings
                    var existingCs = property.Value;

                    // Derive LogDb connection string properly using SqlConnectionStringBuilder
                    string logDb;
                    if (existingCs.TryGetProperty("LogDb", out var logDbProp))
                    {
                        logDb = logDbProp.GetString() ?? newConnectionString;
                    }
                    else
                    {
                        // Create LogDb connection string with different database name
                        var logDbBuilder = new SqlConnectionStringBuilder(newConnectionString);
                        logDbBuilder.InitialCatalog = "EventLogger";
                        logDb = logDbBuilder.ConnectionString;
                    }

                    var redis = existingCs.TryGetProperty("Redis", out var redisProp)
                        ? redisProp.GetString()
                        : "localhost:6379";

                    updatedConfig["ConnectionStrings"] = new
                    {
                        DefaultConnection = newConnectionString,
                        SqlServer = newConnectionString,
                        LogDb = logDb,
                        Redis = redis
                    };
                }
                else if (property.Name == "Authentication" && !string.IsNullOrEmpty(request.JwtSecretKey))
                {
                    // Update JWT config if provided
                    var existingAuth = property.Value;
                    var existingJwt = existingAuth.TryGetProperty("Jwt", out var jwtProp)
                        ? jwtProp
                        : new JsonElement();

                    updatedConfig["Authentication"] = new
                    {
                        Jwt = new
                        {
                            Issuer = "EventForge",
                            Audience = "EventForge",
                            SecretKey = request.JwtSecretKey,
                            ExpirationMinutes = request.JwtExpirationMinutes > 0 ? request.JwtExpirationMinutes : 60,
                            ClockSkewMinutes = 5
                        },
                        PasswordPolicy = existingAuth.TryGetProperty("PasswordPolicy", out var pp)
                            ? JsonSerializer.Deserialize<object>(pp.GetRawText())
                            : null,
                        AccountLockout = existingAuth.TryGetProperty("AccountLockout", out var al)
                            ? JsonSerializer.Deserialize<object>(al.GetRawText())
                            : null
                    };
                }
                else if (property.Name == "Security")
                {
                    updatedConfig["Security"] = new
                    {
                        EnforceHttps = request.EnforceHttps,
                        EnableHsts = request.EnableHsts,
                        HstsMaxAge = 31536000
                    };
                }
                else
                {
                    // Preserve all other sections unchanged
                    updatedConfig[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
                }
            }

            // 5. Create backup
            var backupPath = $"{appsettingsPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
            await SystemFile.WriteAllTextAsync(backupPath, currentJson);
            _logger.LogInformation("Configuration backup created at {Path}", backupPath);

            // 6. Write updated configuration
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = JsonSerializer.Serialize(updatedConfig, options);
            await SystemFile.WriteAllTextAsync(appsettingsPath, newJson);

            _logger.LogWarning("Configuration updated by {User}. Server restart required.",
                User.Identity?.Name ?? "unknown");

            return Ok(new
            {
                Success = true,
                Message = "Configuration saved successfully. Server restart required.",
                BackupPath = backupPath,
                RestartRequired = true
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database connection test failed during save");
            return BadRequest(new
            {
                Success = false,
                Error = "Database connection failed",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            return StatusCode(500, new
            {
                Success = false,
                Error = "Failed to save configuration",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Triggers server restart.
    /// </summary>
    [HttpPost("restart")]
    public IActionResult TriggerServerRestart()
    {
        _logger.LogWarning("Server restart requested by {User}", User.Identity?.Name ?? "unknown");

        Task.Run(async () =>
        {
            await Task.Delay(2000); // Give time to return response
            _appLifetime.StopApplication();
        });

        return Ok(new
        {
            Success = true,
            Message = "Server restart initiated. Please wait 10 seconds and refresh."
        });
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

    /// <summary>
    /// Builds a SQL Server connection string from the request.
    /// </summary>
    private string BuildConnectionString(TestConnectionRequest request)
    {
        var builder = new SqlConnectionStringBuilder();
        builder.DataSource = request.ServerAddress;
        builder.InitialCatalog = request.DatabaseName;
        builder.TrustServerCertificate = request.TrustServerCertificate;

        if (request.AuthenticationType == "Windows")
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = request.Username;
            builder.Password = request.Password;
        }

        return builder.ConnectionString;
    }
}
