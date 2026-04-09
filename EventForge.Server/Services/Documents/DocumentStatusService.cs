using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

public class DocumentStatusService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DocumentStatusService> logger) : IDocumentStatusService
{

    public async Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await context.DocumentHeaders
                .Include(d => d.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (document is null)
            {
                logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                return null;
            }

            var documentDto = document.ToDto();
            var validationResult = DocumentStateMachine.ValidateTransition(documentDto, newStatus);

            if (!validationResult.IsValid)
            {
                logger.LogWarning("Invalid status transition for document {DocumentId}: {ErrorMessage}",
                    documentId, validationResult.ErrorMessage);
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            var oldStatus = document.Status;
            document.Status = newStatus;
            document.ModifiedAt = DateTime.UtcNow;
            document.ModifiedBy = GetCurrentUser();

            if (newStatus == DocumentStatus.Closed)
            {
                document.ClosedAt = DateTime.UtcNow;
            }

            // Create status history record
            var statusHistory = new DocumentStatusHistory
            {
                DocumentHeaderId = documentId,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                Reason = reason,
                ChangedBy = GetCurrentUser(),
                ChangedAt = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                TenantId = tenantContext.CurrentTenantId ?? Guid.Empty,
                CreatedBy = GetCurrentUser(),
                CreatedAt = DateTime.UtcNow
            };

            context.DocumentStatusHistories.Add(statusHistory);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Document {DocumentId} status changed from {OldStatus} to {NewStatus} by {User}",
                documentId, oldStatus, newStatus, GetCurrentUser());

            return document.ToDto();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing status for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await context.DocumentStatusHistories
                .AsNoTracking()
                .Where(h => h.DocumentHeaderId == documentId && !h.IsDeleted)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync(cancellationToken);

            return history.Select(h => new DocumentStatusHistoryDto
            {
                Id = h.Id,
                DocumentHeaderId = h.DocumentHeaderId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Reason = h.Reason,
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving status history for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await context.DocumentHeaders
                .Where(d => d.Id == documentId && !d.IsDeleted)
                .Select(d => d.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == default)
            {
                logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                return [];
            }

            return DocumentStateMachine.GetAvailableTransitions(document);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available transitions for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<StateTransitionValidationResult> ValidateTransitionAsync(
        Guid documentId,
        DocumentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await context.DocumentHeaders
                .AsNoTracking()
                .Include(d => d.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (document is null)
            {
                return StateTransitionValidationResult.Fail(
                    "Document not found",
                    StateTransitionErrorCode.InvalidTransition);
            }

            var documentDto = document.ToDto();
            return DocumentStateMachine.ValidateTransition(documentDto, newStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating transition for document {DocumentId}", documentId);
            throw;
        }
    }

    #region Helper Methods

    private string GetCurrentUser()
    {
        return httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }

    private string? GetClientIpAddress()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        return ipAddress;
    }

    private string? GetUserAgent()
    {
        return httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }

    #endregion

}
