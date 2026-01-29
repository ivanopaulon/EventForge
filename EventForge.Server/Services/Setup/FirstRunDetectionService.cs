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
            _logger.LogInformation("=== FIRST RUN DETECTION START ===");
            _logger.LogInformation("ContentRootPath: {Path}", _environment.ContentRootPath);
            _logger.LogInformation("EnvironmentName: {Env}", _environment.EnvironmentName);
            
            // Level 1: Check environment variable
            var envSetupComplete = Environment.GetEnvironmentVariable("EVENTFORGE_SETUP_COMPLETED");
            _logger.LogDebug("Environment variable EVENTFORGE_SETUP_COMPLETED: {Value}", envSetupComplete ?? "(not set)");
            
            if (!string.IsNullOrEmpty(envSetupComplete) && envSetupComplete.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("✅ Setup completed: Environment variable set");
                return true;
            }

            // Level 2: Check file marker
            var markerPath = Path.Combine(_environment.ContentRootPath, "setup.complete");
            _logger.LogInformation("Checking file marker at: {Path}", markerPath);
            _logger.LogInformation("File exists: {Exists}", File.Exists(markerPath));
            
            if (File.Exists(markerPath))
            {
                _logger.LogInformation("✅ Setup completed: File marker exists");
                return true;
            }

            // Level 3: Check connection string
            _logger.LogInformation("--- Checking Configuration ---");
            
            // Debug: Verify ALL connection strings in configuration
            var allConnectionStrings = _configuration.GetSection("ConnectionStrings").GetChildren();
            _logger.LogInformation("Connection strings found in configuration:");
            foreach (var cs in allConnectionStrings)
            {
                // Log key and value length (NOT the full value for security)
                _logger.LogInformation("  - {Key}: {Length} chars", cs.Key, cs.Value?.Length ?? 0);
            }
            
            var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            var sqlServerConnection = _configuration.GetConnectionString("SqlServer");
            
            _logger.LogInformation("DefaultConnection: {HasValue} ({Length} chars)", 
                defaultConnection != null ? "FOUND" : "NOT FOUND", 
                defaultConnection?.Length ?? 0);
            
            _logger.LogInformation("SqlServer: {HasValue} ({Length} chars)", 
                sqlServerConnection != null ? "FOUND" : "NOT FOUND", 
                sqlServerConnection?.Length ?? 0);
            
            var connectionString = defaultConnection ?? sqlServerConnection;
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("❌ No connection string found in configuration!");
                _logger.LogInformation("First run detected: No connection string configured");
                return false;
            }

            // Debug: Log connection string details (without password)
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                _logger.LogInformation("Connection string details:");
                _logger.LogInformation("  - Server: {Server}", builder.DataSource);
                _logger.LogInformation("  - Database: {Database}", builder.InitialCatalog);
                _logger.LogInformation("  - Auth Type: {Auth}", builder.IntegratedSecurity ? "Windows" : "SQL");
                if (!builder.IntegratedSecurity)
                {
                    _logger.LogInformation("  - User: {User}", builder.UserID);
                    _logger.LogInformation("  - Password: {HasPassword}", !string.IsNullOrEmpty(builder.Password) ? "SET" : "NOT SET");
                }
                _logger.LogInformation("  - TrustServerCertificate: {Trust}", builder.TrustServerCertificate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse connection string");
            }

            _logger.LogInformation("Attempting database connection...");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                
                _logger.LogDebug("Opening connection to: {Server}", connection.DataSource);
                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("✅ Database connection successful!");
                
                // Check if SetupHistories table exists
                using var command = new SqlCommand(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SetupHistories') THEN 1 ELSE 0 END", 
                    connection);
                
                var tableExists = (int)await command.ExecuteScalarAsync(cancellationToken) == 1;
                _logger.LogInformation("SetupHistories table exists: {Exists}", tableExists);
                
                if (tableExists)
                {
                    using var countCommand = new SqlCommand("SELECT COUNT(*) FROM SetupHistories", connection);
                    var count = (int)await countCommand.ExecuteScalarAsync(cancellationToken);
                    
                    _logger.LogInformation("SetupHistories records: {Count}", count);
                    
                    if (count > 0)
                    {
                        _logger.LogInformation("✅ Setup completed: Database has {Count} setup history records", count);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ SetupHistories table exists but is EMPTY");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ SetupHistories table does NOT exist");
                }
                
                await connection.CloseAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "❌ SQL Error {Number}: {Message}", ex.Number, ex.Message);
                
                // Log specific SQL error codes for quick diagnosis
                switch (ex.Number)
                {
                    case 18456:
                        _logger.LogError("SQL Authentication FAILED - Invalid username or password");
                        break;
                    case 4060:
                        _logger.LogError("Database does NOT exist or cannot be opened");
                        break;
                    case 53:
                        _logger.LogError("SQL Server NOT reachable - Check server name and network");
                        break;
                    case -1:
                        _logger.LogError("Connection timeout - SQL Server may not be running");
                        break;
                    default:
                        _logger.LogError("Unhandled SQL error code: {Code}", ex.Number);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Database connection failed: {Message}", ex.Message);
            }

            // Level 4: Fallback - EF Core
            _logger.LogInformation("Trying EF Core fallback...");
            
            try
            {
                var hasSetupHistory = await _dbContext.SetupHistories.AnyAsync(cancellationToken);
                if (hasSetupHistory)
                {
                    _logger.LogInformation("✅ Setup completed: Found via EF Core");
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ EF Core connected but SetupHistories is empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EF Core check failed: {Message}", ex.Message);
            }

            _logger.LogWarning("=== FIRST RUN DETECTED ===");
            _logger.LogInformation("No setup completion markers found - redirecting to setup wizard");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CRITICAL ERROR in first run detection");
            return false;
        }
    }
}
