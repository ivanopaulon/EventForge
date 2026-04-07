using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Configuration;

/// <summary>
/// Hosted service that runs database migrations and bootstrap on application startup.
/// The bootstrap runs in the background so the application starts serving requests immediately.
/// </summary>
public class BootstrapHostedService(IServiceProvider serviceProvider, ILogger<BootstrapHostedService> logger) : IHostedService
{

    private Task? _bootstrapTask;
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Combine the host cancellation token with our own so StopAsync can cancel too
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Use CancellationToken.None for Task.Run so that host startup timeout
        // does not prevent the task from being scheduled; only our linked token cancels it.
        _bootstrapTask = Task.Run(() => RunBootstrapAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_bootstrapTask is null)
            return;

        try
        {
            await _cts!.CancelAsync();
            // Wait for the bootstrap task or the shutdown deadline, whichever comes first
            await Task.WhenAny(_bootstrapTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Bootstrap process was cancelled during shutdown.");
        }
        finally
        {
            _cts?.Dispose();
        }
    }

    private async Task RunBootstrapAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PrymDbContext>();

            if (!dbContext.Database.CanConnect())
            {
                logger.LogError("Cannot connect to database. Migration and bootstrap skipped.");
                return;
            }

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count == 0)
            {
                // Fast-path: skip bootstrap entirely if the system is already fully initialised
                var adminExists = await dbContext.Users.AnyAsync(u => u.Username == "superadmin", cancellationToken);
                if (adminExists)
                {
                    var hasTenant = await dbContext.Tenants.AnyAsync(t => t.Id != Guid.Empty, cancellationToken);
                    if (hasTenant)
                    {
                        var hasVatRates = await dbContext.VatRates.AnyAsync(cancellationToken);
                        var hasUMs = await dbContext.UMs.AnyAsync(cancellationToken);
                        var hasWarehouses = await dbContext.StorageFacilities.AnyAsync(cancellationToken);
                        if (hasVatRates && hasUMs && hasWarehouses)
                            return;

                        logger.LogWarning("Superadmin exists but base entities are missing. Running bootstrap to seed base entities.");
                    }
                }
            }
            else
            {
                logger.LogWarning("Applying {Count} pending migration(s): {Migrations}",
                    pendingList.Count, string.Join(", ", pendingList));
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapService>();
            if (!await bootstrapService.EnsureAdminBootstrappedAsync(cancellationToken))
                logger.LogWarning("Bootstrap process completed with warnings or errors.");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Database migration and bootstrap process was cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database migration and bootstrap. Application will continue but may require manual setup.");
        }
    }

}
