using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{
    /// <summary>
    /// Finds all duplicate DirectMessage chats for <paramref name="userId"/> (same pair of two users,
    /// same tenant) and merges each group into a single primary thread.
    ///
    /// Strategy:
    ///   - For each pair of users that has more than one active DM thread, the thread with the
    ///     most-recent UpdatedAt becomes the "primary".
    ///   - All ChatMessages from secondary threads are re-parented to the primary thread.
    ///   - All MessageReadReceipts and MessageAttachments referencing secondary messages are
    ///     implicitly kept (their MessageId does not change; only the parent thread changes).
    ///   - ChatMembers in secondary threads that are NOT already members of the primary are
    ///     moved to the primary.
    ///   - Secondary threads are soft-deleted (IsDeleted = true, IsActive = false).
    /// </summary>
    public async Task<DmMergeResultDto> MergeDirectMessageDuplicatesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = new DmMergeResultDto();

        try
        {
            // 1. Find all active DM threads where this user is a member.
            var userDmQuery = context.ChatThreads
                .Where(ct => !ct.IsDeleted && ct.IsActive && ct.Type == ChatType.DirectMessage);

            if (tenantId.HasValue)
                userDmQuery = userDmQuery.Where(ct => ct.TenantId == tenantId.Value);

            var userDmIds = await userDmQuery
                .Where(ct => ct.Members.Any(m => m.UserId == userId && !m.IsDeleted))
                .Select(ct => ct.Id)
                .ToListAsync(cancellationToken);

            if (userDmIds.Count < 2) return result; // Nothing to merge

            // 2. Load members for all those threads (only two-person threads qualify as DMs).
            var membersByThread = await context.ChatMembers
                .AsNoTracking()
                .Where(cm => userDmIds.Contains(cm.ChatThreadId) && !cm.IsDeleted)
                .Select(cm => new { cm.ChatThreadId, cm.UserId })
                .ToListAsync(cancellationToken);

            // 3. Group threads by the canonical "other user" (the user who is NOT the caller).
            //    Key = other-user ID; Value = list of thread IDs.
            var threadsByOtherUser = membersByThread
                .GroupBy(cm => cm.ChatThreadId)
                .Where(g =>
                {
                    var uids = g.Select(cm => cm.UserId).Distinct().ToList();
                    // Must be exactly 2 members and one of them must be userId
                    return uids.Count == 2 && uids.Contains(userId);
                })
                .Select(g => new
                {
                    ThreadId = g.Key,
                    OtherUserId = g.Select(cm => cm.UserId).First(id => id != userId)
                })
                .GroupBy(x => x.OtherUserId)
                .Where(g => g.Count() > 1)      // Only groups with actual duplicates
                .ToList();

            if (threadsByOtherUser.Count == 0) return result;

            // 4. Load UpdatedAt for ordering
            var allDupThreadIds = threadsByOtherUser
                .SelectMany(g => g.Select(x => x.ThreadId))
                .Distinct()
                .ToList();

            var threadMeta = await context.ChatThreads
                .Where(ct => allDupThreadIds.Contains(ct.Id))
                .Select(ct => new { ct.Id, ct.UpdatedAt })
                .ToDictionaryAsync(ct => ct.Id, ct => ct.UpdatedAt, cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var group in threadsByOtherUser)
            {
                // Pick the primary: the most recently updated thread
                var ordered = group
                    .OrderByDescending(x => threadMeta.TryGetValue(x.ThreadId, out var upd) ? upd : DateTime.MinValue)
                    .ToList();

                var primaryId = ordered[0].ThreadId;
                var secondaryIds = ordered.Skip(1).Select(x => x.ThreadId).ToList();

                // 5. Re-parent messages from secondary threads to the primary thread
                var messagesToMove = await context.ChatMessages
                    .Where(m => secondaryIds.Contains(m.ChatThreadId))
                    .ToListAsync(cancellationToken);

                foreach (var msg in messagesToMove)
                    msg.ChatThreadId = primaryId;

                result.ReassignedMessageCount += messagesToMove.Count;

                // 6. Move members from secondary threads that aren't already in the primary
                var existingPrimaryMemberIds = await context.ChatMembers
                    .Where(cm => cm.ChatThreadId == primaryId && !cm.IsDeleted)
                    .Select(cm => cm.UserId)
                    .ToListAsync(cancellationToken);

                var membersToMove = await context.ChatMembers
                    .Where(cm => secondaryIds.Contains(cm.ChatThreadId) && !cm.IsDeleted
                              && !existingPrimaryMemberIds.Contains(cm.UserId))
                    .ToListAsync(cancellationToken);

                foreach (var member in membersToMove)
                {
                    member.ChatThreadId = primaryId;
                    member.ModifiedAt = now;
                }

                // 7. Soft-delete secondary threads and their remaining members
                var secondaryThreads = await context.ChatThreads
                    .Where(ct => secondaryIds.Contains(ct.Id))
                    .ToListAsync(cancellationToken);

                foreach (var thread in secondaryThreads)
                {
                    thread.IsActive = false;
                    thread.IsDeleted = true;
                    thread.ModifiedAt = now;
                }

                var remainingSecondaryMembers = await context.ChatMembers
                    .Where(cm => secondaryIds.Contains(cm.ChatThreadId) && !cm.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var member in remainingSecondaryMembers)
                {
                    member.IsDeleted = true;
                    member.IsActive = false;
                    member.ModifiedAt = now;
                }

                result.MergedThreadCount += secondaryIds.Count;
                result.PrimaryThreadIds.Add(primaryId);

                logger.LogInformation(
                    "Merged {SecondaryCount} duplicate DM threads into primary {PrimaryId} for user {UserId} (tenant {TenantId}). Moved {MessageCount} messages.",
                    secondaryIds.Count, primaryId, userId, tenantId, messagesToMove.Count);
            }

            await context.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error merging duplicate DM threads for user {UserId}", userId);
            throw;
        }
    }

}
