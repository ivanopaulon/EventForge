using EventForge.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.HostedServices;

/// <summary>
/// Background service that periodically releases stale document locks
/// that have exceeded the maximum lock duration (1 hour).
/// </summary>
public class DocumentLockCleanupWorker : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxLockDuration = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentLockCleanupWorker> _logger;

    public DocumentLockCleanupWorker(IServiceProvider serviceProvider, ILogger<DocumentLockCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentLockCleanupWorker started. Interval: {Interval}, MaxLockDuration: {MaxLockDuration}",
            CleanupInterval, MaxLockDuration);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
                await CleanupExpiredLocksAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DocumentLockCleanupWorker");
            }
        }

        _logger.LogInformation("DocumentLockCleanupWorker stopped.");
    }

    private async Task CleanupExpiredLocksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var expiryThreshold = DateTime.UtcNow - MaxLockDuration;

        var expiredLocks = await dbContext.DocumentHeaders
            .Where(d => !d.IsDeleted && d.LockedBy != null && d.LockedAt.HasValue && d.LockedAt.Value < expiryThreshold)
            .ToListAsync(cancellationToken);

        if (expiredLocks.Count == 0)
        {
            _logger.LogDebug("DocumentLockCleanupWorker: no expired locks found.");
            return;
        }

        _logger.LogInformation("DocumentLockCleanupWorker: releasing {Count} expired lock(s).", expiredLocks.Count);

        foreach (var doc in expiredLocks)
        {
            _logger.LogInformation(
                "Releasing expired lock on document {DocumentId} (locked by {LockedBy} since {LockedAt})",
                doc.Id, doc.LockedBy, doc.LockedAt);

            doc.LockedBy = null;
            doc.LockedAt = null;
            doc.LockConnectionId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
