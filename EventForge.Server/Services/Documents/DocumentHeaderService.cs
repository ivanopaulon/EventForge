using Prym.DTOs.Documents;
using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

public class DocumentHeaderService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    IDocumentCounterService documentCounterService,
    IStockMovementService stockMovementService,
    IUnitConversionService unitConversionService,
    ILogger<DocumentHeaderService> logger) : IDocumentHeaderService
{

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
                         .Include(dh => dh.DestinationWarehouse)
                         .Include(dh => dh.PriceList);

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
            var query = context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.PriceList)
                .Where(dh => dh.Id == id && !dh.IsDeleted);

            if (includeRows)
            {
                query = query.Include(dh => dh.Rows.Where(r => !r.IsDeleted));
            }

            var documentHeader = await query.FirstOrDefaultAsync(cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found.", id);
                return null;
            }

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeaders = await context.DocumentHeaders
                .AsNoTracking()
                .Where(dh => dh.BusinessPartyId == businessPartyId && !dh.IsDeleted)
                .OrderByDescending(dh => dh.Date)
                .Include(dh => dh.DocumentType)
                .Select(dh => dh.ToDto())
                .ToListAsync(cancellationToken);

            return documentHeaders;
        }
        catch (Exception ex)
        {
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
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot create document header without a tenant context.");
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
                documentHeader.Number = await documentCounterService.GenerateDocumentNumberAsync(
                    documentHeader.DocumentTypeId,
                    series,
                    currentUser,
                    cancellationToken);

                logger.LogInformation("Auto-generated document number '{Number}' for document type {DocumentTypeId}, series '{Series}'.",
                    documentHeader.Number, documentHeader.DocumentTypeId, series);
            }

            _ = context.DocumentHeaders.Add(documentHeader);

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

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Document header {DocumentHeaderId} created by {User}.", documentHeader.Id, currentUser);

            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
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
            var originalHeader = await context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for update.", id);
                return null;
            }

            var documentHeader = await context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for update.", id);
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

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating document header {DocumentHeaderId}.", id);
                throw new InvalidOperationException("Il documento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Update", currentUser, originalHeader, cancellationToken);

            // Sync stock movement dates if document date changed
            if (dateChanged)
            {
                await SyncStockMovementDatesForDocumentAsync(id, newDateUtc, currentUser, cancellationToken);
            }

            logger.LogInformation("Document header {DocumentHeaderId} updated by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
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
            var originalHeader = await context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
                return false;
            }

            var documentHeader = await context.DocumentHeaders
                .Include(dh => dh.Rows)
                .Include(dh => dh.DocumentType)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for deletion.", id);
                return false;
            }

            // If the document is approved and DocumentType is available, generate compensating movements BEFORE delete
            if (documentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved
                && documentHeader.DocumentType is not null)
            {
                var compensatingCount = 0;
                var documentDateUtc = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);

                foreach (var row in documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue))
                {
                    Guid? warehouseLocationId = documentHeader.DocumentType.IsStockIncrease
                        ? row.DestinationWarehouseId ?? documentHeader.DestinationWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId
                        : row.SourceWarehouseId ?? documentHeader.SourceWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId;

                    if (!warehouseLocationId.HasValue)
                    {
                        logger.LogWarning("No warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping compensating movement.", row.Id, id);
                        continue;
                    }

                    var storageLocation = await context.StorageLocations
                        .AsNoTracking()
                        .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (storageLocation is null)
                    {
                        logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping compensating movement.", warehouseLocationId, row.Id);
                        continue;
                    }

                    var quantity = row.BaseQuantity ?? row.Quantity;
                    var notes = $"Compensating movement: document {documentHeader.Id} deleted by {currentUser}";

                    if (documentHeader.DocumentType.IsStockIncrease)
                    {
                        // Original was Inbound → compensating is Outbound
                        await stockMovementService.ProcessOutboundMovementAsync(
                            productId: row.ProductId!.Value,
                            fromLocationId: storageLocation.Id,
                            quantity: quantity,
                            documentHeaderId: documentHeader.Id,
                            documentRowId: row.Id,
                            notes: notes,
                            currentUser: currentUser,
                            movementDate: documentDateUtc,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        // Original was Outbound → compensating is Inbound
                        await stockMovementService.ProcessInboundMovementAsync(
                            productId: row.ProductId!.Value,
                            toLocationId: storageLocation.Id,
                            quantity: quantity,
                            unitCost: row.UnitPrice,
                            documentHeaderId: documentHeader.Id,
                            documentRowId: row.Id,
                            notes: notes,
                            currentUser: currentUser,
                            movementDate: documentDateUtc,
                            cancellationToken: cancellationToken);
                    }

                    compensatingCount++;
                }

                logger.LogInformation("Created {Count} compensating stock movements before deleting document {DocumentHeaderId}.", compensatingCount, id);
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

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Delete", currentUser, originalHeader, cancellationToken);

            logger.LogInformation("Document header {DocumentHeaderId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = await context.DocumentHeaders
                .Include(dh => dh.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for total calculation.", id);
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

            _ = await context.SaveChangesAsync(cancellationToken);


            return documentHeader.ToDto();
        }
        catch (Exception ex)
        {
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
            var originalHeader = await context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for approval.", id);
                return null;
            }

            var documentHeader = await context.DocumentHeaders
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for approval.", id);
                return null;
            }

            documentHeader.ApprovalStatus = EventForge.Server.Data.Entities.Documents.ApprovalStatus.Approved;
            documentHeader.ApprovedBy = currentUser;
            documentHeader.ApprovedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict approving document {DocumentHeaderId}.", id);
                throw new InvalidOperationException("Il documento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Approve", currentUser, originalHeader, cancellationToken);

            // Reload document with dependencies for stock movement processing
            var documentForStockMovement = await context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentForStockMovement is not null)
            {
                // Process stock movements after approval
                await ProcessStockMovementsForDocumentAsync(documentForStockMovement, currentUser, cancellationToken);
            }

            logger.LogInformation("Document header {DocumentHeaderId} approved by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
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
            var originalHeader = await context.DocumentHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (originalHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for closing.", id);
                return null;
            }

            var documentHeader = await context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
            {
                logger.LogWarning("Document header with ID {Id} not found for closing.", id);
                return null;
            }

            documentHeader.Status = Prym.DTOs.Common.DocumentStatus.Closed;
            documentHeader.ClosedAt = DateTime.UtcNow;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict closing document {DocumentHeaderId}.", id);
                throw new InvalidOperationException("Il documento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(documentHeader, "Close", currentUser, originalHeader, cancellationToken);

            // Reload document with dependencies for stock movement processing
            var documentForStockMovement = await context.DocumentHeaders
                .AsNoTracking()
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);

            if (documentForStockMovement is not null)
            {
                // Process stock movements after closing
                await ProcessStockMovementsForDocumentAsync(documentForStockMovement, currentUser, cancellationToken);
            }

            logger.LogInformation("Document header {DocumentHeaderId} closed by {User}.", id, currentUser);

            return documentHeader.ToDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.DocumentHeaders
                .AsNoTracking()
                .AnyAsync(dh => dh.Id == id && !dh.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private IQueryable<DocumentHeader> BuildDocumentHeaderQuery(DocumentHeaderQueryParameters parameters)
    {
        var query = context.DocumentHeaders.AsNoTracking().Where(dh => !dh.IsDeleted);

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
            query = query.Where(dh => dh.Status == (Prym.DTOs.Common.DocumentStatus)parameters.Status.Value);

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
            var existingType = await context.DocumentTypes
                .AsNoTracking()
                .Where(dt => dt.TenantId == tenantId && dt.Code == "INVENTORY" && !dt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingType is not null)
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

            _ = context.DocumentTypes.Add(newType);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created inventory document type for tenant {TenantId}.", tenantId);

            return DocumentTypeMapper.ToDto(newType);
        }
        catch (Exception ex)
        {
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
            var existingType = await context.DocumentTypes
                .AsNoTracking()
                .Where(dt => dt.TenantId == tenantId && dt.Code == "RECEIPT" && !dt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingType is not null)
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

            _ = context.DocumentTypes.Add(newType);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created receipt document type for tenant {TenantId}.", tenantId);

            return DocumentTypeMapper.ToDto(newType);
        }
        catch (Exception ex)
        {
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
            var existingParty = await context.BusinessParties
                .Where(bp => bp.TenantId == tenantId && bp.Name == "System Internal" && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingParty is not null)
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

            _ = context.BusinessParties.Add(newParty);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created system business party for tenant {TenantId}.", tenantId);

            return newParty.Id;
        }
        catch (Exception ex)
        {
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
            var documentHeader = await context.DocumentHeaders
                .FirstOrDefaultAsync(dh => dh.Id == createDto.DocumentHeaderId && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
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
                var productUnit = await context.ProductUnits
                    .FirstOrDefaultAsync(pu =>
                        pu.ProductId == createDto.ProductId.Value &&
                        pu.UnitOfMeasureId == createDto.UnitOfMeasureId.Value &&
                        !pu.IsDeleted,
                        cancellationToken);

                if (productUnit is not null)
                {
                    // Find the base unit for this product (ConversionFactor = 1.0 and UnitType = "Base")
                    var baseUnit = await context.ProductUnits
                        .FirstOrDefaultAsync(pu =>
                            pu.ProductId == createDto.ProductId.Value &&
                            pu.ConversionFactor == 1m &&
                            pu.UnitType == "Base" &&
                            !pu.IsDeleted,
                            cancellationToken);

                    if (baseUnit is not null)
                    {
                        baseUnitOfMeasureId = baseUnit.UnitOfMeasureId;

                        // Compute base quantity using conversion factor
                        baseQuantity = unitConversionService.ConvertToBaseUnit(
                            createDto.Quantity,
                            productUnit.ConversionFactor,
                            decimalPlaces: 4);

                        // Compute base unit price (inverse conversion for price)
                        if (createDto.UnitPrice > 0)
                        {
                            baseUnitPrice = unitConversionService.ConvertPrice(
                                createDto.UnitPrice,
                                fromConversionFactor: productUnit.ConversionFactor,
                                toConversionFactor: 1m,
                                decimalPlaces: 4);
                        }
                    }
                }
            }

            // Check if we should merge with an existing IDENTICAL row
            if (createDto.MergeDuplicateProducts && createDto.ProductId.HasValue)
            {
                var existingRow = await context.DocumentRows
                    .FirstOrDefaultAsync(r =>
                        r.DocumentHeaderId == createDto.DocumentHeaderId &&
                        r.ProductId == createDto.ProductId &&
                        !r.IsDeleted,
                        cancellationToken);

                if (existingRow is not null)
                {

                    // Merge: sum base quantities and recalculate display quantity if units differ
                    if (baseQuantity.HasValue && existingRow.BaseQuantity.HasValue)
                    {
                        existingRow.BaseQuantity += baseQuantity.Value;

                        // Recalculate the display quantity if the existing row has a unit
                        if (existingRow.UnitOfMeasureId.HasValue && createDto.ProductId.HasValue)
                        {
                            var existingProductUnit = await context.ProductUnits
                                .FirstOrDefaultAsync(pu =>
                                    pu.ProductId == createDto.ProductId.Value &&
                                    pu.UnitOfMeasureId == existingRow.UnitOfMeasureId.Value &&
                                    !pu.IsDeleted,
                                    cancellationToken);

                            if (existingProductUnit is not null)
                            {
                                existingRow.Quantity = unitConversionService.ConvertFromBaseUnit(
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

                    _ = await context.SaveChangesAsync(cancellationToken);

                    _ = await auditLogService.TrackEntityChangesAsync(
                        existingRow,
                        "MergeUpdate",
                        currentUser,
                        null,
                        cancellationToken);

                    logger.LogInformation(
                        "Row merged successfully: RowId={RowId}, NewQty={NewQty}, NewBaseQty={NewBaseQty}",
                        existingRow.Id,
                        existingRow.Quantity,
                        existingRow.BaseQuantity);

                    return existingRow.ToDto();
                }
                else
                {
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

            _ = context.DocumentRows.Add(row);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(row, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Document row {RowId} added to document {DocumentHeaderId} by {User}.",
                row.Id, createDto.DocumentHeaderId, currentUser);

            // Auto-create or update ProductSupplier for purchase documents
            if (row.ProductId.HasValue && documentHeader.BusinessPartyId != Guid.Empty)
            {
                var docType = await context.DocumentTypes
                    .FirstOrDefaultAsync(dt => dt.Id == documentHeader.DocumentTypeId && !dt.IsDeleted, cancellationToken);

                if (docType?.IsStockIncrease == true)
                {
                    await EnsureProductSupplierAsync(
                        row.ProductId!.Value,
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
                if (documentHeader.DocumentType is null)
                {
                    documentHeader = await context.DocumentHeaders
                        .Include(dh => dh.DocumentType)
                        .FirstOrDefaultAsync(dh => dh.Id == documentHeader.Id && !dh.IsDeleted, cancellationToken) ?? documentHeader;
                }

                if (documentHeader.DocumentType is not null)
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
                        var storageLocation = await context.StorageLocations
                            .AsNoTracking()
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation is not null)
                        {
                            if (documentHeader.DocumentType.IsStockIncrease)
                            {
                                await stockMovementService.ProcessInboundMovementAsync(
                                    productId: row.ProductId!.Value,
                                    toLocationId: storageLocation.Id,
                                    quantity: row.Quantity,
                                    unitCost: row.UnitPrice,
                                    documentHeaderId: documentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Auto-generated from document {documentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);

                                logger.LogInformation("Created immediate inbound stock movement for approved document row {RowId}.", row.Id);
                            }
                            else
                            {
                                await stockMovementService.ProcessOutboundMovementAsync(
                                    productId: row.ProductId!.Value,
                                    fromLocationId: storageLocation.Id,
                                    quantity: row.Quantity,
                                    documentHeaderId: documentHeader.Id,
                                    documentRowId: row.Id,
                                    notes: $"Auto-generated from document {documentHeader.Number}",
                                    currentUser: currentUser,
                                    movementDate: documentDateUtc,
                                    cancellationToken: cancellationToken);

                                logger.LogInformation("Created immediate outbound stock movement for approved document row {RowId}.", row.Id);
                            }
                        }
                        else
                        {
                            logger.LogWarning("No storage location found in warehouse {WarehouseId} for approved document row {RowId}. Stock movement not created.",
                                warehouseLocationId, row.Id);
                        }
                    }
                    else
                    {
                        logger.LogWarning("No warehouse found for approved document row {RowId}. Stock movement not created.", row.Id);
                    }
                }
            }

            return row.ToDto();
        }
        catch (Exception ex)
        {
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
            var row = await context.DocumentRows
                .Include(r => r.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (row is null)
            {
                logger.LogWarning("Document row {RowId} not found for update.", rowId);
                return null;
            }

            // Store old base quantity for compensating movement calculation
            var oldBaseQuantity = row.BaseQuantity ?? row.Quantity;
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
            row.DiscountType = (Prym.DTOs.Common.DiscountType)updateDto.DiscountType;
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
            row.IsPriceManual = updateDto.IsPriceManual;
            row.AppliedPriceListId = updateDto.AppliedPriceListId;
            row.OriginalPriceFromPriceList = updateDto.OriginalPriceFromPriceList;
            row.PriceNotes = updateDto.PriceNotes;
            row.AppliedPromotionsJSON = updateDto.AppliedPromotionsJSON;
            row.ModifiedBy = currentUser;
            row.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(row, "Update", currentUser, null, cancellationToken);

            logger.LogInformation("Document row {RowId} updated by {User}.", rowId, currentUser);

            // If document is approved and quantity changed, create compensating movement
            if (row.DocumentHeader is not null &&
                row.DocumentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved &&
                row.ProductId.HasValue &&
                row.ProductId == oldProductId)
            {
                var newBaseQuantity = row.BaseQuantity ?? row.Quantity;
                var delta = newBaseQuantity - oldBaseQuantity;
                if (delta != 0)
                {
                    var documentDateUtc = DateTime.SpecifyKind(row.DocumentHeader.Date, DateTimeKind.Utc);

                    // Determine warehouse location
                    Guid? warehouseLocationId = null;
                    if (row.DocumentHeader.DocumentType is not null)
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
                            var storageLocation = await context.StorageLocations
                                .AsNoTracking()
                                .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (storageLocation is not null)
                            {
                                if (delta > 0)
                                {
                                    // Positive delta: add more stock
                                    if (row.DocumentHeader.DocumentType.IsStockIncrease)
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: delta,
                                            unitCost: row.UnitPrice,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: delta,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity increased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
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
                                        await stockMovementService.ProcessOutboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            fromLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await stockMovementService.ProcessInboundMovementAsync(
                                            productId: row.ProductId!.Value,
                                            toLocationId: storageLocation.Id,
                                            quantity: absDelta,
                                            unitCost: row.UnitPrice,
                                            documentHeaderId: row.DocumentHeader.Id,
                                            documentRowId: row.Id,
                                            notes: $"Compensating movement: quantity decreased from {oldBaseQuantity} to {newBaseQuantity} (base units)",
                                            currentUser: currentUser,
                                            movementDate: documentDateUtc,
                                            cancellationToken: cancellationToken);
                                    }
                                }

                                logger.LogInformation("Created compensating stock movement for updated row {RowId} in approved document. Delta: {Delta}",
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
            throw;
        }
    }

    /// <summary>
    /// Deletes a document row.
    /// </summary>
    public async Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await context.DocumentRows
                .Include(r => r.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (row is null)
            {
                logger.LogWarning("Document row {RowId} not found for deletion.", rowId);
                return false;
            }

            // If document is approved, create compensating movement before deleting row
            if (row.DocumentHeader is not null &&
                row.DocumentHeader.ApprovalStatus == Data.Entities.Documents.ApprovalStatus.Approved &&
                row.ProductId.HasValue)
            {
                // Find existing movement for this row
                var existingMovement = await context.StockMovements
                    .FirstOrDefaultAsync(sm => sm.DocumentRowId == rowId && !sm.IsDeleted, cancellationToken);

                if (existingMovement is not null && row.DocumentHeader.DocumentType is not null)
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
                        var storageLocation = await context.StorageLocations
                            .AsNoTracking()
                            .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (storageLocation is not null)
                        {
                            // Create reverse movement to compensate for the deletion
                            if (existingMovement.MovementType == StockMovementType.Inbound)
                            {
                                await stockMovementService.ProcessOutboundMovementAsync(
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
                                await stockMovementService.ProcessInboundMovementAsync(
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

                            logger.LogInformation("Created compensating stock movement for deleted row {RowId} in approved document.", rowId);
                        }
                    }
                }
            }

            // Soft delete
            row.IsDeleted = true;
            row.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(row, "Delete", currentUser, null, cancellationToken);

            logger.LogInformation("Document row {RowId} deleted.", rowId);

            return true;
        }
        catch (Exception ex)
        {
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
            if (documentHeader.DocumentType is null)
            {
                logger.LogWarning("Document type not loaded for document {DocumentHeaderId}. Cannot process stock movements.", documentHeader.Id);
                return;
            }

            if (documentHeader.Rows is null || !documentHeader.Rows.Any())
            {
                return;
            }

            // Ensure document date is in UTC for stock movements
            var documentDateUtc = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);


            foreach (var row in documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue))
            {
                // Per-row guard: skip only this row if its movement already exists
                var rowMovementExists = await context.StockMovements
                    .AnyAsync(sm => sm.DocumentRowId == row.Id && !sm.IsDeleted, cancellationToken);

                if (rowMovementExists)
                {
                    continue;
                }

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
                        logger.LogWarning("No destination warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                            row.Id, documentHeader.Id);
                        continue;
                    }

                    // Get the first storage location in the warehouse
                    var storageLocation = await context.StorageLocations
                        .AsNoTracking()
                        .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (storageLocation is null)
                    {
                        logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                            warehouseLocationId, row.Id);
                        continue;
                    }

                    // Create inbound movement
                    await stockMovementService.ProcessInboundMovementAsync(
                        productId: row.ProductId!.Value,
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
                        logger.LogWarning("No source warehouse found for row {RowId} in document {DocumentHeaderId}. Skipping stock movement.",
                            row.Id, documentHeader.Id);
                        continue;
                    }

                    // Get the storage location with available stock
                    var storageLocation = await context.StorageLocations
                        .AsNoTracking()
                        .Where(sl => sl.WarehouseId == warehouseLocationId.Value && !sl.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (storageLocation is null)
                    {
                        logger.LogWarning("No storage location found in warehouse {WarehouseId} for row {RowId}. Skipping stock movement.",
                            warehouseLocationId, row.Id);
                        continue;
                    }

                    // Check if sufficient stock is available
                    var availableStock = await context.Stocks
                        .Where(s => s.ProductId == row.ProductId!.Value
                                 && s.StorageLocationId == storageLocation.Id
                                 && !s.IsDeleted)
                        .SumAsync(s => s.Quantity - s.ReservedQuantity, cancellationToken);

                    if (availableStock < row.Quantity)
                    {
                        logger.LogWarning("Insufficient stock for product {ProductId} at location {LocationId}. Available: {Available}, Required: {Required}.",
                            row.ProductId!.Value, storageLocation.Id, availableStock, row.Quantity);
                        // Continue processing but log the warning
                    }

                    // Create outbound movement
                    await stockMovementService.ProcessOutboundMovementAsync(
                        productId: row.ProductId!.Value,
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

                }
            }

            logger.LogInformation("Completed processing stock movements for document {DocumentHeaderId}.", documentHeader.Id);
        }
        catch (Exception ex)
        {
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
                affected = await context.Database.ExecuteSqlInterpolatedAsync(
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
                var movements = await context.StockMovements
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
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            if (affected > 0)
            {
                // Log the sync operation
                await auditLogService.LogEntityChangeAsync(
                    "StockMovement",
                    documentHeaderId,
                    "MovementDate",
                    "BulkUpdate",
                    null,
                    $"Synchronized {affected} stock movement(s) to document date {newDateUtc:yyyy-MM-dd HH:mm:ss} UTC",
                    currentUser);

                logger.LogInformation("Synchronized {Count} stock movement dates for document {DocumentHeaderId} to {NewDate}.",
                    affected, documentHeaderId, newDateUtc);
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
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
            var existing = await context.Set<ProductSupplier>()
                .FirstOrDefaultAsync(ps => ps.ProductId == productId &&
                    ps.SupplierId == supplierId && !ps.IsDeleted, ct);

            if (existing is not null)
            {
                existing.LastPurchasePrice = unitPrice;
                existing.LastPurchaseDate = DateTime.UtcNow;
                existing.ModifiedBy = currentUser;
                existing.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                var tenantId = tenantContext.CurrentTenantId!.Value;
                context.Set<ProductSupplier>().Add(new ProductSupplier
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
            await context.SaveChangesAsync(ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring ProductSupplier for Product {ProductId} and Supplier {SupplierId}.",
                productId, supplierId);
            // Don't throw - this is a non-critical operation
        }
    }

    #region Lock Management

    /// <summary>
    /// Acquires an exclusive edit lock for a document.
    /// Lock expires after 30 minutes of inactivity.
    /// Uses optimistic concurrency control via RowVersion to prevent race conditions.
    /// </summary>
    public async Task<bool> AcquireLockAsync(Guid documentId, string userName, string connectionId)
    {
        logger.LogDebug(
            "AcquireLockAsync called: DocumentId={DocumentId}, UserName={UserName}, ConnectionId={ConnectionId}",
            documentId, userName, connectionId);

        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning(
                    "❌ Lock acquisition FAILED: TenantId is NULL. DocumentId={DocumentId}, UserName={UserName}",
                    documentId, userName);
                return false;
            }

            logger.LogDebug(
                "TenantId retrieved: {TenantId} for document {DocumentId}",
                tenantId.Value, documentId);

            // Use a retry pattern for optimistic concurrency
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    logger.LogDebug(
                        "Lock acquisition attempt {Attempt}/{MaxRetries} for document {DocumentId}",
                        attempt + 1, maxRetries, documentId);

                    var document = await context.DocumentHeaders
                        .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId.Value && !d.IsDeleted);

                    if (document is null)
                    {
                        logger.LogWarning(
                            "❌ Lock acquisition FAILED: Document NOT FOUND. DocumentId={DocumentId}, TenantId={TenantId}, UserName={UserName}",
                            documentId, tenantId.Value, userName);
                        return false;
                    }

                    logger.LogDebug(
                        "Document found: {DocumentId}, Current lock status: LockedBy={LockedBy}, LockedAt={LockedAt}, ConnectionId={ConnectionId}",
                        documentId, document.LockedBy ?? "(none)", document.LockedAt, document.LockConnectionId ?? "(none)");

                    // Check existing lock
                    if (!string.IsNullOrEmpty(document.LockedBy) && document.LockedBy != userName)
                    {
                        logger.LogDebug(
                            "Document {DocumentId} has existing lock by different user. Current: {CurrentUser}, Requested: {RequestedUser}",
                            documentId, document.LockedBy, userName);

                        // Check if lock is still valid (less than 30 minutes old)
                        if (document.LockedAt.HasValue)
                        {
                            var lockAge = DateTime.UtcNow - document.LockedAt.Value;

                            logger.LogDebug(
                                "Lock age check: {LockAge} (threshold: 1 hour) for document {DocumentId}",
                                lockAge, documentId);

                            if (lockAge < TimeSpan.FromHours(1))
                            {
                                logger.LogWarning(
                                    "❌ Lock acquisition FAILED: Document {DocumentId} is locked by {LockedBy} (lock age: {LockAge}, still valid)",
                                    documentId, document.LockedBy, lockAge);
                                return false; // Lock is still valid
                            }

                            // Lock expired - can be acquired
                            logger.LogInformation(
                                "Lock on document {DocumentId} EXPIRED (lock age: {LockAge}). Acquiring for {UserName}.",
                                documentId, lockAge, userName);
                        }
                    }

                    // Acquire or refresh lock
                    logger.LogDebug(
                        "Attempting to set lock: DocumentId={DocumentId}, UserName={UserName}, ConnectionId={ConnectionId}",
                        documentId, userName, connectionId);

                    document.LockedBy = userName;
                    document.LockedAt = DateTime.UtcNow;
                    document.LockConnectionId = connectionId;

                    logger.LogDebug(
                        "Lock properties set, calling SaveChangesAsync for document {DocumentId}",
                        documentId);

                    var changeCount = await context.SaveChangesAsync();

                    logger.LogInformation(
                        "✅ Lock ACQUIRED successfully on document {DocumentId} by {UserName} (connection: {ConnectionId}). Changes saved: {ChangeCount}",
                        documentId, userName, connectionId, changeCount);

                    return true;
                }
                catch (DbUpdateConcurrencyException concurrencyEx) when (attempt < maxRetries - 1)
                {
                    logger.LogWarning(
                        concurrencyEx,
                        "⚠️ Concurrency conflict acquiring lock for document {DocumentId}, attempt {Attempt}/{MaxRetries}. Retrying...",
                        documentId, attempt + 1, maxRetries);

                    // Detach the entity to allow retry
                    var entries = context.ChangeTracker.Entries()
                        .Where(e => e.Entity is DocumentHeader && ((DocumentHeader)e.Entity).Id == documentId);
                    foreach (var entry in entries)
                    {
                        logger.LogDebug(
                            "Detaching entity {EntityType} with state {State}",
                            entry.Entity.GetType().Name, entry.State);
                        entry.State = EntityState.Detached;
                    }

                    // Small delay before retry
                    await Task.Delay(50 * (attempt + 1));
                }
                catch (DbUpdateException dbEx)
                {
                    // Non-concurrency database errors (e.g., constraint violations, FK errors)
                    // are not transient and should not be retried - propagate to outer catch
                    logger.LogError(
                        dbEx,
                        "❌ DATABASE UPDATE ERROR during lock acquisition for document {DocumentId}, attempt {Attempt}. Inner exception: {InnerException}",
                        documentId, attempt + 1, dbEx.InnerException?.Message ?? "(none)");
                    throw; // Re-throw to be caught by outer catch
                }
            }

            // All retries failed
            logger.LogWarning(
                "❌ Lock acquisition FAILED: Max retries ({MaxRetries}) exceeded for document {DocumentId}",
                documentId, maxRetries);
            return false;
        }
        catch (DbUpdateConcurrencyException finalConcurrencyEx)
        {
            logger.LogError(
                finalConcurrencyEx,
                "❌ CONCURRENCY EXCEPTION (final) acquiring lock for document {DocumentId}",
                documentId);
            return false;
        }
        catch (DbUpdateException finalDbEx)
        {
            logger.LogError(
                finalDbEx,
                "❌ DATABASE EXCEPTION acquiring lock for document {DocumentId}. Inner: {InnerException}",
                documentId, finalDbEx.InnerException?.Message ?? "(none)");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "❌ UNEXPECTED EXCEPTION acquiring lock for document {DocumentId}. Exception type: {ExceptionType}, Message: {Message}",
                documentId, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Releases an edit lock for a document.
    /// Only the user who holds the lock can release it.
    /// </summary>
    public async Task<bool> ReleaseLockAsync(Guid documentId, string userName)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot release lock without a tenant context.");
                return false;
            }

            var document = await context.DocumentHeaders
                .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId.Value && !d.IsDeleted);

            if (document is null)
            {
                logger.LogWarning("Document {DocumentId} not found for lock release.", documentId);
                return false;
            }

            // Only the user who holds the lock can release it
            if (document.LockedBy == userName)
            {
                document.LockedBy = null;
                document.LockedAt = null;
                document.LockConnectionId = null;

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Lock released on document {DocumentId} by {UserName}",
                    documentId, userName);

                return true;
            }

            logger.LogWarning(
                "User {UserName} attempted to release lock on document {DocumentId} but doesn't hold it (locked by: {LockedBy})",
                userName, documentId, document.LockedBy);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error releasing lock for document {DocumentId}", documentId);
            return false;
        }
    }

    /// <summary>
    /// Releases all locks held by a specific SignalR connection.
    /// Called when a user disconnects.
    /// </summary>
    public async Task ReleaseAllLocksForConnectionAsync(string connectionId, Guid? tenantId = null)
    {
        try
        {
            var effectiveTenantId = tenantId ?? tenantContext.CurrentTenantId;

            var query = context.DocumentHeaders
                .Where(d => d.LockConnectionId == connectionId && !d.IsDeleted);

            if (effectiveTenantId.HasValue)
                query = query.Where(d => d.TenantId == effectiveTenantId.Value);

            var documents = await query.ToListAsync();

            if (documents.Any())
            {
                foreach (var doc in documents)
                {
                    logger.LogInformation(
                        "Releasing lock on document {DocumentId} (locked by {LockedBy}) due to connection disconnect",
                        doc.Id, doc.LockedBy);

                    doc.LockedBy = null;
                    doc.LockedAt = null;
                    doc.LockConnectionId = null;
                }

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Released {Count} locks for disconnected connection {ConnectionId}",
                    documents.Count, connectionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error releasing locks for connection {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Gets lock information for a document.
    /// </summary>
    public async Task<DocumentLockInfo?> GetLockInfoAsync(Guid documentId)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot get lock info without a tenant context.");
                return null;
            }

            var lockInfo = await context.DocumentHeaders
                .AsNoTracking()
                .Where(d => d.Id == documentId && d.TenantId == tenantId.Value && !d.IsDeleted)
                .Select(d => new DocumentLockInfo
                {
                    DocumentId = d.Id,
                    IsLocked = !string.IsNullOrEmpty(d.LockedBy),
                    LockedBy = d.LockedBy,
                    LockedAt = d.LockedAt,
                    ConnectionId = d.LockConnectionId
                })
                .FirstOrDefaultAsync();

            return lockInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lock info for document {DocumentId}", documentId);
            return null;
        }
    }

    #endregion

    #region Export Operations

    public async Task<IEnumerable<Prym.DTOs.Export.DocumentExportDto>> GetDocumentsForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for document operations.");
        }

        var query = context.DocumentHeaders
            .AsNoTracking()
            .Include(d => d.DocumentType)
            .Include(d => d.BusinessParty)
            .Where(d => !d.IsDeleted && d.TenantId == currentTenantId.Value)
            .OrderBy(d => d.Date);

        var totalCount = await query.CountAsync(ct);


        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetDocumentsInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(d => new Prym.DTOs.Export.DocumentExportDto
        {
            Id = d.Id,
            DocumentNumber = d.Number,
            DocumentType = d.DocumentType?.Name ?? string.Empty,
            DocumentDate = d.Date,
            BusinessParty = d.BusinessParty?.Name ?? string.Empty,
            TotalAmount = d.TotalGrossAmount,
            TotalVat = d.VatAmount,
            NetAmount = d.TotalNetAmount,
            Status = d.Status.ToString(),
            Notes = d.Notes,
            CreatedAt = d.CreatedAt
        });
    }

    private async Task<IEnumerable<Prym.DTOs.Export.DocumentExportDto>> GetDocumentsInBatchesAsync(
        IQueryable<DocumentHeader> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.DocumentExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(d => new Prym.DTOs.Export.DocumentExportDto
            {
                Id = d.Id,
                DocumentNumber = d.Number,
                DocumentType = d.DocumentType?.Name ?? string.Empty,
                DocumentDate = d.Date,
                BusinessParty = d.BusinessParty?.Name ?? string.Empty,
                TotalAmount = d.TotalGrossAmount,
                TotalVat = d.VatAmount,
                NetAmount = d.TotalNetAmount,
                Status = d.Status.ToString(),
                Notes = d.Notes,
                CreatedAt = d.CreatedAt
            }));

            skip += batchSize;

        }

        return results;
    }

    #endregion

}
