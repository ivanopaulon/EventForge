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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsAsync(
        Guid documentHeaderId, 
        bool includeHistory = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document attachment
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created attachment DTO</returns>
    Task<DocumentAttachmentDto> CreateAttachmentAsync(
        Guid documentHeaderId,
        CreateDocumentAttachmentDto createDto, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    // Comment operations
    /// <summary>
    /// Gets all comments for a document header
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    Task<IEnumerable<DocumentCommentDto>> GetCommentsAsync(
        Guid documentHeaderId, 
        bool includeReplies = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document comment
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created comment DTO</returns>
    Task<DocumentCommentDto> CreateCommentAsync(
        Guid documentHeaderId,
        CreateDocumentCommentDto createDto, 
        string currentUser, 
        CancellationToken cancellationToken = default);

    // Template operations
    /// <summary>
    /// Gets public document templates available to all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of public document template DTOs</returns>
    Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document template by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template DTO or null if not found</returns>
    Task<DocumentTemplateDto?> GetTemplateByIdAsync(
        Guid templateId, 
        CancellationToken cancellationToken = default);

    // Workflow operations
    /// <summary>
    /// Gets document workflows for a document type or all workflows
    /// </summary>
    /// <param name="documentTypeId">Optional document type ID to filter workflows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflow DTOs</returns>
    Task<IEnumerable<DocumentWorkflowDto>> GetWorkflowsAsync(
        Guid? documentTypeId = null, 
        CancellationToken cancellationToken = default);

    // Analytics operations
    /// <summary>
    /// Gets analytics for a specific document
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics DTO or null if not found</returns>
    Task<DocumentAnalyticsDto?> GetAnalyticsAsync(
        Guid documentHeaderId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates analytics for a document (refresh operation)
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics DTO</returns>
    Task<DocumentAnalyticsDto> RefreshAnalyticsAsync(
        Guid documentHeaderId, 
        string currentUser, 
        CancellationToken cancellationToken = default);
}