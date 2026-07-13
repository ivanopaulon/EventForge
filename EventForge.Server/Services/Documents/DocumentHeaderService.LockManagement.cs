using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{

    /// <summary>
    /// Acquires an exclusive edit lock for a document.
    /// Lock expires after 1 hour of inactivity.
    /// Uses optimistic concurrency control via RowVersion to prevent race conditions.
    /// </summary>
    public async Task<bool> AcquireLockAsync(Guid documentId, string userName, string connectionId, Guid? tenantId = null)
    {
        logger.LogDebug(
            "AcquireLockAsync called: DocumentId={DocumentId}, UserName={UserName}, ConnectionId={ConnectionId}",
            documentId, userName, connectionId);

        try
        {
            var effectiveTenantId = tenantId ?? tenantContext.CurrentTenantId;
            if (!effectiveTenantId.HasValue)
            {
                logger.LogWarning(
                    "❌ Lock acquisition FAILED: TenantId is NULL. DocumentId={DocumentId}, UserName={UserName}",
                    documentId, userName);
                return false;
            }

            logger.LogDebug(
                "TenantId retrieved: {TenantId} for document {DocumentId}",
                effectiveTenantId.Value, documentId);

            // Use a retry pattern for optimistic concurrency
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    logger.LogDebug(
                        "Lock acquisition attempt {Attempt}/{MaxRetries} for document {DocumentId}",
                        attempt + 1, maxRetries, documentId);

                    var document = await context.DocumentHeaders
                        .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == effectiveTenantId.Value && !d.IsDeleted);

                    if (document is null)
                    {
                        logger.LogWarning(
                            "❌ Lock acquisition FAILED: Document NOT FOUND. DocumentId={DocumentId}, TenantId={TenantId}, UserName={UserName}",
                            documentId, effectiveTenantId.Value, userName);
                        return false;
                    }

                    logger.LogDebug(
                        "Document found: {DocumentId}, Current lock status: LockedBy={LockedBy}, LockedAt={LockedAt}, ConnectionId={ConnectionId}",
                        documentId, document.LockedBy ?? "(none)", document.LockedAt, document.LockConnectionId ?? "(none)");

                    // Check existing lock
                    if (!string.IsNullOrEmpty(document.LockedBy) && document.LockedBy != userName)
                    {
                        logger.LogDebug(
                            "Document {DocumentId} has existing lock by different user. Current: {CurrentUser}, Requested: {RequestedUser}",
                            documentId, document.LockedBy, userName);

                        // Check if lock is still valid (less than 1 hour old)
                        if (document.LockedAt.HasValue)
                        {
                            var lockAge = DateTime.UtcNow - document.LockedAt.Value;

                            logger.LogDebug(
                                "Lock age check: {LockAge} (threshold: 1 hour) for document {DocumentId}",
                                lockAge, documentId);

                            if (lockAge < TimeSpan.FromHours(1))
                            {
                                logger.LogWarning(
                                    "❌ Lock acquisition FAILED: Document {DocumentId} is locked by {LockedBy} (lock age: {LockAge}, still valid)",
                                    documentId, document.LockedBy, lockAge);
                                return false; // Lock is still valid
                            }

                            // Lock expired - can be acquired
                            logger.LogInformation(
                                "Lock on document {DocumentId} EXPIRED (lock age: {LockAge}). Acquiring for {UserName}.",
                                documentId, lockAge, userName);
                        }
                    }

                    // Acquire or refresh lock
                    logger.LogDebug(
                        "Attempting to set lock: DocumentId={DocumentId}, UserName={UserName}, ConnectionId={ConnectionId}",
                        documentId, userName, connectionId);

                    document.LockedBy = userName;
                    document.LockedAt = DateTime.UtcNow;
                    document.LockConnectionId = connectionId;

                    logger.LogDebug(
                        "Lock properties set, calling SaveChangesAsync for document {DocumentId}",
                        documentId);

                    var changeCount = await context.SaveChangesAsync();

                    logger.LogInformation(
                        "✅ Lock ACQUIRED successfully on document {DocumentId} by {UserName} (connection: {ConnectionId}). Changes saved: {ChangeCount}",
                        documentId, userName, connectionId, changeCount);

                    return true;
                }
                catch (DbUpdateConcurrencyException concurrencyEx) when (attempt < maxRetries - 1)
                {
                    logger.LogWarning(
                        concurrencyEx,
                        "⚠️ Concurrency conflict acquiring lock for document {DocumentId}, attempt {Attempt}/{MaxRetries}. Retrying...",
                        documentId, attempt + 1, maxRetries);

                    // Detach the entity to allow retry
                    var entries = context.ChangeTracker.Entries()
                        .Where(e => e.Entity is DocumentHeader && ((DocumentHeader)e.Entity).Id == documentId);
                    foreach (var entry in entries)
                    {
                        logger.LogDebug(
                            "Detaching entity {EntityType} with state {State}",
                            entry.Entity.GetType().Name, entry.State);
                        entry.State = EntityState.Detached;
                    }

                    // Small delay before retry
                    await Task.Delay(50 * (attempt + 1));
                }
                catch (DbUpdateException dbEx)
                {
                    // Non-concurrency database errors (e.g., constraint violations, FK errors)
                    // are not transient and should not be retried - propagate to outer catch
                    logger.LogError(
                        dbEx,
                        "❌ DATABASE UPDATE ERROR during lock acquisition for document {DocumentId}, attempt {Attempt}. Inner exception: {InnerException}",
                        documentId, attempt + 1, dbEx.InnerException?.Message ?? "(none)");
                    throw; // Re-throw to be caught by outer catch
                }
            }

            // All retries failed
            logger.LogWarning(
                "❌ Lock acquisition FAILED: Max retries ({MaxRetries}) exceeded for document {DocumentId}",
                documentId, maxRetries);
            return false;
        }
        catch (DbUpdateConcurrencyException finalConcurrencyEx)
        {
            logger.LogError(
                finalConcurrencyEx,
                "❌ CONCURRENCY EXCEPTION (final) acquiring lock for document {DocumentId}",
                documentId);
            return false;
        }
        catch (DbUpdateException finalDbEx)
        {
            logger.LogError(
                finalDbEx,
                "❌ DATABASE EXCEPTION acquiring lock for document {DocumentId}. Inner: {InnerException}",
                documentId, finalDbEx.InnerException?.Message ?? "(none)");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "❌ UNEXPECTED EXCEPTION acquiring lock for document {DocumentId}. Exception type: {ExceptionType}, Message: {Message}",
                documentId, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Releases an edit lock for a document.
    /// Only the user who holds the lock can release it.
    /// </summary>
    public async Task<bool> ReleaseLockAsync(Guid documentId, string userName, Guid? tenantId = null)
    {
        try
        {
            var effectiveTenantId = tenantId ?? tenantContext.CurrentTenantId;
            if (!effectiveTenantId.HasValue)
            {
                logger.LogWarning("Cannot release lock without a tenant context.");
                return false;
            }

            var document = await context.DocumentHeaders
                .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == effectiveTenantId.Value && !d.IsDeleted);

            if (document is null)
            {
                logger.LogWarning("Document {DocumentId} not found for lock release.", documentId);
                return false;
            }

            // Only the user who holds the lock can release it
            if (document.LockedBy == userName)
            {
                document.LockedBy = null;
                document.LockedAt = null;
                document.LockConnectionId = null;

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Lock released on document {DocumentId} by {UserName}",
                    documentId, userName);

                return true;
            }

            if (document.LockedBy is null)
            {
                logger.LogWarning(
                    "User {UserName} attempted to release lock on document {DocumentId} but the document is not locked.",
                    userName, documentId);
            }
            else
            {
                logger.LogWarning(
                    "User {UserName} attempted to release lock on document {DocumentId} but doesn't hold it (locked by: {LockedBy})",
                    userName, documentId, document.LockedBy);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error releasing lock for document {DocumentId}", documentId);
            return false;
        }
    }

    /// <summary>
    /// Releases all locks held by a specific SignalR connection.
    /// Called when a user disconnects.
    /// </summary>
    public async Task ReleaseAllLocksForConnectionAsync(string connectionId, Guid? tenantId = null)
    {
        try
        {
            var effectiveTenantId = tenantId ?? tenantContext.CurrentTenantId;

            var query = context.DocumentHeaders
                .Where(d => d.LockConnectionId == connectionId && !d.IsDeleted);

            if (effectiveTenantId.HasValue)
                query = query.Where(d => d.TenantId == effectiveTenantId.Value);

            var documents = await query.ToListAsync();

            if (documents.Any())
            {
                foreach (var doc in documents)
                {
                    logger.LogInformation(
                        "Releasing lock on document {DocumentId} (locked by {LockedBy}) due to connection disconnect",
                        doc.Id, doc.LockedBy);

                    doc.LockedBy = null;
                    doc.LockedAt = null;
                    doc.LockConnectionId = null;
                }

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Released {Count} locks for disconnected connection {ConnectionId}",
                    documents.Count, connectionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error releasing locks for connection {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Gets lock information for a document.
    /// </summary>
    public async Task<DocumentLockInfo?> GetLockInfoAsync(Guid documentId, Guid? tenantId = null)
    {
        try
        {
            var effectiveTenantId = tenantId ?? tenantContext.CurrentTenantId;
            if (!effectiveTenantId.HasValue)
            {
                logger.LogWarning("Cannot get lock info without a tenant context.");
                return null;
            }

            var lockInfo = await context.DocumentHeaders
                .AsNoTracking()
                .Where(d => d.Id == documentId && d.TenantId == effectiveTenantId.Value && !d.IsDeleted)
                .Select(d => new DocumentLockInfo
                {
                    DocumentId = d.Id,
                    IsLocked = !string.IsNullOrEmpty(d.LockedBy),
                    LockedBy = d.LockedBy,
                    LockedAt = d.LockedAt,
                    ConnectionId = d.LockConnectionId
                })
                .FirstOrDefaultAsync();

            return lockInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lock info for document {DocumentId}", documentId);
            return null;
        }
    }

}
