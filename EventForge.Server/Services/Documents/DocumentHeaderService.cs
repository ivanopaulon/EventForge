using EventForge.Server.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentHeaderService> _logger;

    public DocumentHeaderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentHeaderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildDocumentHeaderQuery(queryParameters);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(dh => dh.Date)
                .Skip(queryParameters.Skip)
                .Take(queryParameters.PageSize)
                .Include(dh => dh.DocumentType)
                .Select(dh => dh.ToDto())
                .ToListAsync(cancellationToken);

            return new PagedResult<DocumentHeaderDto>
            {
                Items = items,
                Page = queryParameters.Page,
                PageSize = queryParameters.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated document headers.");
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentHeaders
                .Where(dh => dh.Id == id && !dh.IsDeleted);

            if (includeRows)
            {
                query = query.Include(dh => dh.Rows.Where(r => !r.IsDeleted));
            }

            var documentHeader = await query.FirstOrDefaultAsync(cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found.", id);
                return null;
            }

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document header {Id}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeaders = await _context.DocumentHeaders
                .Where(dh => dh.BusinessPartyId == businessPartyId && !dh.IsDeleted)
                .OrderByDescending(dh => dh.Date)
                .Include(dh => dh.DocumentType)
                .Select(dh => dh.ToDto())
                .ToListAsync(cancellationToken);

            return documentHeaders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document headers for business party {BusinessPartyId}.", businessPartyId);
            throw;
        }
    }

    public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = createDto.ToEntity();
            documentHeader.CreatedBy = currentUser;
            documentHeader.CreatedAt = DateTime.UtcNow;

            _context.DocumentHeaders.Add(documentHeader);

            if (createDto.Rows?.Any() == true)
            {
                foreach (var rowDto in createDto.Rows)
                {
                    var row = rowDto.ToEntity();
                    row.DocumentHeaderId = documentHeader.Id;
                    row.CreatedBy = currentUser;
                    row.CreatedAt = DateTime.UtcNow;
                    documentHeader.Rows.Add(row);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(documentHeader, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Document header {DocumentHeaderId} created by {User}.", documentHeader.Id, currentUser);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document header.");
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalHeader = await _context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for update.", id);
                return null;
            }

            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for update.", id);
                return null;
            }

            documentHeader.UpdateFromDto(updateDto);
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(documentHeader, "Update", currentUser, originalHeader, cancellationToken);

            _logger.LogInformation("Document header {DocumentHeaderId} updated by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document header {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalHeader = await _context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
                return false;
            }

            var documentHeader = await _context.DocumentHeaders
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
                return false;
            }

            documentHeader.IsDeleted = true;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            foreach (var row in documentHeader.Rows)
            {
                row.IsDeleted = true;
                row.ModifiedBy = currentUser;
                row.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(documentHeader, "Delete", currentUser, originalHeader, cancellationToken);

            _logger.LogInformation("Document header {DocumentHeaderId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document header {Id}.", id);
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = await _context.DocumentHeaders
                .Include(dh => dh.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for total calculation.", id);
                return null;
            }

            var netTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - r.LineDiscount / 100m));
            var vatTotal = documentHeader.Rows.Sum(r => (r.UnitPrice * r.Quantity * (1 - r.LineDiscount / 100m)) * (r.VatRate / 100m));

            if (documentHeader.TotalDiscount > 0)
                netTotal -= netTotal * (documentHeader.TotalDiscount / 100m);

            netTotal -= documentHeader.TotalDiscountAmount;

            documentHeader.TotalNetAmount = Math.Max(0, netTotal);
            documentHeader.VatAmount = vatTotal;
            documentHeader.TotalGrossAmount = documentHeader.TotalNetAmount + documentHeader.VatAmount;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Calculated totals for document header {DocumentHeaderId}.", id);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating document totals for {Id}.", id);
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalHeader = await _context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for approval.", id);
                return null;
            }

            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for approval.", id);
                return null;
            }

            documentHeader.ApprovalStatus = ApprovalStatus.Approved;
            documentHeader.ApprovedBy = currentUser;
            documentHeader.ApprovedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(documentHeader, "Approve", currentUser, originalHeader, cancellationToken);

            _logger.LogInformation("Document header {DocumentHeaderId} approved by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving document {Id}.", id);
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalHeader = await _context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for closing.", id);
                return null;
            }

            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for closing.", id);
                return null;
            }

            documentHeader.Status = DocumentStatus.Closed;
            documentHeader.ClosedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(documentHeader, "Close", currentUser, originalHeader, cancellationToken);

            _logger.LogInformation("Document header {DocumentHeaderId} closed by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing document {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DocumentHeaders
                .AnyAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document header {Id} exists.", id);
            throw;
        }
    }

    private IQueryable<DocumentHeader> BuildDocumentHeaderQuery(DocumentHeaderQueryParameters parameters)
    {
        var query = _context.DocumentHeaders.Where(dh => !dh.IsDeleted);

        if (parameters.DocumentTypeId.HasValue)
            query = query.Where(dh => dh.DocumentTypeId == parameters.DocumentTypeId.Value);

        if (!string.IsNullOrEmpty(parameters.Number))
            query = query.Where(dh => dh.Number.Contains(parameters.Number));

        if (!string.IsNullOrEmpty(parameters.Series))
            query = query.Where(dh => dh.Series == parameters.Series);

        if (parameters.FromDate.HasValue)
            query = query.Where(dh => dh.Date >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(dh => dh.Date <= parameters.ToDate.Value);

        if (parameters.BusinessPartyId.HasValue)
            query = query.Where(dh => dh.BusinessPartyId == parameters.BusinessPartyId.Value);

        if (!string.IsNullOrEmpty(parameters.CustomerName))
            query = query.Where(dh => dh.CustomerName != null && dh.CustomerName.Contains(parameters.CustomerName));

        if (parameters.Status.HasValue)
            query = query.Where(dh => dh.Status == parameters.Status.Value);

        if (parameters.PaymentStatus.HasValue)
            query = query.Where(dh => dh.PaymentStatus == parameters.PaymentStatus.Value);

        if (parameters.ApprovalStatus.HasValue)
            query = query.Where(dh => dh.ApprovalStatus == parameters.ApprovalStatus.Value);

        if (parameters.TeamId.HasValue)
            query = query.Where(dh => dh.TeamId == parameters.TeamId.Value);

        if (parameters.EventId.HasValue)
            query = query.Where(dh => dh.EventId == parameters.EventId.Value);

        if (parameters.SourceWarehouseId.HasValue)
            query = query.Where(dh => dh.SourceWarehouseId == parameters.SourceWarehouseId.Value);

        if (parameters.DestinationWarehouseId.HasValue)
            query = query.Where(dh => dh.DestinationWarehouseId == parameters.DestinationWarehouseId.Value);

        if (parameters.IsFiscal.HasValue)
            query = query.Where(dh => dh.IsFiscal == parameters.IsFiscal.Value);

        if (parameters.IsProforma.HasValue)
            query = query.Where(dh => dh.IsProforma == parameters.IsProforma.Value);

        return query;
    }
}