using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using EventForge.DTOs.Setup;
using EventForge.Server.Data.Entities.Configuration;
using EventForge.Server.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Setup;

/// <summary>
/// Implementation of setup wizard service.
/// </summary>
public class SetupWizardService : ISetupWizardService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SetupWizardService> _logger;

    public SetupWizardService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider,
        ILogger<SetupWizardService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<SetupResult> CompleteSetupAsync(SetupConfiguration config, CancellationToken cancellationToken = default)
    {
        var result = new SetupResult();

        try
        {
            _logger.LogInformation("Setup wizard started for environment: {Environment}", config.Environment);

            // Step 1: Save connection string to appsettings.overrides.json
            await SaveConnectionStringAsync(config, cancellationToken);

            // Step 2: Create database if requested
            if (config.CreateDatabase)
            {
                var dbCreated = await CreateDatabaseAsync(config, cancellationToken);
                if (!dbCreated)
                {
                    result.Errors.Add("Failed to create database");
                    result.Success = false;
                    return result;
                }
            }

            // Step 3: Apply EF Core migrations
            var migrationsApplied = await ApplyMigrationsAsync(cancellationToken);
            if (!migrationsApplied)
            {
                result.Errors.Add("Failed to apply database migrations");
                result.Success = false;
                return result;
            }

            // Step 4: Create SuperAdmin user
            var userCreated = await CreateSuperAdminUserAsync(config, cancellationToken);
            if (!userCreated)
            {
                result.Warnings.Add("SuperAdmin user may already exist");
            }

            // Step 5: Save security configuration
            await SaveSecurityConfigurationAsync(config, cancellationToken);

            // Step 6: Save setup history
            await SaveSetupHistoryAsync(config, cancellationToken);

            // Step 7: Create file marker
            CreateFileMarker();

            result.Success = true;
            result.Message = "Setup completed successfully!";
            result.RedirectUrl = "/";

            _logger.LogInformation("Setup completed successfully - database: {Database}", config.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setup wizard failed");
            result.Success = false;
            result.Errors.Add($"Setup failed: {ex.Message}");
        }

        return result;
    }

    private async Task SaveConnectionStringAsync(SetupConfiguration config, CancellationToken cancellationToken)
    {
        var overridesPath = Path.Combine(_environment.ContentRootPath, "appsettings.overrides.json");

        var connectionString = BuildConnectionString(config);

        var overrides = new
        {
            ConnectionStrings = new
            {
                DefaultConnection = connectionString,
                SqlServer = connectionString  // Backward compatibility
            },
            Authentication = new
            {
                Jwt = new
                {
                    Issuer = "EventForge",
                    Audience = "EventForge",
                    SecretKey = config.JwtSecretKey,
                    ExpirationMinutes = config.TokenExpirationMinutes,
                    ClockSkewMinutes = 5
                }
            },
            Security = new
            {
                EnforceHttps = config.EnforceHttps,
                EnableHsts = config.EnableHsts
            }
        };

        var json = JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(overridesPath, json, cancellationToken);
    }

    private async Task<bool> CreateDatabaseAsync(SetupConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            var masterConnectionString = BuildMasterConnectionString(config);
            
            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            var createDbCommand = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'{SanitizeDatabaseName(config.DatabaseName)}') CREATE DATABASE [{SanitizeDatabaseName(config.DatabaseName)}]";
            using var command = new SqlCommand(createDbCommand, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await connection.CloseAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database '{Database}'", config.DatabaseName);
            return false;
        }
    }

    private async Task<bool> ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

            await dbContext.Database.MigrateAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            return false;
        }
    }

    private async Task<bool> CreateSuperAdminUserAsync(SetupConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapService>();

            var success = await bootstrapService.EnsureAdminBootstrappedAsync(cancellationToken);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SuperAdmin user");
            return false;
        }
    }

    private async Task SaveSecurityConfigurationAsync(SetupConfiguration config, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var configurations = new List<SystemConfiguration>
        {
            new SystemConfiguration
            {
                Key = "Security.JwtSecretKey",
                Value = config.JwtSecretKey,
                Category = "Security",
                Description = "JWT secret key for token signing",
                IsEncrypted = true,
                RequiresRestart = true,
                IsReadOnly = true
            },
            new SystemConfiguration
            {
                Key = "Security.TokenExpirationMinutes",
                Value = config.TokenExpirationMinutes.ToString(),
                Category = "Security",
                Description = "JWT token expiration time in minutes"
            },
            new SystemConfiguration
            {
                Key = "Security.RateLimitingEnabled",
                Value = config.RateLimitingEnabled.ToString(),
                Category = "Security",
                Description = "Enable rate limiting for API endpoints"
            },
            new SystemConfiguration
            {
                Key = "Security.LoginAttemptsLimit",
                Value = config.LoginAttemptsLimit.ToString(),
                Category = "Security",
                Description = "Maximum login attempts before rate limiting"
            },
            new SystemConfiguration
            {
                Key = "Security.ApiCallsLimit",
                Value = config.ApiCallsLimit.ToString(),
                Category = "Security",
                Description = "Maximum API calls per minute"
            },
            new SystemConfiguration
            {
                Key = "Logging.RetentionDays",
                Value = config.LogRetentionDays.ToString(),
                Category = "Logging",
                Description = "Number of days to retain log entries"
            },
            new SystemConfiguration
            {
                Key = "System.MaintenanceMode",
                Value = "false",
                Category = "System",
                Description = "Enable maintenance mode"
            }
        };

        foreach (var configItem in configurations)
        {
            var existing = await dbContext.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == configItem.Key, cancellationToken);

            if (existing == null)
            {
                dbContext.SystemConfigurations.Add(configItem);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveSetupHistoryAsync(SetupConfiguration config, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var setupHistory = new SetupHistory
        {
            CompletedAt = DateTime.UtcNow,
            CompletedBy = config.SuperAdminUsername,
            ConfigurationSnapshot = JsonSerializer.Serialize(new
            {
                config.ServerAddress,
                config.DatabaseName,
                config.Environment,
                config.HttpPort,
                config.HttpsPort,
                config.RateLimitingEnabled,
                config.LogRetentionDays
            }),
            Version = version
        };

        dbContext.SetupHistories.Add(setupHistory);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void CreateFileMarker()
    {
        var markerPath = Path.Combine(_environment.ContentRootPath, "setup.complete");
        File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
    }

    private string BuildConnectionString(SetupConfiguration config)
    {
        // Build connection string matching appsettings.json format EXACTLY
        // Format: Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;
        
        if (config.Credentials.AuthenticationType == "Windows")
        {
            return $"Server={config.ServerAddress};Database={config.DatabaseName};Integrated Security=True;TrustServerCertificate=True;";
        }
        else
        {
            return $"Server={config.ServerAddress};Database={config.DatabaseName};User Id={config.Credentials.Username};Password={config.Credentials.Password};TrustServerCertificate=True;";
        }
    }

    private string BuildMasterConnectionString(SetupConfiguration config)
    {
        // Build master connection string matching appsettings.json format
        
        if (config.Credentials.AuthenticationType == "Windows")
        {
            return $"Server={config.ServerAddress};Database=master;Integrated Security=True;TrustServerCertificate=True;";
        }
        else
        {
            return $"Server={config.ServerAddress};Database=master;User Id={config.Credentials.Username};Password={config.Credentials.Password};TrustServerCertificate=True;";
        }
    }

    private static string SanitizeDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

        if (databaseName.Length > 128)
            throw new ArgumentException("Database name cannot exceed 128 characters", nameof(databaseName));

        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Database name contains invalid characters. Only letters, numbers, and underscores are allowed.", nameof(databaseName));

        return databaseName;
    }
}
