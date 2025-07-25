using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for health check and API version information.
/// </summary>
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : BaseApiController
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(EventForgeDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the health status of the API and its dependencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information including API status, database status, timestamp, and version</returns>
    /// <response code="200">Returns the health status</response>
    /// <response code="503">If the service is unavailable</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthStatusDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusDto>> GetHealth(CancellationToken cancellationToken = default)
    {
        var healthStatus = new HealthStatusDto
        {
            ApiStatus = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetApiVersion()
        };

        try
        {
            // Check database connectivity
            healthStatus.DatabaseStatus = await GetDatabaseStatusAsync(cancellationToken);
            
            // Determine overall health
            var isHealthy = healthStatus.DatabaseStatus == "Healthy";
            
            if (!isHealthy)
            {
                healthStatus.ApiStatus = "Degraded";
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);
            }

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during health check");
            
            healthStatus.ApiStatus = "Unhealthy";
            healthStatus.DatabaseStatus = "Error";
            healthStatus.ErrorMessage = "An error occurred during health check";
            
            return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);
        }
    }

    /// <summary>
    /// Gets detailed health information about the API and its dependencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed health status information</returns>
    /// <response code="200">Returns the detailed health status</response>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DetailedHealthStatusDto>> GetDetailedHealth(CancellationToken cancellationToken = default)
    {
        var healthStatus = new DetailedHealthStatusDto
        {
            ApiStatus = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetApiVersion(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingSet = Environment.WorkingSet,
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
        };

        try
        {
            // Check database connectivity with additional details
            var dbCheckResult = await GetDetailedDatabaseStatusAsync(cancellationToken);
            healthStatus.DatabaseStatus = dbCheckResult.Status;
            healthStatus.DatabaseDetails = dbCheckResult.Details;
            
            // Add dependency checks here (external APIs, caches, etc.)
            healthStatus.Dependencies = new Dictionary<string, string>
            {
                ["Database"] = healthStatus.DatabaseStatus ?? "Unknown"
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during detailed health check");
            
            healthStatus.ApiStatus = "Unhealthy";
            healthStatus.DatabaseStatus = "Error";
            healthStatus.ErrorMessage = ex.Message;
            
            return Ok(healthStatus); // Return 200 even for errors in detailed view
        }
    }

    private async Task<string> GetDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect ? "Healthy" : "Unreachable";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return "Error";
        }
    }

    private async Task<(string Status, Dictionary<string, object> Details)> GetDetailedDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        var details = new Dictionary<string, object>();
        
        try
        {
            var startTime = DateTime.UtcNow;
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            var responseTime = DateTime.UtcNow - startTime;
            
            details["CanConnect"] = canConnect;
            details["ResponseTimeMs"] = responseTime.TotalMilliseconds;
            details["ConnectionString"] = _dbContext.Database.GetConnectionString()?.Replace(";Password=", ";Password=***") ?? "Unknown";
            details["ProviderName"] = _dbContext.Database.ProviderName ?? "Unknown";
            
            if (canConnect)
            {
                try
                {
                    // Test a simple query to verify database functionality
                    var documentTypeCount = await _dbContext.DocumentTypes.CountAsync(cancellationToken);
                    details["TableAccessible"] = true;
                    details["SampleTableCount"] = documentTypeCount;
                }
                catch (Exception queryEx)
                {
                    details["TableAccessible"] = false;
                    details["QueryError"] = queryEx.Message;
                }
            }
            
            return (canConnect ? "Healthy" : "Unreachable", details);
        }
        catch (Exception ex)
        {
            details["Error"] = ex.Message;
            return ("Error", details);
        }
    }

    private string GetApiVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// Basic health status information.
/// </summary>
public class HealthStatusDto
{
    /// <summary>
    /// API health status.
    /// </summary>
    public string ApiStatus { get; set; } = string.Empty;

    /// <summary>
    /// Database health status.
    /// </summary>
    public string DatabaseStatus { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the health check (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Error message if any issues occurred.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Detailed health status information.
/// </summary>
public class DetailedHealthStatusDto : HealthStatusDto
{
    /// <summary>
    /// Environment name.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Machine name.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Process ID.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Working set memory usage.
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Application uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Database connection details.
    /// </summary>
    public Dictionary<string, object>? DatabaseDetails { get; set; }

    /// <summary>
    /// Status of dependencies.
    /// </summary>
    public Dictionary<string, string>? Dependencies { get; set; }
}