using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventForge.Data.Entities.Documents;
using EventForge.DTOs.Audit;
using EventForge.DTOs.Documents;
using EventForge.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Documents;

/// <summary>
/// Service implementation for managing document headers.
/// </summary>
public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly EventForgeDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentHeaderService> _logger;

    public DocumentHeaderService(
        EventForgeDbContext context,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<DocumentHeaderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
                .OrderByDescending(dh => dh.Date)  // Default order
                .Skip(queryParameters.Skip)
                .Take(queryParameters.PageSize)
                .ProjectTo<DocumentHeaderDto>(_mapper.ConfigurationProvider)
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

            return documentHeader == null ? null : _mapper.Map<DocumentHeaderDto>(documentHeader);
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
                .ProjectTo<DocumentHeaderDto>(_mapper.ConfigurationProvider)
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
            var documentHeader = _mapper.Map<DocumentHeader>(createDto);
            documentHeader.CreatedBy = currentUser;
            documentHeader.CreatedAt = DateTime.UtcNow;

            _context.DocumentHeaders.Add(documentHeader);

            // Create rows if provided
            if (createDto.Rows?.Any() == true)
            {
                foreach (var rowDto in createDto.Rows)
                {
                    var row = _mapper.Map<DocumentRow>(rowDto);
                    row.DocumentHeaderId = documentHeader.Id;
                    row.CreatedBy = currentUser;
                    row.CreatedAt = DateTime.UtcNow;
                    documentHeader.Rows.Add(row);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync("DocumentHeader", documentHeader.Id, "Created", "Create", 
                null, $"Document header {documentHeader.Number} created", currentUser, cancellationToken: cancellationToken);

            return _mapper.Map<DocumentHeaderDto>(documentHeader);
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
            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
                return null;

            _mapper.Map(updateDto, documentHeader);
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync("DocumentHeader", id, "Updated", "Update",
                null, $"Document header {documentHeader.Number} updated", currentUser, cancellationToken: cancellationToken);

            return _mapper.Map<DocumentHeaderDto>(documentHeader);
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
            var documentHeader = await _context.DocumentHeaders
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
                return false;

            // Soft delete the header and all its rows
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

            await _auditLogService.LogEntityChangeAsync("DocumentHeader", id, "Deleted", "Delete",
                null, $"Document header {documentHeader.Number} deleted", currentUser, cancellationToken: cancellationToken);

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
                return null;

            // Calculate totals based on document rows
            var netTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - r.LineDiscount / 100m));
            var vatTotal = documentHeader.Rows.Sum(r => (r.UnitPrice * r.Quantity * (1 - r.LineDiscount / 100m)) * (r.VatRate / 100m));

            // Apply document-level discounts
            if (documentHeader.TotalDiscount > 0)
                netTotal -= netTotal * (documentHeader.TotalDiscount / 100m);
            
            netTotal -= documentHeader.TotalDiscountAmount;

            documentHeader.TotalNetAmount = Math.Max(0, netTotal);
            documentHeader.VatAmount = vatTotal;
            documentHeader.TotalGrossAmount = documentHeader.TotalNetAmount + documentHeader.VatAmount;

            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<DocumentHeaderDto>(documentHeader);
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
            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
                return null;

            documentHeader.ApprovalStatus = ApprovalStatus.Approved;
            documentHeader.ApprovedBy = currentUser;
            documentHeader.ApprovedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync("DocumentHeader", id, "Approved", "Update",
                null, $"Document header {documentHeader.Number} approved", currentUser, cancellationToken: cancellationToken);

            return _mapper.Map<DocumentHeaderDto>(documentHeader);
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
            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
                return null;

            documentHeader.Status = DocumentStatus.Closed;
            documentHeader.ClosedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync("DocumentHeader", id, "Closed", "Update",
                null, $"Document header {documentHeader.Number} closed", currentUser, cancellationToken: cancellationToken);

            return _mapper.Map<DocumentHeaderDto>(documentHeader);
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