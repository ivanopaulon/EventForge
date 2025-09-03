using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Facade implementation that orchestrates document-related services for the unified DocumentsController.
/// Delegates operations to existing specialized services without adding new business logic.
/// </summary>
public class DocumentFacade : IDocumentFacade
{
    private readonly IDocumentAttachmentService _attachmentService;
    private readonly IDocumentCommentService _commentService;
    private readonly IDocumentTemplateService _templateService;
    private readonly IDocumentWorkflowService _workflowService;
    private readonly IDocumentAnalyticsService _analyticsService;

    /// <summary>
    /// Initializes a new instance of the DocumentFacade
    /// </summary>
    /// <param name="attachmentService">Document attachment service</param>
    /// <param name="commentService">Document comment service</param>
    /// <param name="templateService">Document template service</param>
    /// <param name="workflowService">Document workflow service</param>
    /// <param name="analyticsService">Document analytics service</param>
    public DocumentFacade(
        IDocumentAttachmentService attachmentService,
        IDocumentCommentService commentService,
        IDocumentTemplateService templateService,
        IDocumentWorkflowService workflowService,
        IDocumentAnalyticsService analyticsService)
    {
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    // Attachment operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsAsync(
        Guid documentHeaderId, 
        bool includeHistory = false, 
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.GetDocumentHeaderAttachmentsAsync(documentHeaderId, includeHistory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto> CreateAttachmentAsync(
        Guid documentHeaderId,
        CreateDocumentAttachmentDto createDto, 
        string currentUser, 
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.CreateAttachmentAsync(createDto, currentUser, cancellationToken);
    }

    // Comment operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentsAsync(
        Guid documentHeaderId, 
        bool includeReplies = true, 
        CancellationToken cancellationToken = default)
    {
        return await _commentService.GetDocumentHeaderCommentsAsync(documentHeaderId, includeReplies, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto> CreateCommentAsync(
        Guid documentHeaderId,
        CreateDocumentCommentDto createDto, 
        string currentUser, 
        CancellationToken cancellationToken = default)
    {
        return await _commentService.CreateCommentAsync(createDto, currentUser, cancellationToken);
    }

    // Template operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _templateService.GetPublicTemplatesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> GetTemplateByIdAsync(
        Guid templateId, 
        CancellationToken cancellationToken = default)
    {
        return await _templateService.GetByIdAsync(templateId, cancellationToken);
    }

    // Workflow operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetWorkflowsAsync(
        Guid? documentTypeId = null, 
        CancellationToken cancellationToken = default)
    {
        if (documentTypeId.HasValue)
        {
            return await _workflowService.GetByDocumentTypeAsync(documentTypeId.Value, cancellationToken);
        }
        
        return await _workflowService.GetAllAsync(cancellationToken);
    }

    // Analytics operations
    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto?> GetAnalyticsAsync(
        Guid documentHeaderId, 
        CancellationToken cancellationToken = default)
    {
        return await _analyticsService.GetDocumentAnalyticsAsync(documentHeaderId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto> RefreshAnalyticsAsync(
        Guid documentHeaderId, 
        string currentUser, 
        CancellationToken cancellationToken = default)
    {
        return await _analyticsService.CreateOrUpdateAnalyticsAsync(documentHeaderId, currentUser, cancellationToken);
    }
}