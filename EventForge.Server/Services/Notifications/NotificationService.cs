using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;

namespace EventForge.Server.Services.Notifications;

/// <summary>
/// Notification service implementation with comprehensive multi-tenant support.
/// 
/// This implementation covers all core notification functionality using EF Core DB operations.
/// 
/// Key architectural patterns:
/// - Multi-tenant data isolation with tenant-aware queries
/// - Comprehensive audit logging for all operations
/// - Rate limiting with tenant-specific policies
/// - Localization support with culture-aware content
/// - Extensible design for future enhancements
/// 
/// Future implementation areas:
/// - External notification providers (email, SMS, push notifications)
/// - Advanced rate limiting with Redis/distributed caching
/// - Machine learning for notification optimization
/// - Real-time analytics and monitoring dashboards
/// - Custom notification templates and workflow engine
/// </summary>
public partial class NotificationService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<NotificationService> logger,
    IMemoryCache memoryCache,
    IHubContext<AppHub> hubContext) : INotificationService
{

    #region Private Helper Methods

    /// <summary>
    /// Validates tenant access for multi-tenant operations.
    /// Checks that the tenant exists and has not been soft-deleted.
    /// </summary>
    private async Task ValidateTenantAccessAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue)
            return;

        var exists = await context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId.Value && !t.IsDeleted, cancellationToken);

        if (!exists)
            throw new InvalidOperationException($"Tenant {tenantId.Value} not found or is inactive.");
    }

    /// <summary>
    /// Validates rate limiting before operations.
    /// Throws if the rate limit is exceeded for the given tenant/user/type combination.
    /// </summary>
    private async Task ValidateRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        NotificationTypes type,
        CancellationToken cancellationToken)
    {
        var rateLimitStatus = await CheckRateLimitAsync(tenantId, userId, type, cancellationToken);

        if (!rateLimitStatus.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Rate limit exceeded for tenant {tenantId}, user {userId}, type {type}. " +
                $"Quota resets in {rateLimitStatus.ResetTime}");
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Resolves a user's display name from the Users table (FirstName+LastName or Username).
    /// Returns "System" for null userId or when no user is found.
    /// </summary>
    private async Task<string> ResolveUserNameAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty) return "System";

        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => new { u.FirstName, u.LastName, u.Username })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return userId.Value.ToString("N")[..8];

        return !string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : user.Username;
    }

    /// <summary>
    /// Batch-resolves user display names for a set of user IDs.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, string>> ResolveUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var users = await context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Username })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => !string.IsNullOrEmpty(u.FirstName) || !string.IsNullOrEmpty(u.LastName)
                ? $"{u.FirstName} {u.LastName}".Trim()
                : u.Username);
    }

    #endregion

}
