using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Setup;

/// <summary>
/// Implementation of first run detection service.
/// </summary>
public class FirstRunDetectionService : IFirstRunDetectionService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FirstRunDetectionService> _logger;

    public FirstRunDetectionService(
        EventForgeDbContext dbContext,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<FirstRunDetectionService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> IsSetupCompleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Level 1: Check environment variable
            var envSetupComplete = Environment.GetEnvironmentVariable("EVENTFORGE_SETUP_COMPLETED");
            if (!string.IsNullOrEmpty(envSetupComplete) && envSetupComplete.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Setup completed: Environment variable EVENTFORGE_SETUP_COMPLETED is set to true");
                return true;
            }

            // Level 2: Check file marker
            var markerPath = Path.Combine(_environment.ContentRootPath, "setup.complete");
            if (File.Exists(markerPath))
            {
                _logger.LogDebug("Setup completed: File marker 'setup.complete' exists at {Path}", markerPath);
                return true;
            }

            // Level 3: Check if valid connection string exists and database is accessible
            var connectionString = _configuration.GetConnectionString("DefaultConnection") 
                                ?? _configuration.GetConnectionString("SqlServer");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);
                    
                    // Check if SetupHistories table exists
                    using var command = new SqlCommand(
                        "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SetupHistories') THEN 1 ELSE 0 END", 
                        connection);
                    
                    var tableExists = (int)await command.ExecuteScalarAsync(cancellationToken) == 1;
                    
                    if (tableExists)
                    {
                        using var countCommand = new SqlCommand("SELECT COUNT(*) FROM SetupHistories", connection);
                        var count = (int)await countCommand.ExecuteScalarAsync(cancellationToken);
                        
                        if (count > 0)
                        {
                            _logger.LogInformation("Setup completed: Database accessible with {Count} setup history records", count);
                            return true;
                        }
                    }
                    
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Database connection check failed during first run detection");
                }
            }

            // Level 4: Fallback - Check via EF Core (if DB context can connect)
            try
            {
                var hasSetupHistory = await _dbContext.SetupHistories.AnyAsync(cancellationToken);
                if (hasSetupHistory)
                {
                    _logger.LogDebug("Setup completed: SetupHistories table has records (via EF Core)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EF Core check failed during first run detection (expected on first run)");
            }

            _logger.LogInformation("First run detected: No setup completion markers found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking setup status, assuming first run for safety");
            return false;
        }
    }
}
