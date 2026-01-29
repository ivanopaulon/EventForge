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
            // Check 1: Environment variable
            var envSetupComplete = Environment.GetEnvironmentVariable("EVENTFORGE_SETUP_COMPLETED");
            if (!string.IsNullOrEmpty(envSetupComplete) && envSetupComplete.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Setup completed: Environment variable EVENTFORGE_SETUP_COMPLETED is set to true");
                return true;
            }

            // Check 2: File marker
            var markerPath = Path.Combine(_environment.ContentRootPath, "setup.complete");
            if (File.Exists(markerPath))
            {
                _logger.LogDebug("Setup completed: File marker 'setup.complete' exists");
                return true;
            }

            // Check 3: Database check for SetupHistories
            try
            {
                var hasSetupHistory = await _dbContext.SetupHistories.AnyAsync(cancellationToken);
                if (hasSetupHistory)
                {
                    _logger.LogDebug("Setup completed: SetupHistories table has records");
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Database might not be accessible or table might not exist yet
                _logger.LogDebug(ex, "Unable to check SetupHistories table, assuming first run");
            }

            _logger.LogInformation("First run detected: No setup completion markers found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking setup status, assuming first run");
            return false;
        }
    }
}
