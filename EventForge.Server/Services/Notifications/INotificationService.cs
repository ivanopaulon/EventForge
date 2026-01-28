using EventForge.DTOs.Common;
using EventForge.DTOs.Notifications;

namespace EventForge.Server.Services.Notifications;

/// <summary>
/// Service interface for comprehensive notification management with multi-tenant support.
/// Handles instant notifications, priority/type management, acknowledgments, silencing,
/// archiving, expiry, history, localization, rate limiting, and accessibility.
/// 
/// This interface is designed for future extensibility with:
/// - Advanced localization and internationalization
/// - Custom notification templates and theming
/// - External notification providers (email, SMS, push, webhooks)
/// - Advanced analytics and reporting
/// - Machine learning-based notification optimization
/// - Integration with external audit and compliance systems
/// </summary>
public interface INotificationService
{
    #region Core Notification Management

    /// <summary>
    /// Sends a notification to specified recipients with full multi-tenant isolation.
    /// Supports broadcast notifications, targeted messaging, and system-wide alerts.
    /// 
    /// Future extensions: Template engine, rich media content, custom delivery channels,
    /// A/B testing for notification effectiveness, dynamic recipient calculation.
    /// </summary>
    /// <param name="createDto">Notification creation parameters with localization support</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Created notification response with delivery status</returns>
    Task<NotificationResponseDto> SendNotificationAsync(
        CreateNotificationDto createDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends bulk notifications with optimized batch processing and rate limiting.
    /// Includes tenant-aware batching and failure recovery mechanisms.
    /// 
    /// Future extensions: Smart batching algorithms, priority queuing,
    /// auto-retry with exponential backoff, delivery status aggregation.
    /// </summary>
    /// <param name="notifications">List of notifications to send</param>
    /// <param name="batchSize">Maximum batch size for processing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation results with per-notification status</returns>
    Task<BulkNotificationResultDto> SendBulkNotificationsAsync(
        List<CreateNotificationDto> notifications,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notifications for a user with advanced filtering and pagination.
    /// Supports real-time updates and tenant-specific content filtering.
    /// 
    /// Future extensions: AI-powered content filtering, personalized ranking,
    /// smart categorization, sentiment analysis, content moderation.
    /// </summary>
    /// <param name="searchDto">Search criteria with multi-tenant filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated notification results with metadata</returns>
    Task<PagedResult<NotificationResponseDto>> GetNotificationsAsync(
        NotificationSearchDto searchDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all notifications for the current user with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of notifications</returns>
    Task<PagedResult<NotificationResponseDto>> GetNotificationsAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves unread notifications for the current user.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of unread notifications</returns>
    Task<PagedResult<NotificationResponseDto>> GetUnreadNotificationsAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notifications by type (Info, Warning, Error, Success).
    /// </summary>
    /// <param name="type">Notification type</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of notifications of the specified type</returns>
    Task<PagedResult<NotificationResponseDto>> GetNotificationsByTypeAsync(
        string type,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific notification by ID with tenant access validation.
    /// Includes comprehensive audit trail and access logging.
    /// </summary>
    /// <param name="notificationId">Notification identifier</param>
    /// <param name="userId">Requesting user ID for access control</param>
    /// <param name="tenantId">Tenant context for isolation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification details or null if not found/accessible</returns>
    Task<NotificationResponseDto?> GetNotificationByIdAsync(
        Guid notificationId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Status Management & User Actions

    /// <summary>
    /// Acknowledges a notification, marking it as read and acknowledged by the user.
    /// Includes audit logging and real-time notification updates to other user sessions.
    /// 
    /// Future extensions: Custom acknowledgment workflows, delegation support,
    /// acknowledgment analytics, automated follow-up actions.
    /// </summary>
    /// <param name="notificationId">Notification to acknowledge</param>
    /// <param name="userId">User performing the acknowledgment</param>
    /// <param name="reason">Optional reason for acknowledgment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated notification status</returns>
    Task<NotificationResponseDto> AcknowledgeNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Silences a notification to prevent further alerts while keeping it visible.
    /// Supports temporary and permanent silencing with configurable expiry.
    /// 
    /// Future extensions: Smart silencing based on user behavior, category-based
    /// silencing, conditional silencing rules, silencing inheritance.
    /// </summary>
    /// <param name="notificationId">Notification to silence</param>
    /// <param name="userId">User performing the action</param>
    /// <param name="reason">Reason for silencing</param>
    /// <param name="expiresAt">Optional expiry for temporary silencing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated notification status</returns>
    Task<NotificationResponseDto> SilenceNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a notification, moving it to long-term storage with searchability.
    /// Includes configurable retention policies and data lifecycle management.
    /// 
    /// Future extensions: Intelligent archiving based on importance, automated
    /// archiving policies, archive compression, external archive storage.
    /// </summary>
    /// <param name="notificationId">Notification to archive</param>
    /// <param name="userId">User performing the action</param>
    /// <param name="reason">Optional reason for archiving</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated notification status</returns>
    Task<NotificationResponseDto> ArchiveNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk status operations on multiple notifications with transaction support.
    /// Includes validation, access control, and comprehensive audit logging.
    /// 
    /// Future extensions: Conditional bulk operations, workflow automation,
    /// approval processes for bulk actions, impact analysis before execution.
    /// </summary>
    /// <param name="bulkAction">Bulk operation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation results with detailed status per notification</returns>
    Task<BulkNotificationResultDto> ProcessBulkActionAsync(
        BulkNotificationActionDto bulkAction,
        CancellationToken cancellationToken = default);

    #endregion

    #region Preferences & Localization

    /// <summary>
    /// Gets user notification preferences with tenant-specific defaults and overrides.
    /// Supports hierarchical preference inheritance and dynamic preference calculation.
    /// 
    /// Future extensions: AI-powered preference suggestions, context-aware defaults,
    /// preference synchronization across devices, privacy-preserving preferences.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User notification preferences</returns>
    Task<NotificationPreferencesDto> GetUserPreferencesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user notification preferences with validation and audit logging.
    /// Includes immediate effect application and preference conflict resolution.
    /// 
    /// Future extensions: Preference templates, bulk preference management,
    /// preference versioning, rollback capabilities, preference analytics.
    /// </summary>
    /// <param name="preferences">Updated preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved preferences with applied changes</returns>
    Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(
        NotificationPreferencesDto preferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Localizes notification content based on user/tenant preferences and context.
    /// Supports dynamic content translation, cultural adaptation, and accessibility.
    /// 
    /// Future extensions: AI-powered translation, cultural context adaptation,
    /// accessibility optimization, regional compliance, content personalization.
    /// </summary>
    /// <param name="notification">Notification to localize</param>
    /// <param name="targetLocale">Target locale</param>
    /// <param name="userId">User context for personalization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Localized notification content</returns>
    Task<NotificationResponseDto> LocalizeNotificationAsync(
        NotificationResponseDto notification,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Expiry & Cleanup Management

    /// <summary>
    /// Processes expired notifications with tenant-aware cleanup policies.
    /// Includes configurable retention, archiving, and deletion strategies.
    /// 
    /// Future extensions: Smart expiry based on importance, legal hold support,
    /// graduated expiry policies, external archival integration.
    /// </summary>
    /// <param name="tenantId">Optional tenant to process (null for all tenants)</param>
    /// <param name="batchSize">Maximum notifications to process per batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Expiry processing results with statistics</returns>
    Task<ExpiryProcessingResultDto> ProcessExpiredNotificationsAsync(
        Guid? tenantId = null,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up notification data based on retention policies and tenant settings.
    /// Supports selective cleanup, data anonymization, and compliance requirements.
    /// 
    /// Future extensions: GDPR compliance automation, selective data retention,
    /// automated anonymization, regulatory compliance reporting.
    /// </summary>
    /// <param name="cleanupPolicy">Cleanup policy parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup operation results</returns>
    Task<CleanupResultDto> CleanupNotificationDataAsync(
        NotificationCleanupPolicyDto cleanupPolicy,
        CancellationToken cancellationToken = default);

    #endregion

    #region Rate Limiting & Tenant Management

    /// <summary>
    /// Checks if notification sending is allowed under current rate limits.
    /// Supports tenant-specific, user-specific, and global rate limiting policies.
    /// 
    /// Future extensions: Dynamic rate limiting based on load, priority-based
    /// rate limiting, burst capacity management, adaptive rate limiting.
    /// </summary>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="userId">User context</param>
    /// <param name="notificationType">Type of notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit status and remaining quota</returns>
    Task<RateLimitStatusDto> CheckRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        NotificationTypes notificationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates rate limiting policies for tenants with immediate effect.
    /// Includes policy validation, conflict resolution, and audit logging.
    /// 
    /// Future extensions: Dynamic policy adjustment, policy templates,
    /// policy inheritance, exception handling, compliance integration.
    /// </summary>
    /// <param name="tenantId">Tenant to update</param>
    /// <param name="rateLimitPolicy">New rate limiting policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Applied rate limiting policy</returns>
    Task<RateLimitPolicyDto> UpdateTenantRateLimitAsync(
        Guid tenantId,
        RateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive notification statistics for analytics and monitoring.
    /// Supports real-time metrics, historical analysis, and predictive insights.
    /// 
    /// Future extensions: Real-time dashboards, predictive analytics,
    /// anomaly detection, performance optimization suggestions, trend analysis.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="dateRange">Date range for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive notification statistics</returns>
    Task<NotificationStatsDto> GetNotificationStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region System Administration & Monitoring

    /// <summary>
    /// Sends system-wide notifications for critical alerts and administrative messages.
    /// Includes emergency broadcast capabilities and priority override mechanisms.
    /// 
    /// Future extensions: Emergency alert integration, regulatory compliance alerts,
    /// cascading notification policies, multi-channel broadcast coordination.
    /// </summary>
    /// <param name="systemNotification">System notification parameters</param>
    /// <param name="adminUserId">Administrator performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System notification delivery results</returns>
    Task<SystemNotificationResultDto> SendSystemNotificationAsync(
        CreateSystemNotificationDto systemNotification,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed audit trail for notification operations with security context.
    /// Supports compliance reporting, security analysis, and operational monitoring.
    /// 
    /// Future extensions: Advanced security analytics, threat detection,
    /// compliance automation, audit data correlation, forensic analysis.
    /// </summary>
    /// <param name="auditQuery">Audit query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit trail results</returns>
    Task<PagedResult<NotificationAuditEntryDto>> GetNotificationAuditTrailAsync(
        NotificationAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors notification system health with real-time alerting.
    /// Includes performance metrics, error rates, and capacity monitoring.
    /// 
    /// Future extensions: Predictive health monitoring, auto-scaling triggers,
    /// intelligent alerting, performance optimization, capacity planning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health status and metrics</returns>
    Task<NotificationSystemHealthDto> GetSystemHealthAsync(
        CancellationToken cancellationToken = default);

    #endregion
}

#region Supporting DTOs for Future Extensions

/// <summary>
/// Result DTO for bulk notification operations with detailed status tracking.
/// </summary>
public class BulkNotificationResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<NotificationOperationResult> Results { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual notification operation result for bulk operations.
/// </summary>
public class NotificationOperationResult
{
    public Guid? NotificationId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Rate limiting status and quota information.
/// </summary>
public class RateLimitStatusDto
{
    public bool IsAllowed { get; set; }
    public int RemainingQuota { get; set; }
    public TimeSpan ResetTime { get; set; }
    public string? RateLimitType { get; set; }
    public Dictionary<string, object>? LimitDetails { get; set; }
}

/// <summary>
/// Rate limiting policy configuration for tenants.
/// </summary>
public class RateLimitPolicyDto
{
    public Guid TenantId { get; set; }
    public Dictionary<NotificationTypes, int> LimitsPerType { get; set; } = new();
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromHours(1);
    public int GlobalLimit { get; set; }
    public bool EnableBurstCapacity { get; set; }
    public int BurstLimit { get; set; }
}

/// <summary>
/// Date range helper for queries and statistics.
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// System notification creation parameters.
/// </summary>
public class CreateSystemNotificationDto : CreateNotificationDto
{
    public bool IsEmergency { get; set; }
    public List<Guid>? TargetTenants { get; set; }
    public bool OverrideUserPreferences { get; set; }
}

/// <summary>
/// System notification delivery results.
/// </summary>
public class SystemNotificationResultDto
{
    public Guid NotificationId { get; set; }
    public int TotalRecipients { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> DeliveryErrors { get; set; } = new();
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification audit query parameters.
/// </summary>
public class NotificationAuditQueryDto
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? Operations { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Notification audit trail entry.
/// </summary>
public class NotificationAuditEntryDto
{
    public Guid Id { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// System health monitoring DTO.
/// </summary>
public class NotificationSystemHealthDto
{
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Alerts { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Expiry processing results.
/// </summary>
public class ExpiryProcessingResultDto
{
    public int ProcessedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ArchivedCount { get; set; }
    public int DeletedCount { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Cleanup policy parameters.
/// </summary>
public class NotificationCleanupPolicyDto
{
    public Guid? TenantId { get; set; }
    public TimeSpan RetentionPeriod { get; set; }
    public bool IncludeArchived { get; set; }
    public bool AnonymizeData { get; set; }
    public List<NotificationTypes>? TypeFilter { get; set; }
}

/// <summary>
/// Cleanup operation results.
/// </summary>
public class CleanupResultDto
{
    public int CleanedCount { get; set; }
    public int AnonymizedCount { get; set; }
    public long FreedBytes { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion