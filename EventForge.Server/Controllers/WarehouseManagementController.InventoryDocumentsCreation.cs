using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class WarehouseManagementController
{
    /// <summary>
    /// Gets all inventory documents with pagination and filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="status">Filter by document status (Draft, Closed, etc.)</param>
    /// <param name="fromDate">Filter documents from this date</param>
    /// <param name="toDate">Filter documents to this date</param>
    /// <param name="includeRows">Whether to include document rows (default: false for performance)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of inventory documents</returns>
    /// <response code="200">Returns the paginated list of inventory documents</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents")]
    [ProducesResponseType(typeof(PagedResult<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<InventoryDocumentDto>>> GetInventoryDocuments(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            // Build query parameters to filter inventory documents
            var queryParams = new DocumentHeaderQueryParameters
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                DocumentTypeId = inventoryDocType.Id,
                IncludeRows = includeRows, // Controlled by parameter - default false for performance
                SortBy = "Date",
                SortDirection = "desc"
            };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var parsedStatus))
            {
                queryParams.Status = (Prym.DTOs.Common.DocumentStatus)(int)parsedStatus;
            }

            if (fromDate.HasValue)
            {
                queryParams.FromDate = fromDate.Value;
            }

            if (toDate.HasValue)
            {
                queryParams.ToDate = toDate.Value;
            }

            // Get documents
            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            // Convert to InventoryDocumentDto with enriched rows (only if requested)
            var inventoryDocuments = new List<InventoryDocumentDto>();
            foreach (var doc in documentsResult.Items)
            {
                // Enrich rows with complete product and location data using optimized batch method
                // Only enrich if rows were requested and are present
                var enrichedRows = includeRows && doc.Rows is not null && doc.Rows.Any()
                    ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(doc.Rows, cancellationToken)
                    : new List<InventoryDocumentRowDto>();

                inventoryDocuments.Add(new InventoryDocumentDto
                {
                    Id = doc.Id,
                    Number = doc.Number,
                    Series = doc.Series,
                    InventoryDate = doc.Date,
                    WarehouseId = doc.SourceWarehouseId,
                    WarehouseName = doc.SourceWarehouseName,
                    Status = doc.Status.ToString(),
                    Notes = doc.Notes,
                    CreatedAt = doc.CreatedAt,
                    CreatedBy = doc.CreatedBy,
                    FinalizedAt = doc.ArchivedAt,
                    FinalizedBy = null,
                    Rows = enrichedRows
                });
            }

            var result = new PagedResult<InventoryDocumentDto>
            {
                Items = inventoryDocuments,
                TotalCount = documentsResult.TotalCount,
                Page = documentsResult.Page,
                PageSize = documentsResult.PageSize
            };

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory documents.", ex);
        }
    }

    /// <summary>
    /// Gets an inventory document by ID.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory document</returns>
    /// <response code="200">Returns the inventory document</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/document/{documentId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Enrich rows with complete product and location data
            var enrichedRows = documentHeader.Rows is not null && documentHeader.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(documentHeader.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = documentHeader.Id,
                Number = documentHeader.Number,
                Series = documentHeader.Series,
                InventoryDate = documentHeader.Date,
                WarehouseId = documentHeader.SourceWarehouseId,
                WarehouseName = documentHeader.SourceWarehouseName,
                Status = documentHeader.Status.ToString(),
                Notes = documentHeader.Notes,
                CreatedAt = documentHeader.CreatedAt,
                CreatedBy = documentHeader.CreatedBy,
                FinalizedAt = documentHeader.ArchivedAt,
                FinalizedBy = null,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving inventory document.", ex);
        }
    }

    /// <summary>
    /// Starts a new inventory document.
    /// </summary>
    /// <param name="createDto">Inventory document creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created inventory document</returns>
    /// <response code="200">Returns the created inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/start")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartInventoryDocument([FromBody] CreateInventoryDocumentDto createDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return Problem("Tenant not found or invalid.", statusCode: StatusCodes.Status403Forbidden);
            }

            // Get or create an "Inventory" document type
            var inventoryDocumentType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(currentTenantId.Value, cancellationToken);

            // Get or create system business party for internal operations
            var systemBusinessPartyId = await warehouseFacade.GetOrCreateSystemBusinessPartyAsync(currentTenantId.Value, cancellationToken);

            // Generate document number if not provided
            var documentNumber = createDto.Number ?? $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // Create a simplified document header for inventory
            var createHeaderDto = new CreateDocumentHeaderDto
            {
                DocumentTypeId = inventoryDocumentType.Id,
                Series = createDto.Series,
                Number = documentNumber,
                Date = createDto.InventoryDate,
                BusinessPartyId = systemBusinessPartyId,
                SourceWarehouseId = createDto.WarehouseId,
                Notes = createDto.Notes,
                IsFiscal = false,
                IsProforma = true
            };

            var documentHeader = await warehouseFacade.CreateDocumentHeaderAsync(createHeaderDto, GetCurrentUser(), cancellationToken);

            // Map to inventory document DTO
            var result = new InventoryDocumentDto
            {
                Id = documentHeader.Id,
                Number = documentHeader.Number,
                Series = documentHeader.Series,
                InventoryDate = documentHeader.Date,
                WarehouseId = documentHeader.SourceWarehouseId,
                WarehouseName = documentHeader.SourceWarehouseName,
                Status = documentHeader.Status.ToString(),
                Notes = documentHeader.Notes,
                CreatedAt = documentHeader.CreatedAt,
                CreatedBy = documentHeader.CreatedBy,
                Rows = new List<InventoryDocumentRowDto>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while starting the inventory document.", ex);
        }
    }

    /// <summary>
    /// Adds a row to an existing inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowDto">Row data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/{documentId:guid}/row")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddInventoryDocumentRow(Guid documentId, [FromBody] AddInventoryDocumentRowDto rowDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Get current stock level to calculate adjustment
            var existingStocks = await warehouseFacade.GetStockAsync(
                page: 1,
                pageSize: 1,
                productId: rowDto.ProductId,
                locationId: rowDto.LocationId,
                lotId: rowDto.LotId,
                cancellationToken: cancellationToken);

            var existingStock = existingStocks.Items.FirstOrDefault();
            var currentQuantity = existingStock?.Quantity ?? 0;
            var adjustmentQuantity = rowDto.Quantity - currentQuantity;

            // Get product and location info for the row - fetch from ProductService to ensure complete data
            var product = await warehouseFacade.GetProductByIdAsync(rowDto.ProductId, cancellationToken);
            var location = await warehouseFacade.GetStorageLocationByIdAsync(rowDto.LocationId, cancellationToken);

            if (product is null)
            {
                return CreateNotFoundProblem($"Product with ID {rowDto.ProductId} was not found.");
            }

            if (location is null)
            {
                return CreateNotFoundProblem($"Location with ID {rowDto.LocationId} was not found.");
            }

            // Get unit of measure symbol if available
            string? unitOfMeasure = null;
            if (product.UnitOfMeasureId.HasValue)
            {
                try
                {
                    unitOfMeasure = await warehouseFacade.GetUnitOfMeasureSymbolAsync(product.UnitOfMeasureId.Value, cancellationToken);
                }
                catch
                {
                    // Continue without unit of measure if fetch fails
                }
            }

            // Get VAT rate if available
            decimal vatRate = 0m;
            string? vatDescription = null;
            if (product.VatRateId.HasValue)
            {
                try
                {
                    var vatDetails = await warehouseFacade.GetVatRateDetailsAsync(product.VatRateId.Value, cancellationToken);
                    if (vatDetails.HasValue)
                    {
                        vatRate = vatDetails.Value.Percentage;
                        vatDescription = vatDetails.Value.Description;
                    }
                }
                catch
                {
                    // Continue without VAT rate if fetch fails
                }
            }

            // Check if a row with the same ProductId + LocationId (+ LotId if present) already exists
            // This implements the row merging feature (accorpamento delle righe per articolo/ubicazione)
            var existingRow = documentHeader.Rows?
                .FirstOrDefault(r =>
                    r.ProductId == rowDto.ProductId &&
                    r.LocationId == rowDto.LocationId);

            DocumentRowDto documentRow;

            if (existingRow is not null)
            {
                // Row exists - merge by adding quantities together
                var newQuantity = existingRow.Quantity + rowDto.Quantity;


                // Update the existing row via facade
                documentRow = await warehouseFacade.UpdateOrMergeInventoryRowAsync(
                    documentId,
                    existingRow.Id,
                    newQuantity,
                    rowDto.Notes,
                    GetCurrentUser(),
                    cancellationToken);
            }
            else
            {
                // No existing row - create a new one
                var createRowDto = new CreateDocumentRowDto
                {
                    DocumentHeaderId = documentId,
                    ProductCode = product.Code,
                    ProductId = rowDto.ProductId,
                    LocationId = rowDto.LocationId,
                    Description = product.Name, // Clean product name only
                    UnitOfMeasure = unitOfMeasure,
                    UnitOfMeasureId = rowDto.UnitOfMeasureId, // Pass UnitOfMeasureId to enable conversion
                    Quantity = rowDto.Quantity,
                    UnitPrice = 0, // Purchase price - skipped for now per requirements
                    VatRate = vatRate,
                    VatDescription = vatDescription,
                    SourceWarehouseId = location.WarehouseId, // Track the warehouse/location
                    Notes = rowDto.Notes
                };

                documentRow = await warehouseFacade.AddDocumentRowAsync(createRowDto, GetCurrentUser(), cancellationToken);
            }

            // Build response with the new row
            var newRow = new InventoryDocumentRowDto
            {
                Id = documentRow.Id,
                ProductId = rowDto.ProductId,
                ProductName = product?.Name ?? string.Empty,
                ProductCode = product?.Code ?? string.Empty,
                LocationId = rowDto.LocationId,
                LocationName = location?.Code ?? string.Empty,
                Quantity = rowDto.Quantity,
                PreviousQuantity = currentQuantity,
                AdjustmentQuantity = adjustmentQuantity,
                LotId = rowDto.LotId,
                LotCode = existingStock?.LotCode,
                Notes = rowDto.Notes,
                CreatedAt = documentRow.CreatedAt,
                CreatedBy = documentRow.CreatedBy
            };

            // Get updated document
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data using the helper method
            var enrichedRows = updatedDocument?.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument!.Id,
                Number = updatedDocument.Number,
                Series = updatedDocument.Series,
                InventoryDate = updatedDocument.Date,
                WarehouseId = updatedDocument.SourceWarehouseId,
                WarehouseName = updatedDocument.SourceWarehouseName,
                Status = updatedDocument.Status.ToString(),
                Notes = updatedDocument.Notes,
                CreatedAt = updatedDocument.CreatedAt,
                CreatedBy = updatedDocument.CreatedBy,
                Rows = enrichedRows
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding row to inventory document.", ex);
        }
    }

}
