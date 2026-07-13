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
    /// Seeds an inventory document with rows for all active products in the tenant.
    /// Creates a test inventory document with one row per product.
    /// </summary>
    /// <param name="request">Seed request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Seed operation result</returns>
    /// <response code="200">Returns the seed operation result</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/seed-all")]
    [RequireLicenseFeature("ProductManagement")]
    [ProducesResponseType(typeof(InventorySeedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedInventoryDocument(
        [FromBody] InventorySeedRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.SeedInventoryAsync(
                request,
                GetCurrentUser(),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Operation error during inventory seed");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while seeding inventory document.", ex);
        }
    }

    /// <summary>
    /// Validates an inventory document to identify data quality issues and estimate load time.
    /// Performs diagnostic checks without loading all rows into memory.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with issues and statistics</returns>
    /// <response code="200">Returns the validation result</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/validate")]
    [ProducesResponseType(typeof(InventoryValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ValidateInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = new InventoryValidationResultDto
            {
                DocumentId = documentId,
                Timestamp = DateTime.UtcNow,
                IsValid = true,
                Issues = new List<InventoryValidationIssue>(),
                Stats = new InventoryStats()
            };

            // 1. Verify document exists
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // 2. Count total rows without loading them
            var totalRows = await warehouseFacade.CountDocumentRowsAsync(documentId, cancellationToken);

            result.TotalRows = totalRows;

            if (totalRows == 0)
            {
                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Warning",
                    Code = "EMPTY_DOCUMENT",
                    Message = "Document has no rows",
                    Details = "The inventory document contains no line items"
                });
            }

            // 3. Identify rows with null ProductId or LocationId
            var rowsWithNullData = await warehouseFacade.GetRowsWithNullDataAsync(documentId, cancellationToken);

            foreach (var row in rowsWithNullData)
            {
                var missingFields = new List<string>();
                if (row.ProductId is null) missingFields.Add("ProductId");
                if (row.LocationId is null) missingFields.Add("LocationId");

                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Error",
                    Code = "MISSING_REQUIRED_FIELD",
                    Message = $"Row has missing required fields: {string.Join(", ", missingFields)}",
                    RowId = row.Id,
                    Details = $"This row cannot be processed without {string.Join(" and ", missingFields)}"
                });
                result.IsValid = false;
            }

            // 4. Get unique product and location IDs
            var (productIds, locationIds) = await warehouseFacade.GetUniqueProductAndLocationIdsAsync(documentId, cancellationToken);

            result.Stats.UniqueProducts = productIds.Count;
            result.Stats.UniqueLocations = locationIds.Count;

            // 5. Verify referenced products exist
            if (productIds.Any())
            {
                var missingProductIds = await warehouseFacade.ValidateProductsExistAsync(productIds, cancellationToken);
                if (missingProductIds.Any())
                {
                    result.Issues.Add(new InventoryValidationIssue
                    {
                        Severity = "Error",
                        Code = "MISSING_PRODUCTS",
                        Message = $"Document references {missingProductIds.Count} non-existent product(s)",
                        Details = $"Product IDs: {string.Join(", ", missingProductIds.Take(MAX_DISPLAYED_MISSING_IDS))}" +
                                 (missingProductIds.Count > MAX_DISPLAYED_MISSING_IDS ? $" and {missingProductIds.Count - MAX_DISPLAYED_MISSING_IDS} more" : "")
                    });
                    result.IsValid = false;
                }
            }

            // 6. Verify referenced locations exist
            if (locationIds.Any())
            {
                var missingLocationIds = await warehouseFacade.ValidateLocationsExistAsync(locationIds, cancellationToken);
                if (missingLocationIds.Any())
                {
                    result.Issues.Add(new InventoryValidationIssue
                    {
                        Severity = "Error",
                        Code = "MISSING_LOCATIONS",
                        Message = $"Document references {missingLocationIds.Count} non-existent location(s)",
                        Details = $"Location IDs: {string.Join(", ", missingLocationIds.Take(MAX_DISPLAYED_MISSING_IDS))}" +
                                 (missingLocationIds.Count > MAX_DISPLAYED_MISSING_IDS ? $" and {missingLocationIds.Count - MAX_DISPLAYED_MISSING_IDS} more" : "")
                    });
                    result.IsValid = false;
                }
            }

            // 7. Estimate load time based on row count
            // Optimized method: ~0.01 seconds per row (3 batch queries regardless of size)
            // Old method would be: ~0.12 seconds per row (3 queries per row)
            result.Stats.EstimatedLoadTimeSeconds = Math.Max(MIN_ESTIMATED_LOAD_TIME_SECONDS, totalRows * ESTIMATED_SECONDS_PER_ROW);

            if (totalRows > LARGE_DOCUMENT_THRESHOLD)
            {
                result.Issues.Add(new InventoryValidationIssue
                {
                    Severity = "Info",
                    Code = "LARGE_DOCUMENT",
                    Message = $"Document has {totalRows} rows - this is a large inventory",
                    Details = $"Estimated load time: {result.Stats.EstimatedLoadTimeSeconds:F1} seconds with optimized queries"
                });
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Completed validation for document {DocumentId} in {ElapsedMs}ms. " +
                "Total rows: {TotalRows}, Issues: {IssueCount}, Valid: {IsValid}",
                documentId, stopwatch.ElapsedMilliseconds, totalRows, result.Issues.Count, result.IsValid);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while validating inventory document.", ex);
        }
    }

    /// <summary>
    /// Gets all open inventory documents (Status == "Open").
    /// Returns documents ordered by creation date (most recent first).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of open inventory documents</returns>
    /// <response code="200">Returns the list of open inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/open")]
    [ProducesResponseType(typeof(List<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentDto>>> GetOpenInventoryDocuments(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get or create the inventory document type
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            // Query for Open status documents
            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (Prym.DTOs.Common.DocumentStatus)(int)DocumentStatus.Active,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = true
            };

            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var inventoryDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items is not null)
            {
                foreach (var doc in documentsResult.Items.OrderByDescending(d => d.CreatedAt))
                {
                    // Enrich rows with product and location data
                    var enrichedRows = doc.Rows is not null && doc.Rows.Any()
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
            }

            return Ok(inventoryDocuments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Returns lightweight headers (no rows) of all Open inventory documents for the current tenant.
    /// RowCount is calculated via SQL COUNT — no rows are loaded into memory, safe for any number of documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of lightweight open inventory document headers</returns>
    /// <response code="200">Returns the list of open inventory document headers</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/open-headers")]
    [ProducesResponseType(typeof(List<InventoryDocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentHeaderDto>>> GetOpenInventoryDocumentHeaders(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var headers = await warehouseFacade.GetOpenInventoryDocumentHeadersAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            return Ok(headers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving open inventory document headers.", ex);
        }
    }

    /// <summary>
    /// Cancels an inventory document without saving (changes status to "Cancelled").
    /// Does NOT apply stock adjustments.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the document was successfully cancelled</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Cancel the document via facade
            var cancelled = await warehouseFacade.CancelInventoryDocumentAsync(documentId, GetCurrentUser(), cancellationToken);

            if (!cancelled)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            logger.LogInformation("Cancelled inventory document {DocumentId} without applying adjustments", documentId);

            return Ok();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while cancelling inventory document.", ex);
        }
    }

    /// <summary>
    /// Finalizes ALL open inventory documents by applying stock adjustments to each one.
    /// This operation is transactional - if one fails, all changes are rolled back.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of finalized inventory documents</returns>
    /// <response code="200">Returns the list of finalized inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/finalize-all")]
    [ProducesResponseType(typeof(List<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InventoryDocumentDto>>> FinalizeAllOpenInventories(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        using var transaction = await warehouseFacade.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // Get all open inventory documents
            var inventoryDocType = await warehouseFacade.GetOrCreateInventoryDocumentTypeAsync(
                tenantContext.CurrentTenantId!.Value,
                cancellationToken);

            var queryParams = new DocumentHeaderQueryParameters
            {
                DocumentTypeId = inventoryDocType.Id,
                Status = (Prym.DTOs.Common.DocumentStatus)(int)DocumentStatus.Active,
                Page = 1,
                PageSize = MaxBulkOperationPageSize,
                IncludeRows = false
            };

            var documentsResult = await warehouseFacade.GetPagedDocumentHeadersAsync(queryParams, cancellationToken);

            var finalizedDocuments = new List<InventoryDocumentDto>();

            if (documentsResult?.Items is not null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();

                foreach (var doc in itemsList)
                {
                    // Call the existing finalize logic for each document
                    // We need to get the result as InventoryDocumentDto
                    var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(doc.Id, includeRows: true, cancellationToken);

                    if (documentHeader is not null)
                    {
                        // Process each row and apply stock adjustments (reuse logic from FinalizeInventoryDocument)
                        if (documentHeader.Rows is not null && documentHeader.Rows.Any())
                        {
                            foreach (var row in documentHeader.Rows)
                            {
                                try
                                {
                                    Guid productId = row.ProductId ?? Guid.Empty;
                                    Guid locationId = row.LocationId ?? Guid.Empty;
                                    Guid? lotId = null;

                                    if (productId == Guid.Empty || locationId == Guid.Empty)
                                    {
                                        logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                                        continue;
                                    }

                                    var newQuantity = row.Quantity;
                                    var existingStocks = await warehouseFacade.GetStockAsync(
                                        page: 1,
                                        pageSize: 1,
                                        productId: productId,
                                        locationId: locationId,
                                        lotId: lotId,
                                        cancellationToken: cancellationToken);

                                    var currentQuantity = existingStocks.Items.FirstOrDefault()?.Quantity ?? 0;
                                    var adjustmentQuantity = newQuantity - currentQuantity;

                                    if (adjustmentQuantity != 0)
                                    {
                                        _ = await warehouseFacade.ProcessAdjustmentMovementAsync(
                                            productId: productId,
                                            locationId: locationId,
                                            adjustmentQuantity: adjustmentQuantity,
                                            reason: "Inventory Count - Bulk Finalization",
                                            lotId: lotId,
                                            notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                            currentUser: GetCurrentUser(),
                                            movementDate: documentHeader.Date,
                                            cancellationToken: cancellationToken);

                                        var createStockDto = new CreateStockDto
                                        {
                                            ProductId = productId,
                                            StorageLocationId = locationId,
                                            LotId = lotId,
                                            Quantity = newQuantity,
                                            Notes = $"Adjusted by inventory document {documentHeader.Number}"
                                        };

                                        var updatedStock = await warehouseFacade.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);
                                        if (updatedStock is not null)
                                        {
                                            await warehouseFacade.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}", row.Id, doc.Id);
                                    throw; // Re-throw to trigger transaction rollback
                                }
                            }
                        }

                        // Close the document
                        var closedDocument = await warehouseFacade.ArchiveDocumentAsync(doc.Id, GetCurrentUser(), cancellationToken);

                        // Enrich rows with product and location data
                        var enrichedRows = closedDocument!.Rows is not null && closedDocument.Rows.Any()
                            ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
                            : new List<InventoryDocumentRowDto>();

                        finalizedDocuments.Add(new InventoryDocumentDto
                        {
                            Id = closedDocument.Id,
                            Number = closedDocument.Number,
                            Series = closedDocument.Series,
                            InventoryDate = closedDocument.Date,
                            WarehouseId = closedDocument.SourceWarehouseId,
                            WarehouseName = closedDocument.SourceWarehouseName,
                            Status = closedDocument.Status.ToString(),
                            Notes = closedDocument.Notes,
                            CreatedAt = closedDocument.CreatedAt,
                            CreatedBy = closedDocument.CreatedBy,
                            FinalizedAt = closedDocument.ArchivedAt,
                            FinalizedBy = closedDocument.ModifiedBy,
                            Rows = enrichedRows
                        });
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Successfully finalized {Count} inventory documents", finalizedDocuments.Count);

            return Ok(finalizedDocuments);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CreateInternalServerErrorProblem("An error occurred while finalizing all open inventory documents.", ex);
        }
    }

}
