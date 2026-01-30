using EventForge.DTOs.Health;
using EventForge.Server.Data;
using EventForge.Server.Services;
using EventForge.Server.Services.Performance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for health check and API version information.
/// </summary>
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : BaseApiController
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;
    private readonly IPerformanceMonitoringService? _performanceService;

    public HealthController(EventForgeDbContext dbContext, ILogger<HealthController> logger, IPerformanceMonitoringService? performanceService = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceService = performanceService;
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

            // Check authentication system
            healthStatus.AuthenticationStatus = GetAuthenticationStatus();

            // Determine overall health
            var isHealthy = healthStatus.DatabaseStatus == "Healthy" && healthStatus.AuthenticationStatus == "Healthy";

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

            // Get applied migrations - always populate this property
            healthStatus.AppliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);

            // Check authentication system with details
            healthStatus.AuthenticationStatus = GetAuthenticationStatus();
            healthStatus.AuthenticationDetails = GetAuthenticationDetails();

            // Add performance information if available
            if (_performanceService != null)
            {
                try
                {
                    var perfStats = await _performanceService.GetStatisticsAsync();
                    healthStatus.AuthenticationDetails["PerformanceMonitoring"] = "Enabled";
                    healthStatus.AuthenticationDetails["TotalQueries"] = perfStats.TotalQueries;
                    healthStatus.AuthenticationDetails["SlowQueryPercentage"] = Math.Round(perfStats.SlowQueryPercentage, 2);
                    healthStatus.AuthenticationDetails["AverageQueryDurationMs"] = Math.Round(perfStats.AverageQueryDuration.TotalMilliseconds, 2);
                }
                catch
                {
                    healthStatus.AuthenticationDetails["PerformanceMonitoring"] = "Error";
                }
            }
            else
            {
                healthStatus.AuthenticationDetails["PerformanceMonitoring"] = "Disabled";
            }

            // Add dependency checks here (external APIs, caches, etc.)
            healthStatus.Dependencies = new Dictionary<string, string>
            {
                ["Database"] = healthStatus.DatabaseStatus ?? "Unknown",
                ["Authentication"] = healthStatus.AuthenticationStatus ?? "Unknown",
                ["PerformanceMonitoring"] = _performanceService != null ? "Healthy" : "Disabled"
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during detailed health check");

            healthStatus.ApiStatus = "Unhealthy";
            healthStatus.DatabaseStatus = "Error";
            healthStatus.ErrorMessage = ex.Message;

            // Ensure AppliedMigrations is always populated, even in error cases
            if (healthStatus.AppliedMigrations == null || !healthStatus.AppliedMigrations.Any())
            {
                healthStatus.AppliedMigrations = new List<string>();
            }

            return Ok(healthStatus); // Return 200 even for errors in detailed view
        }
    }

    private async Task<IEnumerable<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            _logger.LogDebug("Retrieved {Count} applied migrations", appliedMigrations.Count());
            return appliedMigrations;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve applied migrations");
            return new List<string>();
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

    private string GetAuthenticationStatus()
    {
        try
        {
            // Check if JWT configuration is valid
            var jwtSection = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Authentication:Jwt");
            var secretKey = jwtSection["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                return "Configuration Error";
            }

            // Check if bootstrap service is working
            var bootstrapService = HttpContext.RequestServices.GetService<IBootstrapService>();
            if (bootstrapService == null)
            {
                return "Service Error";
            }

            return "Healthy";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Authentication health check failed");
            return "Error";
        }
    }

    private Dictionary<string, object> GetAuthenticationDetails()
    {
        var details = new Dictionary<string, object>();

        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var jwtSection = configuration.GetSection("Authentication:Jwt");

            details["JwtIssuer"] = jwtSection["Issuer"] ?? "Unknown";
            details["JwtAudience"] = jwtSection["Audience"] ?? "Unknown";
            details["JwtExpirationMinutes"] = jwtSection["ExpirationMinutes"] ?? "Unknown";
            details["HasSecretKey"] = !string.IsNullOrEmpty(jwtSection["SecretKey"]);

            var passwordSection = configuration.GetSection("Authentication:PasswordPolicy");
            details["PasswordMinLength"] = passwordSection["MinimumLength"] ?? "Unknown";
            details["PasswordRequireUppercase"] = passwordSection["RequireUppercase"] ?? "Unknown";
            details["PasswordRequireDigits"] = passwordSection["RequireDigits"] ?? "Unknown";

            var lockoutSection = configuration.GetSection("Authentication:AccountLockout");
            details["MaxFailedAttempts"] = lockoutSection["MaxFailedAttempts"] ?? "Unknown";
            details["LockoutDurationMinutes"] = lockoutSection["LockoutDurationMinutes"] ?? "Unknown";

            var bootstrapSection = configuration.GetSection("Authentication:Bootstrap");
            details["AutoCreateAdmin"] = bootstrapSection["AutoCreateAdmin"] ?? "Unknown";
            details["DefaultAdminUsername"] = bootstrapSection["DefaultAdminUsername"] ?? "Unknown";

            details["AuthenticationScheme"] = "JWT Bearer";
            details["ConfigurationValid"] = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting authentication details");
            details["Error"] = ex.Message;
            details["ConfigurationValid"] = false;
        }

        return details;
    }
}