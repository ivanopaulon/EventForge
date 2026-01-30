using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Facade interface that orchestrates document-related services for the unified DocumentsController.
/// Provides a simplified interface by delegating to existing specialized services.
/// </summary>
public interface IDocumentFacade
{
    // Attachment operations
    /// <summary>
    /// Gets all attachments for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of document attachments</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsAsync(
        Guid documentHeaderId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document attachment
    /// </summary>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created attachment DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAttachmentDto> CreateAttachmentAsync(
        CreateDocumentAttachmentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attachments for a document row
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of document attachments</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentAttachmentDto>> GetDocumentRowAttachmentsAsync(
        Guid documentRowId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document attachment by ID
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document attachment DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAttachmentDto?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document attachment
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="updateDto">Attachment update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated attachment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAttachmentDto?> UpdateAttachmentAsync(
        Guid attachmentId,
        UpdateDocumentAttachmentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an attachment
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="versionDto">Version data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated attachment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when versionDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAttachmentDto?> CreateAttachmentVersionAsync(
        Guid attachmentId,
        AttachmentVersionDto versionDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of an attachment
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of attachment versions</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentVersionsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a document attachment
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="signatureInfo">Signature information</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated attachment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when signatureInfo or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAttachmentDto?> SignAttachmentAsync(
        Guid attachmentId,
        string signatureInfo,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attachments by category
    /// </summary>
    /// <param name="category">Attachment category</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of attachments in the category</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document attachment
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an attachment exists
    /// </summary>
    /// <param name="attachmentId">Attachment unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> AttachmentExistsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    // Comment operations
    /// <summary>
    /// Gets all comments for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of document comments</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentCommentDto>> GetCommentsAsync(
        Guid documentHeaderId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document comment
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created comment DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentDto> CreateCommentAsync(
        CreateDocumentCommentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments for a document row
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of document comments</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentCommentDto>> GetDocumentRowCommentsAsync(
        Guid documentRowId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a comment by ID
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Comment DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentDto?> GetCommentByIdAsync(
        Guid commentId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="updateDto">Comment update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentDto?> UpdateCommentAsync(
        Guid commentId,
        UpdateDocumentCommentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a document comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="resolveDto">Resolution data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when resolveDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentDto?> ResolveCommentAsync(
        Guid commentId,
        ResolveCommentDto resolveDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens a resolved comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated comment DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentDto?> ReopenCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comment statistics for a document
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Comment statistics DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentCommentStatsDto> GetDocumentCommentStatsAsync(
        Guid documentId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comments assigned to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of assigned comments</returns>
    /// <exception cref="ArgumentNullException">Thrown when userId is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentCommentDto>> GetAssignedCommentsAsync(
        string userId,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a comment exists
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> CommentExistsAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    // Template operations
    /// <summary>
    /// Gets public document templates available to all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of public document template DTOs</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document template by ID
    /// </summary>
    /// <param name="templateId">Template unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document template DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTemplateDto?> GetTemplateByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all document templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of all document template DTOs</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentTemplateDto>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by document type
    /// </summary>
    /// <param name="documentTypeId">Document type unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of templates for the document type</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByDocumentTypeAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by category
    /// </summary>
    /// <param name="category">Template category</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of templates in the category</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document template
    /// </summary>
    /// <param name="createDto">Template creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created template DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTemplateDto> CreateTemplateAsync(
        CreateDocumentTemplateDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document template
    /// </summary>
    /// <param name="templateId">Template unique identifier</param>
    /// <param name="updateDto">Template update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated template DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTemplateDto?> UpdateTemplateAsync(
        Guid templateId,
        UpdateDocumentTemplateDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document template
    /// </summary>
    /// <param name="templateId">Template unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteTemplateAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates template usage statistics
    /// </summary>
    /// <param name="templateId">Template unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if updated, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UpdateTemplateUsageAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Workflow operations
    /// <summary>
    /// Gets document workflows for a document type or all workflows
    /// </summary>
    /// <param name="documentTypeId">Optional document type ID to filter workflows</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of document workflow DTOs</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentWorkflowDto>> GetWorkflowsAsync(
        Guid? documentTypeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow by ID
    /// </summary>
    /// <param name="workflowId">Workflow unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Workflow DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentWorkflowDto?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document workflow
    /// </summary>
    /// <param name="createDto">Workflow creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created workflow DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentWorkflowDto> CreateWorkflowAsync(
        CreateDocumentWorkflowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document workflow
    /// </summary>
    /// <param name="workflowId">Workflow unique identifier</param>
    /// <param name="updateDto">Workflow update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated workflow DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentWorkflowDto?> UpdateWorkflowAsync(
        Guid workflowId,
        UpdateDocumentWorkflowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document workflow
    /// </summary>
    /// <param name="workflowId">Workflow unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteWorkflowAsync(
        Guid workflowId,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Analytics operations
    /// <summary>
    /// Gets analytics for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Analytics DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAnalyticsDto?> GetAnalyticsAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates analytics for a document (refresh operation)
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated analytics DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAnalyticsDto> RefreshAnalyticsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics summary for a date range
    /// </summary>
    /// <param name="from">Start date (optional)</param>
    /// <param name="to">End date (optional)</param>
    /// <param name="groupBy">Group by field (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Analytics summary DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates KPI summary for a date range
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>KPI summary DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentKpiSummaryDto> CalculateKpiSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a workflow event for analytics
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="eventType">Event type</param>
    /// <param name="eventData">Event data (optional)</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated analytics DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventType or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentAnalyticsDto> HandleWorkflowEventAsync(
        Guid documentId,
        string eventType,
        object? eventData,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Document Header operations
    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated document headers</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryParameters is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document header by ID.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document header DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document headers by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of document headers</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created document header DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document header.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="updateDto">Document header update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document header (soft delete).
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates document totals (net, VAT, gross) for a document header.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document header with updated totals or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a document header.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document header exists.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a row to an existing document header.
    /// </summary>
    /// <param name="createDto">Document row creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created document row DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    /// <param name="rowId">Document row unique identifier</param>
    /// <param name="updateDto">Document row update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document row DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document row (soft delete).
    /// </summary>
    /// <param name="rowId">Document row unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        CancellationToken cancellationToken = default);

    // Document Type operations
    /// <summary>
    /// Gets all document types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of document type DTOs</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<DocumentTypeDto>> GetAllDocumentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document type by ID
    /// </summary>
    /// <param name="id">Document type unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document type DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document type
    /// </summary>
    /// <param name="createDto">Document type creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created document type DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTypeDto> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document type
    /// </summary>
    /// <param name="id">Document type unique identifier</param>
    /// <param name="updateDto">Document type update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document type DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document type (soft delete)
    /// </summary>
    /// <param name="id">Document type unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteDocumentTypeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Document Status operations
    /// <summary>
    /// Changes the status of a document
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="newStatus">New document status</param>
    /// <param name="reason">Optional reason for status change</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status history for a document
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of status history entries</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available status transitions for a document
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of available document statuses</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    #region Bulk Operations

    /// <summary>
    /// Performs a bulk approval operation on multiple documents in a single transaction.
    /// </summary>
    /// <param name="bulkApprovalDto">Bulk approval request data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the bulk approval operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when bulkApprovalDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<EventForge.DTOs.Bulk.BulkApprovalResultDto> BulkApproveAsync(
        EventForge.DTOs.Bulk.BulkApprovalDto bulkApprovalDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a bulk status change operation on multiple documents in a single transaction.
    /// </summary>
    /// <param name="bulkStatusChangeDto">Bulk status change request data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the bulk status change operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when bulkStatusChangeDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<EventForge.DTOs.Bulk.BulkStatusChangeResultDto> BulkStatusChangeAsync(
        EventForge.DTOs.Bulk.BulkStatusChangeDto bulkStatusChangeDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    #endregion
}