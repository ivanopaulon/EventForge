using EventForge.DTOs.Chat;

namespace EventForge.Server.Services.Chat;

/// <summary>
/// Service interface for comprehensive chat and messaging functionality with multi-tenant support.
/// Handles 1:1 and group chat, file/media attachments, message status tracking,
/// SuperAdmin moderation, group creation, localization, rate limiting, and accessibility.
/// 
/// This interface is designed for future extensibility with:
/// - Advanced media processing and content analysis
/// - AI-powered content moderation and translation
/// - Voice and video calling integration
/// - Bot and automation framework
/// - Advanced search and indexing capabilities
/// - External chat platform integrations
/// - End-to-end encryption support
/// - Advanced analytics and insights
/// </summary>
public interface IChatService
{
    #region Chat Thread Management

    /// <summary>
    /// Creates a new chat thread (direct message or group) with comprehensive validation.
    /// Supports automatic member addition, permission setup, and notification dispatch.
    /// 
    /// Future extensions: Chat templates, automated group setup workflows,
    /// AI-powered member suggestions, integration with organization charts,
    /// custom chat themes and branding, advanced permission models.
    /// </summary>
    /// <param name="createChatDto">Chat creation parameters with member list</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created chat with member details and initial configuration</returns>
    Task<ChatResponseDto> CreateChatAsync(
        CreateChatDto createChatDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed chat information including members, recent messages, and metadata.
    /// Supports permission-based data filtering and real-time status updates.
    /// 
    /// Future extensions: Chat analytics, activity summaries, member presence,
    /// custom fields, integration data, pinned messages, chat insights.
    /// </summary>
    /// <param name="chatId">Chat identifier</param>
    /// <param name="userId">Requesting user for permission validation</param>
    /// <param name="tenantId">Tenant context for multi-tenant isolation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat details or null if not accessible</returns>
    Task<ChatResponseDto?> GetChatByIdAsync(
        Guid chatId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches and filters user's chats with advanced criteria and pagination.
    /// Supports full-text search, activity-based sorting, and smart categorization.
    /// 
    /// Future extensions: AI-powered chat categorization, semantic search,
    /// custom filters, saved searches, chat recommendations, activity insights.
    /// </summary>
    /// <param name="searchDto">Search criteria with filtering options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated chat results with activity metadata</returns>
    Task<PagedResult<ChatResponseDto>> SearchChatsAsync(
        ChatSearchDto searchDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates chat properties including name, description, and settings.
    /// Includes permission validation and change notification to members.
    /// 
    /// Future extensions: Workflow-based approval for changes, change templates,
    /// automated change notifications, integration with external systems.
    /// </summary>
    /// <param name="chatId">Chat to update</param>
    /// <param name="updateDto">Update parameters</param>
    /// <param name="userId">User performing the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated chat information</returns>
    Task<ChatResponseDto> UpdateChatAsync(
        Guid chatId,
        UpdateChatDto updateDto,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives or deletes a chat with comprehensive cleanup and notification.
    /// Supports soft deletion, data retention policies, and member notification.
    /// 
    /// Future extensions: Graduated deletion policies, export before deletion,
    /// legal hold support, automated archival workflows, recovery mechanisms.
    /// </summary>
    /// <param name="chatId">Chat to delete</param>
    /// <param name="userId">User performing the deletion</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="softDelete">Whether to soft delete (true) or hard delete (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion operation result</returns>
    Task<ChatOperationResultDto> DeleteChatAsync(
        Guid chatId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        CancellationToken cancellationToken = default);

    #endregion

    #region Message Management

    /// <summary>
    /// Sends a message in a chat with comprehensive validation and delivery tracking.
    /// Supports rich content, attachments, threading, and real-time delivery.
    /// 
    /// Future extensions: Message templates, AI-powered content suggestions,
    /// advanced formatting, message scheduling, auto-translation, content analysis.
    /// </summary>
    /// <param name="messageDto">Message content and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sent message with delivery status and metadata</returns>
    Task<ChatMessageDto> SendMessageAsync(
        SendMessageDto messageDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves chat messages with filtering, pagination, and permission validation.
    /// Supports thread navigation, search within conversations, and content filtering.
    /// 
    /// Future extensions: Semantic search, message clustering, content summaries,
    /// AI-powered insights, custom views, export capabilities, analytics.
    /// </summary>
    /// <param name="searchDto">Message search and filtering criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated message results with context information</returns>
    Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        MessageSearchDto searchDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all chat messages with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of chat messages</returns>
    Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves messages for a specific conversation.
    /// </summary>
    /// <param name="conversationId">Conversation/Chat thread ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of messages for the conversation</returns>
    Task<PagedResult<ChatMessageDto>> GetMessagesByConversationAsync(
        Guid conversationId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves unread messages for the current user.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of unread messages</returns>
    Task<PagedResult<ChatMessageDto>> GetUnreadMessagesAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific message by ID with access validation and context.
    /// Includes thread context, attachments, and read receipt information.
    /// 
    /// Future extensions: Message analytics, related message suggestions,
    /// content analysis results, translation variants, edit history.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="userId">Requesting user for access validation</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message details or null if not accessible</returns>
    Task<ChatMessageDto?> GetMessageByIdAsync(
        Guid messageId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing message with validation and change tracking.
    /// Supports edit history, permission validation, and member notification.
    /// 
    /// Future extensions: Collaborative editing, change suggestions,
    /// approval workflows for edits, edit conflict resolution, version control.
    /// </summary>
    /// <param name="editDto">Message edit parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated message with edit metadata</returns>
    Task<ChatMessageDto> EditMessageAsync(
        EditMessageDto editDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message with soft/hard delete options and comprehensive logging.
    /// Supports cascade deletion of attachments and notification cleanup.
    /// 
    /// Future extensions: Bulk deletion, automated cleanup policies,
    /// legal hold support, recovery mechanisms, deletion workflows.
    /// </summary>
    /// <param name="messageId">Message to delete</param>
    /// <param name="userId">User performing the deletion</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="softDelete">Whether to soft delete (true) or hard delete (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion operation result</returns>
    Task<MessageOperationResultDto> DeleteMessageAsync(
        Guid messageId,
        Guid userId,
        string? reason = null,
        bool softDelete = true,
        CancellationToken cancellationToken = default);

    #endregion

    #region Message Status & Read Receipts

    /// <summary>
    /// Updates message delivery status with real-time notification.
    /// Supports delivery confirmation, failure tracking, and retry mechanisms.
    /// 
    /// Future extensions: Advanced delivery analytics, delivery optimization,
    /// custom delivery channels, integration with external messaging systems.
    /// </summary>
    /// <param name="messageId">Message to update</param>
    /// <param name="status">New delivery status</param>
    /// <param name="userId">User for whom to update status</param>
    /// <param name="metadata">Additional status metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated message status information</returns>
    Task<MessageStatusUpdateResultDto> UpdateMessageStatusAsync(
        Guid messageId,
        MessageStatus status,
        Guid userId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as read by a user with timestamp tracking.
    /// Updates read receipts and triggers delivery confirmations.
    /// 
    /// Future extensions: Smart read tracking, attention analytics,
    /// reading pattern analysis, automated insights, privacy controls.
    /// </summary>
    /// <param name="messageId">Message to mark as read</param>
    /// <param name="userId">User who read the message</param>
    /// <param name="readAt">Optional custom read timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated read receipt information</returns>
    Task<MessageReadReceiptDto> MarkMessageAsReadAsync(
        Guid messageId,
        Guid userId,
        DateTime? readAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive read receipt information for a message.
    /// Includes detailed user read status and timing analytics.
    /// 
    /// Future extensions: Read pattern analytics, engagement metrics,
    /// privacy-preserving read tracking, aggregated insights.
    /// </summary>
    /// <param name="messageId">Message to get read receipts for</param>
    /// <param name="requestingUserId">User requesting the information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Read receipt information with user details</returns>
    Task<List<MessageReadReceiptDto>> GetMessageReadReceiptsAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk marks multiple messages as read for efficient batch processing.
    /// Supports conversation-level read status updates with optimization.
    /// 
    /// Future extensions: Smart bulk reading, conversation-aware batching,
    /// read status synchronization across devices, offline read tracking.
    /// </summary>
    /// <param name="messageIds">Messages to mark as read</param>
    /// <param name="userId">User performing the bulk read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk read operation results</returns>
    Task<BulkReadResultDto> BulkMarkAsReadAsync(
        List<Guid> messageIds,
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region File & Media Management

    /// <summary>
    /// Uploads and processes file attachments with comprehensive validation.
    /// Supports multiple file types, virus scanning, content analysis, and optimization.
    /// 
    /// Future extensions: AI-powered content analysis, automatic transcription,
    /// thumbnail generation, format conversion, cloud storage integration,
    /// advanced security scanning, content moderation.
    /// </summary>
    /// <param name="uploadDto">File upload parameters and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file metadata and access information</returns>
    Task<FileUploadResultDto> UploadFileAsync(
        FileUploadDto uploadDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets secure file download information with access validation.
    /// Supports time-limited URLs, access logging, and download tracking.
    /// 
    /// Future extensions: Advanced access controls, watermarking,
    /// download analytics, integration with DRM systems, audit trails.
    /// </summary>
    /// <param name="attachmentId">File attachment identifier</param>
    /// <param name="userId">User requesting download access</param>
    /// <param name="tenantId">Tenant context for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secure download information or null if not accessible</returns>
    Task<FileDownloadInfoDto?> GetFileDownloadInfoAsync(
        Guid attachmentId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes media files for optimization and thumbnail generation.
    /// Supports various media formats with configurable processing pipelines.
    /// 
    /// Future extensions: AI-powered media analysis, content tagging,
    /// format optimization, adaptive streaming, advanced compression.
    /// </summary>
    /// <param name="attachmentId">Media attachment to process</param>
    /// <param name="processingOptions">Processing configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Media processing results with generated variants</returns>
    Task<MediaProcessingResultDto> ProcessMediaAsync(
        Guid attachmentId,
        MediaProcessingOptionsDto processingOptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes file attachments with cleanup and cascade operations.
    /// Supports soft deletion, orphan cleanup, and storage optimization.
    /// 
    /// Future extensions: Advanced cleanup policies, storage analytics,
    /// automated archival, legal hold support, recovery mechanisms.
    /// </summary>
    /// <param name="attachmentId">File attachment to delete</param>
    /// <param name="userId">User performing the deletion</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion operation result</returns>
    Task<FileOperationResultDto> DeleteFileAsync(
        Guid attachmentId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Chat Member Management

    /// <summary>
    /// Adds new members to a chat with validation and notification.
    /// Supports bulk member addition, role assignment, and welcome workflows.
    /// 
    /// Future extensions: Invitation workflows, approval processes,
    /// automated onboarding, integration with directory services,
    /// member suggestions based on AI analysis.
    /// </summary>
    /// <param name="chatId">Chat to add members to</param>
    /// <param name="userIds">Users to add as members</param>
    /// <param name="addedBy">User performing the addition</param>
    /// <param name="defaultRole">Default role for new members</param>
    /// <param name="welcomeMessage">Optional welcome message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Member addition operation results</returns>
    Task<MemberOperationResultDto> AddMembersAsync(
        Guid chatId,
        List<Guid> userIds,
        Guid addedBy,
        ChatMemberRole defaultRole = ChatMemberRole.Member,
        string? welcomeMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes members from a chat with notification and cleanup.
    /// Supports graceful member removal with data retention options.
    /// 
    /// Future extensions: Exit interview workflows, data anonymization,
    /// automated cleanup, member exit analytics, re-invitation policies.
    /// </summary>
    /// <param name="chatId">Chat to remove members from</param>
    /// <param name="userIds">Users to remove</param>
    /// <param name="removedBy">User performing the removal</param>
    /// <param name="reason">Reason for removal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Member removal operation results</returns>
    Task<MemberOperationResultDto> RemoveMembersAsync(
        Guid chatId,
        List<Guid> userIds,
        Guid removedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates member roles and permissions with validation.
    /// Supports bulk role updates and permission inheritance.
    /// 
    /// Future extensions: Custom permission models, role templates,
    /// approval workflows for role changes, role analytics, audit trails.
    /// </summary>
    /// <param name="chatId">Chat to update member roles in</param>
    /// <param name="roleUpdates">User ID to role mappings</param>
    /// <param name="updatedBy">User performing the updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role update operation results</returns>
    Task<MemberOperationResultDto> UpdateMemberRolesAsync(
        Guid chatId,
        Dictionary<Guid, ChatMemberRole> roleUpdates,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive member information for a chat.
    /// Includes role details, activity status, and permission information.
    /// 
    /// Future extensions: Member analytics, activity insights,
    /// presence information, custom member fields, integration data.
    /// </summary>
    /// <param name="chatId">Chat to get members for</param>
    /// <param name="requestingUserId">User requesting member information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed member information list</returns>
    Task<List<ChatMemberDto>> GetChatMembersAsync(
        Guid chatId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Rate Limiting & Multi-Tenant Management

    /// <summary>
    /// Checks if chat operations are allowed under current rate limits.
    /// Supports tenant-specific, user-specific, and operation-specific limits.
    /// 
    /// Future extensions: Dynamic rate limiting, intelligent throttling,
    /// priority-based limits, usage analytics, predictive scaling.
    /// </summary>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="userId">User context</param>
    /// <param name="operationType">Type of chat operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit status and quota information</returns>
    Task<ChatRateLimitStatusDto> CheckChatRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        ChatOperationType operationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates chat rate limiting policies for tenants.
    /// Supports granular policy configuration and immediate effect.
    /// 
    /// Future extensions: Policy templates, inheritance models,
    /// dynamic policy adjustment, compliance integration.
    /// </summary>
    /// <param name="tenantId">Tenant to update policies for</param>
    /// <param name="rateLimitPolicy">New rate limiting policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Applied rate limiting policy</returns>
    Task<ChatRateLimitPolicyDto> UpdateTenantChatRateLimitAsync(
        Guid tenantId,
        ChatRateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive chat statistics and analytics.
    /// Supports real-time metrics, historical analysis, and trend identification.
    /// 
    /// Future extensions: Predictive analytics, anomaly detection,
    /// custom dashboards, automated insights, performance optimization.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="dateRange">Date range for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive chat analytics and metrics</returns>
    Task<ChatStatsDto> GetChatStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Moderation & Administration

    /// <summary>
    /// Performs moderation actions on chats and messages (Admin/SuperAdmin).
    /// Supports content review, user warnings, and automatic enforcement.
    /// 
    /// Future extensions: AI-powered content moderation, automated workflows,
    /// escalation procedures, community moderation, appeals process.
    /// </summary>
    /// <param name="moderationAction">Moderation action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Moderation action results</returns>
    Task<ModerationResultDto> ModerateChatAsync(
        ChatModerationActionDto moderationAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed audit trail for chat operations with security context.
    /// Supports compliance reporting, investigation tools, and forensic analysis.
    /// 
    /// Future extensions: Advanced analytics, pattern detection,
    /// automated compliance reporting, security insights, threat analysis.
    /// </summary>
    /// <param name="auditQuery">Audit query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit trail results</returns>
    Task<PagedResult<ChatAuditEntryDto>> GetChatAuditTrailAsync(
        ChatAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors chat system health with real-time alerting.
    /// Includes performance metrics, error rates, and capacity monitoring.
    /// 
    /// Future extensions: Predictive health monitoring, auto-scaling,
    /// intelligent alerting, performance optimization, capacity planning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat system health status and metrics</returns>
    Task<ChatSystemHealthDto> GetChatSystemHealthAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Localization & Accessibility

    /// <summary>
    /// Localizes chat content based on user preferences and context.
    /// Supports real-time translation, cultural adaptation, and accessibility.
    /// 
    /// Future extensions: AI-powered translation, cultural context adaptation,
    /// accessibility optimization, regional compliance, content personalization.
    /// </summary>
    /// <param name="message">Message to localize</param>
    /// <param name="targetLocale">Target locale</param>
    /// <param name="userId">User context for personalization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Localized message content</returns>
    Task<ChatMessageDto> LocalizeChatMessageAsync(
        ChatMessageDto message,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates chat localization preferences for users.
    /// Supports per-chat language settings and global preferences.
    /// 
    /// Future extensions: Smart language detection, preference inheritance,
    /// context-aware localization, accessibility preferences.
    /// </summary>
    /// <param name="userId">User to update preferences for</param>
    /// <param name="preferences">Localization preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated localization preferences</returns>
    Task<ChatLocalizationPreferencesDto> UpdateChatLocalizationAsync(
        Guid userId,
        ChatLocalizationPreferencesDto preferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle message reaction (add or remove).
    /// </summary>
    /// <param name="reactionDto">Reaction action details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with updated reaction state</returns>
    Task<MessageOperationResultDto> ToggleMessageReactionAsync(
        MessageReactionActionDto reactionDto,
        CancellationToken cancellationToken = default);

    #endregion
}

#region Supporting DTOs for Future Extensions

/// <summary>
/// Date range helper for queries and statistics.
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Chat operation result with detailed status information.
/// </summary>
public class ChatOperationResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Message operation result with status tracking.
/// </summary>
public class MessageOperationResultDto
{
    public Guid MessageId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public MessageStatus? NewStatus { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Message status update result.
/// </summary>
public class MessageStatusUpdateResultDto
{
    public Guid MessageId { get; set; }
    public MessageStatus Status { get; set; }
    public Guid UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Bulk read operation result.
/// </summary>
public class BulkReadResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<Guid> ProcessedMessageIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// File upload parameters.
/// </summary>
public class FileUploadDto
{
    public Guid ChatId { get; set; }
    public Guid UploadedBy { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Stream FileStream { get; set; } = Stream.Null;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// File upload result with access information.
/// </summary>
public class FileUploadResultDto
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public MediaType MediaType { get; set; }
    public long FileSize { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// File download information with secure access.
/// </summary>
public class FileDownloadInfoDto
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Media processing options.
/// </summary>
public class MediaProcessingOptionsDto
{
    public bool GenerateThumbnails { get; set; } = true;
    public bool OptimizeForWeb { get; set; } = true;
    public List<string> OutputFormats { get; set; } = new();
    public Dictionary<string, object>? CustomOptions { get; set; }
}

/// <summary>
/// Media processing result with generated variants.
/// </summary>
public class MediaProcessingResultDto
{
    public Guid AttachmentId { get; set; }
    public bool Success { get; set; }
    public List<MediaVariantDto> GeneratedVariants { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Media variant information.
/// </summary>
public class MediaVariantDto
{
    public string VariantType { get; set; } = string.Empty; // thumbnail, optimized, etc.
    public string Url { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Dictionary<string, object>? Properties { get; set; } // dimensions, duration, etc.
}

/// <summary>
/// File operation result.
/// </summary>
public class FileOperationResultDto
{
    public Guid AttachmentId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Member operation result with detailed status.
/// </summary>
public class MemberOperationResultDto
{
    public Guid ChatId { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<MemberOperationDetail> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual member operation detail.
/// </summary>
public class MemberOperationDetail
{
    public Guid UserId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ChatMemberRole? AssignedRole { get; set; }
}

/// <summary>
/// Chat operation types for rate limiting.
/// </summary>
public enum ChatOperationType
{
    SendMessage,
    CreateChat,
    AddMember,
    UploadFile,
    EditMessage,
    DeleteMessage
}

/// <summary>
/// Chat rate limiting status.
/// </summary>
public class ChatRateLimitStatusDto
{
    public bool IsAllowed { get; set; }
    public int RemainingQuota { get; set; }
    public TimeSpan ResetTime { get; set; }
    public ChatOperationType OperationType { get; set; }
    public Dictionary<string, object>? LimitDetails { get; set; }
}

/// <summary>
/// Chat rate limiting policy.
/// </summary>
public class ChatRateLimitPolicyDto
{
    public Guid TenantId { get; set; }
    public Dictionary<ChatOperationType, int> LimitsPerOperation { get; set; } = new();
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromHours(1);
    public int GlobalMessageLimit { get; set; }
    public long MaxFileSize { get; set; }
    public int MaxFilesPerMessage { get; set; }
}

/// <summary>
/// Moderation result with action details.
/// </summary>
public class ModerationResultDto
{
    public bool Success { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public List<string> AffectedItems { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Chat audit query parameters.
/// </summary>
public class ChatAuditQueryDto
{
    public Guid? TenantId { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? Operations { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Chat audit trail entry.
/// </summary>
public class ChatAuditEntryDto
{
    public Guid Id { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Chat system health monitoring.
/// </summary>
public class ChatSystemHealthDto
{
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Alerts { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Chat localization preferences.
/// </summary>
public class ChatLocalizationPreferencesDto
{
    public Guid UserId { get; set; }
    public string PreferredLocale { get; set; } = "en-US";
    public bool AutoTranslate { get; set; }
    public List<string> TranslationLanguages { get; set; } = new();
    public bool ShowOriginalText { get; set; } = true;
    public Dictionary<Guid, string> ChatSpecificLocales { get; set; } = new(); // ChatId -> Locale
}

/// <summary>
/// Chat update parameters.
/// </summary>
public class UpdateChatDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
    public string? PreferredLocale { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion