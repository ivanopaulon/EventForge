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

    /// <summary>
    /// Helper method to create file marker if missing. Prevents code duplication and handles errors gracefully.
    /// </summary>
    private void CreateFileMarkerIfMissing(string markerPath, string reason)
    {
        if (!File.Exists(markerPath))
        {
            try
            {
                File.WriteAllText(markerPath, $"Setup completed ({reason} on {DateTime.UtcNow:O})");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create file marker (non-critical)");
            }
        }
    }

    public async Task<bool> IsSetupCompleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Level 1: Check environment variable
            var envSetupComplete = Environment.GetEnvironmentVariable("EVENTFORGE_SETUP_COMPLETED");

            if (!string.IsNullOrEmpty(envSetupComplete) && envSetupComplete.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Level 2: Check file marker
            var markerPath = Path.Combine(_environment.ContentRootPath, "setup.complete");

            if (File.Exists(markerPath))
            {
                return true;
            }

            // Level 3: Check connection string and database
            var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            var sqlServerConnection = _configuration.GetConnectionString("SqlServer");

            var connectionString = defaultConnection ?? sqlServerConnection;

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Setup not complete - no connection string configured");
                return false;
            }

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
                        // AUTO-FIX: Create file marker if missing
                        CreateFileMarkerIfMissing(markerPath, "auto-synced from database");

                        return true;
                    }
                    else
                    {
                        // AUTO-FIX: Populate SetupHistories from existing configuration
                        try
                        {
                            var insertCommand = new SqlCommand(
                                @"INSERT INTO SetupHistories 
                                  (Id, CompletedAt, CompletedBy, ConfigurationSnapshot, Version, TenantId, CreatedAt, CreatedBy, IsDeleted, IsActive) 
                                  VALUES 
                                  (NEWID(), GETUTCDATE(), @completedBy, @snapshot, @version, @tenantId, GETUTCDATE(), @createdBy, 0, 1)",
                                connection);

                            var configSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                ServerAddress = connection.DataSource,
                                DatabaseName = connection.Database,
                                Environment = _environment.EnvironmentName,
                                AutoSynced = true,
                                SyncedAt = DateTime.UtcNow
                            });

                            insertCommand.Parameters.AddWithValue("@completedBy", "auto_sync");
                            insertCommand.Parameters.AddWithValue("@snapshot", configSnapshot);
                            insertCommand.Parameters.AddWithValue("@version", "1.0.0");
                            insertCommand.Parameters.AddWithValue("@tenantId", Guid.Empty);
                            insertCommand.Parameters.AddWithValue("@createdBy", "system");

                            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                            // Create file marker
                            CreateFileMarkerIfMissing(markerPath, "auto-synced");

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to auto-populate SetupHistories");
                            // Continue to other checks
                        }
                    }
                }

                await connection.CloseAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error {ErrorNumber} during setup check", ex.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed during setup check");
            }

            // Level 4: Fallback - EF Core
            try
            {
                var hasSetupHistory = await _dbContext.SetupHistories.AnyAsync(cancellationToken);
                if (hasSetupHistory)
                {
                    // Auto-create file marker
                    CreateFileMarkerIfMissing(markerPath, "auto-synced via EF Core");

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EF Core check failed");
            }

            _logger.LogWarning("Setup not complete - setup wizard required");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in first run detection");
            return false;
        }
    }
}
