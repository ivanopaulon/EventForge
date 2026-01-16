using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

public class DocumentStatusService : IDocumentStatusService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DocumentStatusService> _logger;

    public DocumentStatusService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DocumentStatusService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _context.DocumentHeaders
                .Include(d => d.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                return null;
            }

            var documentDto = document.ToDto();
            var validationResult = DocumentStateMachine.ValidateTransition(documentDto, newStatus);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid status transition for document {DocumentId}: {ErrorMessage}",
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
                TenantId = _tenantContext.CurrentTenantId ?? Guid.Empty,
                CreatedBy = GetCurrentUser(),
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document {DocumentId} status changed from {OldStatus} to {NewStatus} by {User}",
                documentId, oldStatus, newStatus, GetCurrentUser());

            return document.ToDto();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _context.DocumentStatusHistories
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
            _logger.LogError(ex, "Error retrieving status history for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _context.DocumentHeaders
                .Where(d => d.Id == documentId && !d.IsDeleted)
                .Select(d => d.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == default)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                return new List<DocumentStatus>();
            }

            return DocumentStateMachine.GetAvailableTransitions(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available transitions for document {DocumentId}", documentId);
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
            var document = await _context.DocumentHeaders
                .Include(d => d.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (document == null)
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
            _logger.LogError(ex, "Error validating transition for document {DocumentId}", documentId);
            throw;
        }
    }

    #region Helper Methods

    private string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        return ipAddress;
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }

    #endregion
}
