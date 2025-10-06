using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Hosted service that runs database migrations and bootstrap process on application startup.
/// </summary>
public class BootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BootstrapHostedService> _logger;

    public BootstrapHostedService(IServiceProvider serviceProvider, ILogger<BootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Run bootstrap in background to avoid blocking application startup
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting database migration and bootstrap process in background...");

                using var scope = _serviceProvider.CreateScope();

                // Apply migrations first
                var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

                // Fast-path check: Can we connect to the database?
                if (!dbContext.Database.CanConnect())
                {
                    _logger.LogError("Cannot connect to database. Migration and bootstrap skipped.");
                    return;
                }

                // Fast-path check: Are there any pending migrations?
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

                if (!pendingMigrations.Any())
                {
                    _logger.LogInformation("Database is up to date. No migrations to apply.");
                    
                    // Fast-path check: Does admin already exist?
                    var adminExists = await dbContext.Users.AnyAsync(u => u.Username == "superadmin", cancellationToken);
                    if (adminExists)
                    {
                        _logger.LogInformation("Bootstrap already complete. Skipping bootstrap process for faster startup.");
                        return;
                    }
                }
                else
                {
                    _logger.LogInformation("Found {Count} pending migrations: {Migrations}",
                        pendingMigrations.Count(), string.Join(", ", pendingMigrations));

                    await dbContext.Database.MigrateAsync(cancellationToken);

                    _logger.LogInformation("Database migrations applied successfully: {AppliedMigrations}",
                        string.Join(", ", pendingMigrations));
                }

                // Run bootstrap process only if needed
                var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapService>();
                var bootstrapResult = await bootstrapService.EnsureAdminBootstrappedAsync(cancellationToken);

                if (bootstrapResult)
                {
                    _logger.LogInformation("Bootstrap process completed successfully");
                }
                else
                {
                    _logger.LogWarning("Bootstrap process completed with warnings or errors");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Database migration and bootstrap process was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database migration and bootstrap process. Application will continue but may require manual setup.");
            }
        }, cancellationToken);

        // Return immediately to allow application to start serving requests
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No cleanup needed
        return Task.CompletedTask;
    }
}