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
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IDocumentTypeService _documentTypeService;
    private readonly IDocumentStatusService _documentStatusService;
    private readonly ILogger<DocumentFacade> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentFacade
    /// </summary>
    /// <param name="attachmentService">Document attachment service</param>
    /// <param name="commentService">Document comment service</param>
    /// <param name="templateService">Document template service</param>
    /// <param name="workflowService">Document workflow service</param>
    /// <param name="analyticsService">Document analytics service</param>
    /// <param name="documentHeaderService">Document header service</param>
    /// <param name="documentTypeService">Document type service</param>
    /// <param name="documentStatusService">Document status service</param>
    /// <param name="logger">Logger instance</param>
    public DocumentFacade(
        IDocumentAttachmentService attachmentService,
        IDocumentCommentService commentService,
        IDocumentTemplateService templateService,
        IDocumentWorkflowService workflowService,
        IDocumentAnalyticsService analyticsService,
        IDocumentHeaderService documentHeaderService,
        IDocumentTypeService documentTypeService,
        IDocumentStatusService documentStatusService,
        ILogger<DocumentFacade> logger)
    {
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
        _documentStatusService = documentStatusService ?? throw new ArgumentNullException(nameof(documentStatusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        CreateDocumentAttachmentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.CreateAttachmentAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetDocumentRowAttachmentsAsync(
        Guid documentRowId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.GetDocumentRowAttachmentsAsync(documentRowId, includeHistory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.GetAttachmentByIdAsync(attachmentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> UpdateAttachmentAsync(
        Guid attachmentId,
        UpdateDocumentAttachmentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.UpdateAttachmentAsync(attachmentId, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> CreateAttachmentVersionAsync(
        Guid attachmentId,
        AttachmentVersionDto versionDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.CreateAttachmentVersionAsync(attachmentId, versionDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentVersionsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.GetAttachmentVersionsAsync(attachmentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> SignAttachmentAsync(
        Guid attachmentId,
        string signatureInfo,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.SignAttachmentAsync(attachmentId, signatureInfo, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.GetAttachmentsByCategoryAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.DeleteAttachmentAsync(attachmentId, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AttachmentExistsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentService.AttachmentExistsAsync(attachmentId, cancellationToken);
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
        CreateDocumentCommentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.CreateCommentAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetDocumentRowCommentsAsync(
        Guid documentRowId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.GetDocumentRowCommentsAsync(documentRowId, includeReplies, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> GetCommentByIdAsync(
        Guid commentId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.GetCommentByIdAsync(commentId, includeReplies, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> UpdateCommentAsync(
        Guid commentId,
        UpdateDocumentCommentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.UpdateCommentAsync(commentId, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ResolveCommentAsync(
        Guid commentId,
        ResolveCommentDto resolveDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.ResolveCommentAsync(commentId, resolveDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ReopenCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.ReopenCommentAsync(commentId, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentCommentStatsDto> GetDocumentCommentStatsAsync(
        Guid documentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.GetDocumentCommentStatsAsync(documentId, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetAssignedCommentsAsync(
        string userId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.GetAssignedCommentsAsync(userId, status, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.DeleteCommentAsync(commentId, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CommentExistsAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        return await _commentService.CommentExistsAsync(commentId, cancellationToken);
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

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _templateService.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByDocumentTypeAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.GetByDocumentTypeAsync(documentTypeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.GetByCategoryAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto> CreateTemplateAsync(
        CreateDocumentTemplateDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.CreateAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> UpdateTemplateAsync(
        Guid templateId,
        UpdateDocumentTemplateDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.UpdateAsync(templateId, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.DeleteAsync(templateId, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTemplateUsageAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _templateService.UpdateUsageAsync(templateId, currentUser, cancellationToken);
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

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _workflowService.GetByIdAsync(workflowId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto> CreateWorkflowAsync(
        CreateDocumentWorkflowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _workflowService.CreateAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> UpdateWorkflowAsync(
        Guid workflowId,
        UpdateDocumentWorkflowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _workflowService.UpdateAsync(workflowId, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteWorkflowAsync(
        Guid workflowId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _workflowService.DeleteAsync(workflowId, currentUser, cancellationToken);
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

    /// <inheritdoc />
    public async Task<DocumentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        return await _analyticsService.GetAnalyticsSummaryAsync(from, to, groupBy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentKpiSummaryDto> CalculateKpiSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _analyticsService.CalculateKpiSummaryAsync(from, to, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto> HandleWorkflowEventAsync(
        Guid documentId,
        string eventType,
        object? eventData,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _analyticsService.HandleWorkflowEventAsync(documentId, eventType, eventData, currentUser, cancellationToken);
    }

    // Document Header operations
    /// <inheritdoc />
    public async Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.UpdateDocumentHeaderAsync(id, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.DeleteDocumentHeaderAsync(id, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.CalculateDocumentTotalsAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.ApproveDocumentAsync(id, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.DocumentHeaderExistsAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.AddDocumentRowAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.UpdateDocumentRowAsync(rowId, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        CancellationToken cancellationToken = default)
    {
        return await _documentHeaderService.DeleteDocumentRowAsync(rowId, cancellationToken);
    }

    // Document Type operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _documentTypeService.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _documentTypeService.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        return await _documentTypeService.CreateAsync(createDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        return await _documentTypeService.UpdateAsync(id, updateDto, currentUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentTypeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        return await _documentTypeService.DeleteAsync(id, currentUser, cancellationToken);
    }

    // Document Status operations
    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await _documentStatusService.ChangeStatusAsync(documentId, newStatus, reason, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _documentStatusService.GetStatusHistoryAsync(documentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _documentStatusService.GetAvailableTransitionsAsync(documentId, cancellationToken);
    }
}