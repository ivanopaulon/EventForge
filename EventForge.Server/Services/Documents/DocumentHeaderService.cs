using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly IDocumentCounterService _documentCounterService;
    private readonly IStockMovementService _stockMovementService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<DocumentHeaderService> _logger;

    public DocumentHeaderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        IDocumentCounterService documentCounterService,
        IStockMovementService stockMovementService,
        IUnitConversionService unitConversionService,
        ILogger<DocumentHeaderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _documentCounterService = documentCounterService ?? throw new ArgumentNullException(nameof(documentCounterService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _unitConversionService = unitConversionService ?? throw new ArgumentNullException(nameof(unitConversionService));
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

            // Include related entities
            query = query.Include(dh => dh.DocumentType)
                         .Include(dh => dh.BusinessParty)
                         .Include(dh => dh.SourceWarehouse)
                         .Include(dh => dh.DestinationWarehouse);

            // Include Rows if requested
            if (queryParameters.IncludeRows)
            {
                query = query.Include(dh => dh.Rows.Where(r => !r.IsDeleted));
            }

            var items = await query
                .OrderByDescending(dh => dh.Date)
                .Skip(queryParameters.Skip)
                .Take(queryParameters.PageSize)
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
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot create document header without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var documentHeader = createDto.ToEntity();
            documentHeader.TenantId = tenantId.Value;
            documentHeader.CreatedBy = currentUser;
            documentHeader.CreatedAt = DateTime.UtcNow;

            // Auto-generate document number if not provided
            if (string.IsNullOrWhiteSpace(documentHeader.Number))
            {
                var series = documentHeader.Series ?? string.Empty;
                documentHeader.Number = await _documentCounterService.GenerateDocumentNumberAsync(
                    documentHeader.DocumentTypeId,
                    series,
                    currentUser,
                    cancellationToken);

                _logger.LogInformation("Auto-generated document number '{Number}' for document type {DocumentTypeId}, series '{Series}'.",
                    documentHeader.Number, documentHeader.DocumentTypeId, series);
            }

            _ = _context.DocumentHeaders.Add(documentHeader);

            if (createDto.Rows?.Any() == true)
            {
                foreach (var rowDto in createDto.Rows)
                {
                    var row = rowDto.ToEntity();
                    row.DocumentHeaderId = documentHeader.Id;
                    row.TenantId = tenantId.Value;
                    row.CreatedBy = currentUser;
                    row.CreatedAt = DateTime.UtcNow;
                    documentHeader.Rows.Add(row);
                }
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(documentHeader, "Insert", currentUser, null, cancellationToken);

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

            // Detect if Date changed to sync stock movements
            // Normalize both dates to UTC for proper comparison
            var originalDateUtc = DateTime.SpecifyKind(originalHeader.Date, DateTimeKind.Utc);
            var newDateUtc = DateTime.SpecifyKind(updateDto.Date, DateTimeKind.Utc);
            var dateChanged = originalDateUtc != newDateUtc;

            documentHeader.UpdateFromDto(updateDto);
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(documentHeader, "Update", currentUser, originalHeader, cancellationToken);

            // Sync stock movement dates if document date changed
            if (dateChanged)
            {
                await SyncStockMovementDatesForDocumentAsync(id, newDateUtc, currentUser, cancellationToken);
            }

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

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(documentHeader, "Delete", currentUser, originalHeader, cancellationToken);

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

            var netTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)));
            var vatTotal = documentHeader.Rows.Sum(r => r.UnitPrice * r.Quantity * (1 - (r.LineDiscount / 100m)) * (r.VatRate / 100m));

            if (documentHeader.TotalDiscount > 0)
                netTotal -= netTotal * (documentHeader.TotalDiscount / 100m);

            netTotal -= documentHeader.TotalDiscountAmount;

            documentHeader.TotalNetAmount = Math.Max(0, netTotal);
            documentHeader.VatAmount = vatTotal;
            documentHeader.TotalGrossAmount = documentHeader.TotalNetAmount + documentHeader.VatAmount;

            _ = await _context.SaveChangesAsync(cancellationToken);

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
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                _logger.LogWarning("Document header with ID {Id} not found for approval.", id);
                return null;
            }

            documentHeader.ApprovalStatus = EventForge.Server.Data.Entities.Documents.ApprovalStatus.Approved;
            documentHeader.ApprovedBy = currentUser;
            documentHeader.ApprovedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(documentHeader, "Approve", currentUser, originalHeader, cancellationToken);

            // Reload document with dependencies for stock movement processing
            var documentForStockMovement = await _context.DocumentHeaders
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentForStockMovement != null)
            {
                // Process stock movements after approval
                await ProcessStockMovementsForDocumentAsync(documentForStockMovement, currentUser, cancellationToken);
            }

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

            documentHeader.Status = EventForge.Server.Data.Entities.Documents.DocumentStatus.Closed;
            documentHeader.ClosedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(documentHeader, "Close", currentUser, originalHeader, cancellationToken);

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
            query = query.Where(dh => dh.Status == (EventForge.Server.Data.Entities.Documents.DocumentStatus)parameters.Status.Value);

        if (parameters.PaymentStatus.HasValue)
            query = query.Where(dh => dh.PaymentStatus == (EventForge.Server.Data.Entities.Documents.PaymentStatus)parameters.PaymentStatus.Value);

        if (parameters.ApprovalStatus.HasValue)
            query = query.Where(dh => dh.ApprovalStatus == (EventForge.Server.Data.Entities.Documents.ApprovalStatus)parameters.ApprovalStatus.Value);

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

        if (parameters.ProductId.HasValue)
            query = query.Where(dh => dh.Rows.Any(r => !r.IsDeleted && r.ProductId == parameters.ProductId.Value));

        return query;
    }

    public async Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find existing inventory document type
            var existingType = await _context.DocumentTypes
                .Where(dt => dt.TenantId == tenantId && dt.Code == "INVENTORY" && !dt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingType != null)
            {
                return DocumentTypeMapper.ToDto(existingType);
            }

            // Create new inventory document type
            var newType = new DocumentType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "INVENTORY",
                Name = "Inventory Document",
                Notes = "Physical inventory count document",
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.DocumentTypes.Add(newType);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created inventory document type for tenant {TenantId}.", tenantId);

            return DocumentTypeMapper.ToDto(newType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating inventory document type for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates a receipt document type for sales.
    /// </summary>
    public async Task<DocumentTypeDto> GetOrCreateReceiptDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find existing receipt document type
            var existingType = await _context.DocumentTypes
                .Where(dt => dt.TenantId == tenantId && dt.Code == "RECEIPT" && !dt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingType != null)
            {
                return DocumentTypeMapper.ToDto(existingType);
            }

            // Create new receipt document type
            var newType = new DocumentType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "RECEIPT",
                Name = "Receipt Document",
                Notes = "Sales receipt document",
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.DocumentTypes.Add(newType);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created receipt document type for tenant {TenantId}.", tenantId);

            return DocumentTypeMapper.ToDto(newType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating receipt document type for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates a system business party for internal operations.
    /// </summary>
    public async Task<Guid> GetOrCreateSystemBusinessPartyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find existing system business party
            var existingParty = await _context.BusinessParties
                .Where(bp => bp.TenantId == tenantId && bp.Name == "System Internal" && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingParty != null)
            {
                return existingParty.Id;
            }

            // Create new system business party
            var newParty = new BusinessParty
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "System Internal",
                PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.BusinessParties.Add(newParty);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created system business party for tenant {TenantId}.", tenantId);

            return newParty.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating system business party for tenant {TenantId}.", tenantId);
            throw;
        }
    }

    public async Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify document header exists
            var documentHeader = await _context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == createDto.DocumentHeaderId && !dh.IsDeleted, cancellationToken);

            if (documentHeader == null)
            {
                throw new InvalidOperationException($"Document header with ID {createDto.DocumentHeaderId} not found.");
            }

            // Compute base quantity and base unit price if UnitOfMeasureId is provided
            decimal? baseQuantity = createDto.BaseQuantity;
            decimal? baseUnitPrice = createDto.BaseUnitPrice;
            Guid? baseUnitOfMeasureId = createDto.BaseUnitOfMeasureId;

            if (createDto.UnitOfMeasureId.HasValue && createDto.ProductId.HasValue)
            {
                // Load the ProductUnit to get the conversion factor and base unit
                var productUnit = await _context.ProductUnits
                    .FirstOrDefaultAsync(pu =>
                        pu.ProductId == createDto.ProductId.Value &&
                        pu.UnitOfMeasureId == createDto.UnitOfMeasureId.Value &&
                        !pu.IsDeleted,
                        cancellationToken);

                if (productUnit != null)
                {
                    // Find the base unit for this product (ConversionFactor = 1.0 and UnitType = "Base")
                    var baseUnit = await _context.ProductUnits
                        .FirstOrDefaultAsync(pu =>
                            pu.ProductId == createDto.ProductId.Value &&
                            pu.ConversionFactor == 1m &&
                            pu.UnitType == "Base" &&
                            !pu.IsDeleted,
                            cancellationToken);

                    if (baseUnit != null)
                    {
                        baseUnitOfMeasureId = baseUnit.UnitOfMeasureId;

                        // Compute base quantity using conversion factor
                        baseQuantity = _unitConversionService.ConvertToBaseUnit(
                            createDto.Quantity,
                            productUnit.ConversionFactor,
                            decimalPlaces: 4);

                        // Compute base unit price (inverse conversion for price)
                        if (createDto.UnitPrice > 0)
                        {
                            baseUnitPrice = _unitConversionService.ConvertPrice(
                                createDto.UnitPrice,
                                fromConversionFactor: productUnit.ConversionFactor,
                                toConversionFactor: 1m,
                                decimalPlaces: 4);
                        }
                    }
                }
            }

            // Check if we should merge with an existing row
            if (createDto.MergeDuplicateProducts && createDto.ProductId.HasValue)
            {
                var existingRow = await _context.DocumentRows
                    .FirstOrDefaultAsync(r =>
                        r.DocumentHeaderId == createDto.DocumentHeaderId &&
                        r.ProductId == createDto.ProductId &&
                        !r.IsDeleted,
                        cancellationToken);

                if (existingRow != null)
                {
                    // Merge: sum base quantities and recalculate display quantity if units differ
                    if (baseQuantity.HasValue && existingRow.BaseQuantity.HasValue)
                    {
                        existingRow.BaseQuantity += baseQuantity.Value;

                        // Recalculate the display quantity if the existing row has a unit
                        if (existingRow.UnitOfMeasureId.HasValue && createDto.ProductId.HasValue)
                        {
                            var existingProductUnit = await _context.ProductUnits
                                .FirstOrDefaultAsync(pu =>
                                    pu.ProductId == createDto.ProductId.Value &&
                                    pu.UnitOfMeasureId == existingRow.UnitOfMeasureId.Value &&
                                    !pu.IsDeleted,
                                    cancellationToken);

                            if (existingProductUnit != null)
                            {
                                existingRow.Quantity = _unitConversionService.ConvertFromBaseUnit(
                                    existingRow.BaseQuantity.Value,
                                    existingProductUnit.ConversionFactor,
                                    decimalPlaces: 4);
                            }
                            else
                            {
                                existingRow.Quantity += createDto.Quantity;
                            }
                        }
                        else
                        {
                            existingRow.Quantity += createDto.Quantity;
                        }
                    }
                    else
                    {
                        // Fallback: just add quantities if base quantities not available
                        existingRow.Quantity += createDto.Quantity;
                    }

                    existingRow.ModifiedBy = currentUser;
                    existingRow.ModifiedAt = DateTime.UtcNow;

                    _ = await _context.SaveChangesAsync(cancellationToken);

                    _ = await _auditLogService.TrackEntityChangesAsync(existingRow, "Update", currentUser, null, cancellationToken);

                    _logger.LogInformation("Document row {RowId} quantity updated (merged) in document {DocumentHeaderId} by {User}.",
                        existingRow.Id, createDto.DocumentHeaderId, currentUser);

                    return existingRow.ToDto();
                }
            }

            // Create new row (default behavior)
            var row = createDto.ToEntity();
            row.TenantId = documentHeader.TenantId; // Set TenantId from document header
            row.BaseQuantity = baseQuantity;
            row.BaseUnitPrice = baseUnitPrice;
            row.BaseUnitOfMeasureId = baseUnitOfMeasureId;
            row.CreatedBy = currentUser;
            row.CreatedAt = DateTime.UtcNow;

            _ = _context.DocumentRows.Add(row);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(row, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Document row {RowId} added to document {DocumentHeaderId} by {User}.",
                row.Id, createDto.DocumentHeaderId, currentUser);

            // Auto-create or update ProductSupplier for purchase documents
            if (row.ProductId.HasValue && documentHeader.BusinessPartyId != Guid.Empty)
            {
                var docType = await _context.DocumentTypes
                    .FirstOrDefaultAsync(dt => dt.Id == documentHeader.DocumentTypeId && !dt.IsDeleted, cancellationToken);

                if (docType?.IsStockIncrease == true)
                {
                    await EnsureProductSupplierAsync(
                        row.ProductId.Value,
                        documentHeader.BusinessPartyId,
                        row.UnitPrice,
                        currentUser,
                        cancellationToken);
                }
            }

            // If document is already approved, create stock movement immediately
            if (documentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved && row.ProductId.HasValue)
            {
                // Load document type to determine stock increase/decrease
                if (documentHeader.DocumentType == null)
                {
                    documentHeader = await _context.DocumentHeaders
                        .Include(dh => dh.DocumentType)
                        .FirstOrDefaultAsync(dh => dh.Id == documentHeader.Id && !dh.IsDeleted, cancellationToken) ?? documentHeader;
                }

                if (documentHeader.DocumentType != null)
                {
                    var documentDateUtc = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);

                    // Determine the warehouse location to use (same logic as ProcessStockMovementsForDocumentAsync)
                    Guid? warehouseLocationId = null;
                    if (documentHeader.DocumentType.IsStockIncrease)
                    {
                        warehouseLocationId = row.DestinationWarehouseId
                                           ?? documentHeader.DestinationWarehouseId
                                           ?? documentHeader.DocumentType.DefaultWarehouseId;
                    }
                    else
                    {
                        warehouseLocationId = row.SourceWarehouseId
                                           ?? documentHeader.SourceWarehouseId
                                           ?? documentHeader.DocumentType.DefaultWarehouseId;
                    }

                    if (warehouseLocationId.HasValue)
                    {
                        var storageLocation = await _context.StorageLocations
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation != null)
                        {
                            if (documentHeader.DocumentType.IsStockIncrease)
                            {
                                await _stockMovementService.ProcessInboundMovementAsync(
                                    productId: row.ProductId.Value,
                                    toLocationId: storageLocation.Id,
                                    quantity: row.Quantity,
                                    unitCost: row.UnitPrice,
                                    documentHeaderId: documentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Auto-generated from document {documentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);

                                _logger.LogInformation("Created immediate inbound stock movement for approved document row {RowId}.", row.Id);
                            }
                            else
                            {
                                await _stockMovementService.ProcessOutboundMovementAsync(
                                    productId: row.ProductId.Value,
                                    fromLocationId: storageLocation.Id,
                                    quantity: row.Quantity,
                                    documentHeaderId: documentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Auto-generated from document {documentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);

                                _logger.LogInformation("Created immediate outbound stock movement for approved document row {RowId}.", row.Id);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No storage location found in warehouse {WarehouseId} for approved document row {RowId}. Stock movement not created.",
                                warehouseLocationId, row.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No warehouse found for approved document row {RowId}. Stock movement not created.", row.Id);
                    }
                }
            }

            return row.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document row to document {DocumentHeaderId}.", createDto.DocumentHeaderId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await _context.DocumentRows
                .Include(r => r.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (row == null)
            {
                _logger.LogWarning("Document row {RowId} not found for update.", rowId);
                return null;
            }

            // Store old quantity for compensating movement calculation
            var oldQuantity = row.Quantity;
            var oldProductId = row.ProductId;

            // Update row properties
            row.RowType = (Data.Entities.Documents.DocumentRowType)updateDto.RowType;
            row.ParentRowId = updateDto.ParentRowId;
            row.ProductCode = updateDto.ProductCode;
            row.Description = updateDto.Description;
            row.UnitOfMeasure = updateDto.UnitOfMeasure;
            row.UnitOfMeasureId = updateDto.UnitOfMeasureId;
            row.UnitPrice = updateDto.UnitPrice;
            row.Quantity = updateDto.Quantity;
            row.LineDiscount = updateDto.LineDiscount;
            row.LineDiscountValue = updateDto.LineDiscountValue;
            row.DiscountType = (EventForge.DTOs.Common.DiscountType)updateDto.DiscountType;
            row.VatRate = updateDto.VatRate;
            row.VatDescription = updateDto.VatDescription;
            row.IsGift = updateDto.IsGift;
            row.IsManual = updateDto.IsManual;
            row.SourceWarehouseId = updateDto.SourceWarehouseId;
            row.DestinationWarehouseId = updateDto.DestinationWarehouseId;
            row.Notes = updateDto.Notes;
            row.SortOrder = updateDto.SortOrder;
            row.StationId = updateDto.StationId;
            row.BaseQuantity = updateDto.BaseQuantity;
            row.BaseUnitPrice = updateDto.BaseUnitPrice;
            row.BaseUnitOfMeasureId = updateDto.BaseUnitOfMeasureId;
            row.ModifiedBy = currentUser;
            row.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(row, "Update", currentUser, null, cancellationToken);

            _logger.LogInformation("Document row {RowId} updated by {User}.", rowId, currentUser);

            // If document is approved and quantity changed, create compensating movement
            if (row.DocumentHeader != null &&
                row.DocumentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved &&
                row.ProductId.HasValue &&
                row.ProductId == oldProductId)
            {
                var delta = row.Quantity - oldQuantity;
                if (delta != 0)
                {
                    var documentDateUtc = DateTime.SpecifyKind(row.DocumentHeader.Date, DateTimeKind.Utc);

                    // Determine warehouse location
                    Guid? warehouseLocationId = null;
                    if (row.DocumentHeader.DocumentType != null)
                    {
                        if (row.DocumentHeader.DocumentType.IsStockIncrease)
                        {
                            warehouseLocationId = row.DestinationWarehouseId
                                               ?? row.DocumentHeader.DestinationWarehouseId
                                               ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                        }
                        else
                        {
                            warehouseLocationId = row.SourceWarehouseId
                                               ?? row.DocumentHeader.SourceWarehouseId
                                               ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                        }

                        if (warehouseLocationId.HasValue)
                        {
                            var storageLocation = await _context.StorageLocations
                                .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (storageLocation != null)
                            {
                                if (delta > 0)
                                {
                                    // Positive delta: add more stock
                                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await _stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: delta,
                                            unitCost: row.UnitPrice,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldQuantity} to {row.Quantity}",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await _stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: delta,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldQuantity} to {row.Quantity}",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                }
                                else
                                {
                                    // Negative delta: remove stock
                                    var absDelta = Math.Abs(delta);
                                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await _stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldQuantity} to {row.Quantity}",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await _stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            unitCost: row.UnitPrice,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldQuantity} to {row.Quantity}",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                }

                                _logger.LogInformation("Created compensating stock movement for updated row {RowId} in approved document. Delta: {Delta}",
                                    rowId, delta);
                            }
                        }
                    }
                }
            }

            return row.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document row {RowId}.", rowId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a document row.
    /// </summary>
    public async Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await _context.DocumentRows
                .Include(r => r.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (row == null)
            {
                _logger.LogWarning("Document row {RowId} not found for deletion.", rowId);
                return false;
            }

            var currentUser = "System"; // Default, should be passed in ideally

            // If document is approved, create compensating movement before deleting row
            if (row.DocumentHeader != null &&
                row.DocumentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved &&
                row.ProductId.HasValue)
            {
                // Find existing movement for this row
                var existingMovement = await _context.StockMovements
                    .FirstOrDefaultAsync(sm => sm.DocumentRowId == rowId && !sm.IsDeleted, cancellationToken);

                if (existingMovement != null && row.DocumentHeader.DocumentType != null)
                {
                    var documentDateUtc = DateTime.SpecifyKind(row.DocumentHeader.Date, DateTimeKind.Utc);

                    // Determine warehouse location
                    Guid? warehouseLocationId = null;
                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                    {
                        warehouseLocationId = row.DestinationWarehouseId
                                           ?? row.DocumentHeader.DestinationWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }
                    else
                    {
                        warehouseLocationId = row.SourceWarehouseId
                                           ?? row.DocumentHeader.SourceWarehouseId
                                           ?? row.DocumentHeader.DocumentType.DefaultWarehouseId;
                    }

                    if (warehouseLocationId.HasValue)
                    {
                        var storageLocation = await _context.StorageLocations
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation != null)
                        {
                            // Create reverse movement to compensate for the deletion
                            if (existingMovement.MovementType == StockMovementType.Inbound)
                            {
                                await _stockMovementService.ProcessOutboundMovementAsync(
                                    productId: existingMovement.ProductId,
                                    fromLocationId: existingMovement.ToLocationId ?? storageLocation.Id,
                                    quantity: existingMovement.Quantity,
                                    documentHeaderId: row.DocumentHeader.Id,
                                    documentRowId: rowId,
                                    notes: $"Compensating movement: document row deleted",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await _stockMovementService.ProcessInboundMovementAsync(
                                    productId: existingMovement.ProductId,
                                    toLocationId: existingMovement.FromLocationId ?? storageLocation.Id,
                                    quantity: existingMovement.Quantity,
                                    documentHeaderId: row.DocumentHeader.Id,
                                    documentRowId: rowId,
                                    notes: $"Compensating movement: document row deleted",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);
                            }

                            _logger.LogInformation("Created compensating stock movement for deleted row {RowId} in approved document.", rowId);
                        }
                    }
                }
            }

            // Soft delete
            row.IsDeleted = true;
            row.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(row, "Delete", currentUser, null, cancellationToken);

            _logger.LogInformation("Document row {RowId} deleted.", rowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document row {RowId}.", rowId);
            throw;
        }
    }

    /// <summary>
    /// Processes stock movements for a document based on its type and rows.
    /// </summary>
    private async Task ProcessStockMovementsForDocumentAsync(
        Data.Entities.Documents.DocumentHeader documentHeader,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (documentHeader.DocumentType == null)
            {
                _logger.LogWarning("Document type not loaded for document {DocumentHeaderId}. Cannot process stock movements.", documentHeader.Id);
                return;
            }

            if (documentHeader.Rows == null || !documentHeader.Rows.Any())
            {
                _logger.LogInformation("Document {DocumentHeaderId} has no rows. No stock movements to process.", documentHeader.Id);
                return;
            }

            // Check if movements already exist for this document
            var existingMovements = await _context.StockMovements
                .Where(sm => sm.DocumentHeaderId == documentHeader.Id && !sm.IsDeleted)
                .AnyAsync(cancellationToken);

            if (existingMovements)
            {
                _logger.LogInformation("Stock movements already exist for document {DocumentHeaderId}. Skipping.", documentHeader.Id);
                return;
            }

            // Ensure document date is in UTC for stock movements
            var documentDateUtc = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);

            _logger.LogInformation("Processing stock movements for document {DocumentHeaderId} with type {DocumentTypeName}.",
                documentHeader.Id, documentHeader.DocumentType.Name);

            foreach (var row in documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue))
            {
                // Determine the warehouse location to use
                Guid? warehouseLocationId = null;

                // For stock increase documents (purchases, returns)
                if (documentHeader.DocumentType.IsStockIncrease)
                {
                    // Use destination warehouse from row, or document, or document type default
                    warehouseLocationId = row.DestinationWarehouseId
                                       ?? documentHeader.DestinationWarehouseId
                                       ?? documentHeader.DocumentType.DefaultWarehouseId;

                    if (!warehouseLocationId.HasValue)
                    {
                        _logger.LogWarning("No destination warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                            row.Id, documentHeader.Id);
                        continue;
                    }

                    // Get the first storage location in the warehouse
                    var storageLocation = await _context.StorageLocations
                        .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (storageLocation == null)
                    {
                        _logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                            warehouseLocationId, row.Id);
                        continue;
                    }

                    // Create inbound movement
                    await _stockMovementService.ProcessInboundMovementAsync(
                        productId: row.ProductId.Value,
                        toLocationId: storageLocation.Id,
                        quantity: row.Quantity,
                        unitCost: row.UnitPrice,
                        lotId: null,
                        serialId: null,
                        documentHeaderId: documentHeader.Id,
                        documentRowId: row.Id,
                        notes: $"Auto-generated from document {documentHeader.Number}",
                        currentUser: currentUser,
                        movementDate: documentDateUtc,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Created inbound stock movement for product {ProductId}, quantity {Quantity} in document {DocumentHeaderId}.",
                        row.ProductId.Value, row.Quantity, documentHeader.Id);
                }
                // For stock decrease documents (sales, deliveries)
                else
                {
                    // Use source warehouse from row, or document, or document type default
                    warehouseLocationId = row.SourceWarehouseId
                                       ?? documentHeader.SourceWarehouseId
                                       ?? documentHeader.DocumentType.DefaultWarehouseId;

                    if (!warehouseLocationId.HasValue)
                    {
                        _logger.LogWarning("No source warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                            row.Id, documentHeader.Id);
                        continue;
                    }

                    // Get the storage location with available stock
                    var storageLocation = await _context.StorageLocations
                        .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (storageLocation == null)
                    {
                        _logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                            warehouseLocationId, row.Id);
                        continue;
                    }

                    // Check if sufficient stock is available
                    var availableStock = await _context.Stocks
                        .Where(s => s.ProductId == row.ProductId.Value
                                 && s.StorageLocationId == storageLocation.Id
                                 && !s.IsDeleted)
                        .SumAsync(s => s.Quantity - s.ReservedQuantity, cancellationToken);

                    if (availableStock < row.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId} at location {LocationId}. Available: {Available}, Required: {Required}.",
                            row.ProductId.Value, storageLocation.Id, availableStock, row.Quantity);
                        // Continue processing but log the warning
                    }

                    // Create outbound movement
                    await _stockMovementService.ProcessOutboundMovementAsync(
                        productId: row.ProductId.Value,
                        fromLocationId: storageLocation.Id,
                        quantity: row.Quantity,
                        lotId: null,
                        serialId: null,
                        documentHeaderId: documentHeader.Id,
                        documentRowId: row.Id,
                        notes: $"Auto-generated from document {documentHeader.Number}",
                        currentUser: currentUser,
                        movementDate: documentDateUtc,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Created outbound stock movement for product {ProductId}, quantity {Quantity} in document {DocumentHeaderId}.",
                        row.ProductId.Value, row.Quantity, documentHeader.Id);
                }
            }

            _logger.LogInformation("Completed processing stock movements for document {DocumentHeaderId}.", documentHeader.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock movements for document {DocumentHeaderId}.", documentHeader.Id);
            throw;
        }
    }

    /// <summary>
    /// Synchronizes stock movement dates for a document when the document date changes.
    /// </summary>
    private async Task SyncStockMovementDatesForDocumentAsync(
        Guid documentHeaderId,
        DateTime newDate,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the date is in UTC
            var newDateUtc = DateTime.SpecifyKind(newDate, DateTimeKind.Utc);
            var modifiedAt = DateTime.UtcNow;

            // Try batch SQL update for efficiency (works with SQL Server)
            // Fall back to in-memory update for test databases
            int affected;
            try
            {
                affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE StockMovements
                       SET MovementDate = {newDateUtc}, 
                           ModifiedAt = {modifiedAt}, 
                           ModifiedBy = {currentUser}
                       WHERE DocumentHeaderId = {documentHeaderId} 
                         AND IsDeleted = 0",
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Fallback for in-memory databases used in tests
                var movements = await _context.StockMovements
                    .Where(sm => sm.DocumentHeaderId == documentHeaderId && !sm.IsDeleted)
                    .ToListAsync(cancellationToken);

                affected = movements.Count;
                foreach (var movement in movements)
                {
                    movement.MovementDate = newDateUtc;
                    movement.ModifiedAt = modifiedAt;
                    movement.ModifiedBy = currentUser;
                }

                if (affected > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            if (affected > 0)
            {
                // Log the sync operation
                await _auditLogService.LogEntityChangeAsync(
                    "StockMovement",
                    documentHeaderId,
                    "MovementDate",
                    "BulkUpdate",
                    null,
                    $"Synchronized {affected} stock movement(s) to document date {newDateUtc:yyyy-MM-dd HH:mm:ss} UTC",
                    currentUser);

                _logger.LogInformation("Synchronized {Count} stock movement dates for document {DocumentHeaderId} to {NewDate}.",
                    affected, documentHeaderId, newDateUtc);
            }
            else
            {
                _logger.LogInformation("No stock movements found for document {DocumentHeaderId} to sync dates.", documentHeaderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing stock movement dates for document {DocumentHeaderId}.", documentHeaderId);
            throw;
        }
    }

    /// <summary>
    /// Ensures a ProductSupplier relationship exists for the given product and supplier.
    /// If it exists, updates the last purchase price and date. If not, creates a new one.
    /// </summary>
    private async Task EnsureProductSupplierAsync(Guid productId, Guid supplierId,
        decimal unitPrice, string currentUser, CancellationToken ct)
    {
        try
        {
            var existing = await _context.Set<ProductSupplier>()
                .FirstOrDefaultAsync(ps => ps.ProductId == productId &&
                    ps.SupplierId == supplierId && !ps.IsDeleted, ct);

            if (existing != null)
            {
                existing.LastPurchasePrice = unitPrice;
                existing.LastPurchaseDate = DateTime.UtcNow;
                existing.ModifiedBy = currentUser;
                existing.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                var tenantId = _tenantContext.CurrentTenantId.Value;
                _context.Set<ProductSupplier>().Add(new ProductSupplier
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    SupplierId = supplierId,
                    TenantId = tenantId,
                    UnitCost = unitPrice,
                    LastPurchasePrice = unitPrice,
                    LastPurchaseDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("ProductSupplier ensured for Product {ProductId} and Supplier {SupplierId}.",
                productId, supplierId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring ProductSupplier for Product {ProductId} and Supplier {SupplierId}.",
                productId, supplierId);
            // Don't throw - this is a non-critical operation
        }
    }
}