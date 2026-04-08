using EventForge.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Facade implementation that orchestrates document-related services for the unified DocumentsController.
/// Delegates operations to existing specialized services without adding new business logic.
/// </summary>
public class DocumentFacade(
    IDocumentAttachmentService attachmentService,
    IDocumentCommentService commentService,
    IDocumentTemplateService templateService,
    IDocumentWorkflowService workflowService,
    IDocumentAnalyticsService analyticsService,
    IDocumentHeaderService documentHeaderService,
    IDocumentTypeService documentTypeService,
    IDocumentStatusService documentStatusService,
    EventForgeDbContext context,
    ITenantContext tenantContext,
    ILogger<DocumentFacade> logger) : IDocumentFacade
{
    // Attachment operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsAsync(
        Guid documentHeaderId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.GetDocumentHeaderAttachmentsAsync(documentHeaderId, includeHistory, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAttachmentsAsync for {DocumentHeaderId}.", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto> CreateAttachmentAsync(
        CreateDocumentAttachmentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.CreateAttachmentAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateAttachmentAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetDocumentRowAttachmentsAsync(
        Guid documentRowId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.GetDocumentRowAttachmentsAsync(documentRowId, includeHistory, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentRowAttachmentsAsync for {DocumentRowId}.", documentRowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.GetAttachmentByIdAsync(attachmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAttachmentByIdAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> UpdateAttachmentAsync(
        Guid attachmentId,
        UpdateDocumentAttachmentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.UpdateAttachmentAsync(attachmentId, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateAttachmentAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> CreateAttachmentVersionAsync(
        Guid attachmentId,
        AttachmentVersionDto versionDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.CreateAttachmentVersionAsync(attachmentId, versionDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateAttachmentVersionAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentVersionsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.GetAttachmentVersionsAsync(attachmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAttachmentVersionsAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentDto?> SignAttachmentAsync(
        Guid attachmentId,
        string signatureInfo,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.SignAttachmentAsync(attachmentId, signatureInfo, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SignAttachmentAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.GetAttachmentsByCategoryAsync(category, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAttachmentsByCategoryAsync for {Category}.", category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.DeleteAttachmentAsync(attachmentId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteAttachmentAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> AttachmentExistsAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await attachmentService.AttachmentExistsAsync(attachmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in AttachmentExistsAsync for {AttachmentId}.", attachmentId);
            throw;
        }
    }

    // Comment operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetCommentsAsync(
        Guid documentHeaderId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.GetDocumentHeaderCommentsAsync(documentHeaderId, includeReplies, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetCommentsAsync for {DocumentHeaderId}.", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto> CreateCommentAsync(
        CreateDocumentCommentDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.CreateCommentAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateCommentAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetDocumentRowCommentsAsync(
        Guid documentRowId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.GetDocumentRowCommentsAsync(documentRowId, includeReplies, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentRowCommentsAsync for {DocumentRowId}.", documentRowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> GetCommentByIdAsync(
        Guid commentId,
        bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.GetCommentByIdAsync(commentId, includeReplies, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetCommentByIdAsync for {CommentId}.", commentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> UpdateCommentAsync(
        Guid commentId,
        UpdateDocumentCommentDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.UpdateCommentAsync(commentId, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateCommentAsync for {CommentId}.", commentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ResolveCommentAsync(
        Guid commentId,
        ResolveCommentDto resolveDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.ResolveCommentAsync(commentId, resolveDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ResolveCommentAsync for {CommentId}.", commentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentDto?> ReopenCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.ReopenCommentAsync(commentId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ReopenCommentAsync for {CommentId}.", commentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentCommentStatsDto> GetDocumentCommentStatsAsync(
        Guid documentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.GetDocumentCommentStatsAsync(documentId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentCommentStatsAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentCommentDto>> GetAssignedCommentsAsync(
        string userId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.GetAssignedCommentsAsync(userId, status, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAssignedCommentsAsync for {UserId}.", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCommentAsync(
        Guid commentId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.DeleteCommentAsync(commentId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteCommentAsync for {CommentId}.", commentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CommentExistsAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await commentService.CommentExistsAsync(commentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CommentExistsAsync for {CommentId}.", commentId);
            throw;
        }
    }

    // Template operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.GetPublicTemplatesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetPublicTemplatesAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> GetTemplateByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.GetByIdAsync(templateId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetTemplateByIdAsync for {TemplateId}.", templateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAllTemplatesAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByDocumentTypeAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.GetByDocumentTypeAsync(documentTypeId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetTemplatesByDocumentTypeAsync for {DocumentTypeId}.", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetTemplatesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.GetByCategoryAsync(category, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetTemplatesByCategoryAsync for {Category}.", category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto> CreateTemplateAsync(
        CreateDocumentTemplateDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.CreateAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateTemplateAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> UpdateTemplateAsync(
        Guid templateId,
        UpdateDocumentTemplateDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.UpdateAsync(templateId, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateTemplateAsync for {TemplateId}.", templateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.DeleteAsync(templateId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteTemplateAsync for {TemplateId}.", templateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTemplateUsageAsync(
        Guid templateId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await templateService.UpdateUsageAsync(templateId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateTemplateUsageAsync for {TemplateId}.", templateId);
            throw;
        }
    }

    // Workflow operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetWorkflowsAsync(
        Guid? documentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (documentTypeId.HasValue)
            {
                return await workflowService.GetByDocumentTypeAsync(documentTypeId.Value, cancellationToken);
            }

            return await workflowService.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetWorkflowsAsync for {DocumentTypeId}.", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await workflowService.GetByIdAsync(workflowId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetWorkflowByIdAsync for {WorkflowId}.", workflowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto> CreateWorkflowAsync(
        CreateDocumentWorkflowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await workflowService.CreateAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateWorkflowAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> UpdateWorkflowAsync(
        Guid workflowId,
        UpdateDocumentWorkflowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await workflowService.UpdateAsync(workflowId, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateWorkflowAsync for {WorkflowId}.", workflowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteWorkflowAsync(
        Guid workflowId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await workflowService.DeleteAsync(workflowId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteWorkflowAsync for {WorkflowId}.", workflowId);
            throw;
        }
    }

    // Analytics operations
    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto?> GetAnalyticsAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await analyticsService.GetDocumentAnalyticsAsync(documentHeaderId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAnalyticsAsync for {DocumentHeaderId}.", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto> RefreshAnalyticsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await analyticsService.CreateOrUpdateAnalyticsAsync(documentHeaderId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RefreshAnalyticsAsync for {DocumentHeaderId}.", documentHeaderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await analyticsService.GetAnalyticsSummaryAsync(from, to, groupBy, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAnalyticsSummaryAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentKpiSummaryDto> CalculateKpiSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await analyticsService.CalculateKpiSummaryAsync(from, to, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CalculateKpiSummaryAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAnalyticsDto> HandleWorkflowEventAsync(
        Guid documentId,
        string eventType,
        object? eventData,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await analyticsService.HandleWorkflowEventAsync(documentId, eventType, eventData, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in HandleWorkflowEventAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    // Document Header operations
    /// <inheritdoc />
    public async Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetPagedDocumentHeadersAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentHeaderByIdAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentHeadersByBusinessPartyAsync for {BusinessPartyId}.", businessPartyId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateDocumentHeaderAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.UpdateDocumentHeaderAsync(id, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateDocumentHeaderAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.DeleteDocumentHeaderAsync(id, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteDocumentHeaderAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.CalculateDocumentTotalsAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CalculateDocumentTotalsAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.ApproveDocumentAsync(id, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ApproveDocumentAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CloseDocumentAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.DocumentHeaderExistsAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DocumentHeaderExistsAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.AddDocumentRowAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in AddDocumentRowAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.UpdateDocumentRowAsync(rowId, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateDocumentRowAsync for {RowId}.", rowId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentHeaderService.DeleteDocumentRowAsync(rowId, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteDocumentRowAsync for {RowId}.", rowId);
            throw;
        }
    }

    // Document Type operations
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentTypeService.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAllDocumentTypesAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> GetDocumentTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentTypeService.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentTypeByIdAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto> CreateDocumentTypeAsync(CreateDocumentTypeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentTypeService.CreateAsync(createDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateDocumentTypeAsync.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, UpdateDocumentTypeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentTypeService.UpdateAsync(id, updateDto, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateDocumentTypeAsync for {Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentTypeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentTypeService.DeleteAsync(id, currentUser, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteDocumentTypeAsync for {Id}.", id);
            throw;
        }
    }

    // Document Status operations
    /// <inheritdoc />
    public async Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentStatusService.ChangeStatusAsync(documentId, newStatus, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ChangeStatusAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentStatusService.GetStatusHistoryAsync(documentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetStatusHistoryAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await documentStatusService.GetAvailableTransitionsAsync(documentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAvailableTransitionsAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    #region Bulk Operations

    /// <inheritdoc />
    public async Task<EventForge.DTOs.Bulk.BulkApprovalResultDto> BulkApproveAsync(
        EventForge.DTOs.Bulk.BulkApprovalDto bulkApprovalDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<EventForge.DTOs.Bulk.BulkItemError>();
        var successCount = 0;

        // Validate batch size
        if (bulkApprovalDto.DocumentIds.Count > 500)
        {
            throw new ArgumentException("Maximum 500 documents can be approved at once.");
        }

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for bulk approval operations.");
            }

            var approvalDate = bulkApprovalDto.ApprovalDate ?? DateTime.UtcNow;

            // Fetch all documents in one query
            var documents = await context.DocumentHeaders
                .Where(d => bulkApprovalDto.DocumentIds.Contains(d.Id) && d.TenantId == currentTenantId.Value)
                .ToListAsync(cancellationToken);

            // Check for missing documents
            var foundIds = documents.Select(d => d.Id).ToHashSet();
            var missingIds = bulkApprovalDto.DocumentIds.Where(id => !foundIds.Contains(id)).ToList();
            foreach (var missingId in missingIds)
            {
                errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                {
                    ItemId = missingId,
                    ErrorMessage = "Document not found or does not belong to the current tenant."
                });
            }

            // Approve documents
            foreach (var document in documents)
            {
                try
                {
                    // Update document status to Closed (finalized/approved)
                    var oldStatus = document.Status;
                    document.Status = DocumentStatus.Closed;
                    document.ApprovedAt = approvalDate;
                    document.ApprovedBy = currentUser;
                    document.ModifiedAt = DateTime.UtcNow;
                    document.ModifiedBy = currentUser;

                    // Add status history entry
                    var statusHistory = new EventForge.Server.Data.Entities.Documents.DocumentStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId.Value,
                        DocumentHeaderId = document.Id,
                        FromStatus = oldStatus,
                        ToStatus = DocumentStatus.Closed,
                        Reason = bulkApprovalDto.ApprovalNotes,
                        ChangedBy = currentUser,
                        ChangedAt = approvalDate,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };
                    context.DocumentStatusHistories.Add(statusHistory);

                    successCount++;

                    logger.LogInformation(
                        "Bulk approval: Document {DocumentId} approved. Notes: {Notes}",
                        document.Id, bulkApprovalDto.ApprovalNotes ?? "N/A");
                }
                catch (Exception ex)
                {
                    errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = document.Id,
                        ItemName = document.Number,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Bulk approval completed: {SuccessCount} successful, {FailureCount} failed",
                successCount, errors.Count);

            return new EventForge.DTOs.Bulk.BulkApprovalResultDto
            {
                TotalCount = bulkApprovalDto.DocumentIds.Count,
                SuccessCount = successCount,
                FailedCount = errors.Count,
                Errors = errors,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = false
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Bulk approval failed and was rolled back");

            return new EventForge.DTOs.Bulk.BulkApprovalResultDto
            {
                TotalCount = bulkApprovalDto.DocumentIds.Count,
                SuccessCount = 0,
                FailedCount = bulkApprovalDto.DocumentIds.Count,
                Errors = new List<EventForge.DTOs.Bulk.BulkItemError>
                {
                    new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = Guid.Empty,
                        ErrorMessage = $"Transaction failed and was rolled back: {ex.Message}"
                    }
                },
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = true
            };
        }
    }

    /// <inheritdoc />
    public async Task<EventForge.DTOs.Bulk.BulkStatusChangeResultDto> BulkStatusChangeAsync(
        EventForge.DTOs.Bulk.BulkStatusChangeDto bulkStatusChangeDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<EventForge.DTOs.Bulk.BulkItemError>();
        var successCount = 0;

        // Validate batch size
        if (bulkStatusChangeDto.DocumentIds.Count > 500)
        {
            throw new ArgumentException("Maximum 500 documents can have their status changed at once.");
        }

        // Parse the new status
        if (!Enum.TryParse<DocumentStatus>(bulkStatusChangeDto.NewStatus, true, out var newStatus))
        {
            throw new ArgumentException($"Invalid status: {bulkStatusChangeDto.NewStatus}");
        }

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for bulk status change operations.");
            }

            var changeDate = bulkStatusChangeDto.ChangeDate ?? DateTime.UtcNow;

            // Fetch all documents in one query
            var documents = await context.DocumentHeaders
                .Where(d => bulkStatusChangeDto.DocumentIds.Contains(d.Id) && d.TenantId == currentTenantId.Value)
                .ToListAsync(cancellationToken);

            // Check for missing documents
            var foundIds = documents.Select(d => d.Id).ToHashSet();
            var missingIds = bulkStatusChangeDto.DocumentIds.Where(id => !foundIds.Contains(id)).ToList();
            foreach (var missingId in missingIds)
            {
                errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                {
                    ItemId = missingId,
                    ErrorMessage = "Document not found or does not belong to the current tenant."
                });
            }

            // Change document statuses
            foreach (var document in documents)
            {
                try
                {
                    // Check if status change is valid
                    if (document.Status == newStatus)
                    {
                        errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                        {
                            ItemId = document.Id,
                            ItemName = document.Number,
                            ErrorMessage = $"Document already has status {newStatus}."
                        });
                        continue;
                    }

                    var oldStatus = document.Status;
                    document.Status = newStatus;
                    document.ModifiedAt = DateTime.UtcNow;
                    document.ModifiedBy = currentUser;

                    // Add status history entry
                    var statusHistory = new EventForge.Server.Data.Entities.Documents.DocumentStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId.Value,
                        DocumentHeaderId = document.Id,
                        FromStatus = oldStatus,
                        ToStatus = newStatus,
                        Reason = bulkStatusChangeDto.Reason,
                        ChangedBy = currentUser,
                        ChangedAt = changeDate,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUser
                    };
                    context.DocumentStatusHistories.Add(statusHistory);

                    successCount++;

                    logger.LogInformation(
                        "Bulk status change: Document {DocumentId} status changed from {OldStatus} to {NewStatus}. Reason: {Reason}",
                        document.Id, oldStatus, newStatus, bulkStatusChangeDto.Reason ?? "N/A");
                }
                catch (Exception ex)
                {
                    errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = document.Id,
                        ItemName = document.Number,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Bulk status change completed: {SuccessCount} successful, {FailureCount} failed",
                successCount, errors.Count);

            return new EventForge.DTOs.Bulk.BulkStatusChangeResultDto
            {
                TotalCount = bulkStatusChangeDto.DocumentIds.Count,
                SuccessCount = successCount,
                FailedCount = errors.Count,
                Errors = errors,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = false
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Bulk status change failed and was rolled back");

            return new EventForge.DTOs.Bulk.BulkStatusChangeResultDto
            {
                TotalCount = bulkStatusChangeDto.DocumentIds.Count,
                SuccessCount = 0,
                FailedCount = bulkStatusChangeDto.DocumentIds.Count,
                Errors = new List<EventForge.DTOs.Bulk.BulkItemError>
                {
                    new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = Guid.Empty,
                        ErrorMessage = $"Transaction failed and was rolled back: {ex.Message}"
                    }
                },
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = true
            };
        }
    }

    #endregion

}
