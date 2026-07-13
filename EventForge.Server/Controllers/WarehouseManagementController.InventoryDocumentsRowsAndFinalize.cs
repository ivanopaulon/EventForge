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
    /// Updates an inventory document's metadata (date, warehouse, notes).
    /// Can only update Draft documents.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="updateDto">Updated document data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid or document is not in Draft status</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("inventory/document/{documentId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInventoryDocument(Guid documentId, [FromBody] UpdateInventoryDocumentDto updateDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header to check status
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);

            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Only allow updating Draft documents (status is Open in entity)
            if (documentHeader.Status != Prym.DTOs.Common.DocumentStatus.Active)
            {
                return CreateValidationProblemDetails("Only Draft inventory documents can be updated. This document has already been finalized.");
            }

            // Update the document header fields via facade
            await warehouseFacade.UpdateDocumentHeaderFieldsAsync(
                documentId,
                updateDto.InventoryDate,
                updateDto.WarehouseId,
                updateDto.Notes,
                GetCurrentUser(),
                cancellationToken);

            logger.LogInformation("Updated inventory document {DocumentId} - Date: {Date}, Warehouse: {WarehouseId}",
                documentId, updateDto.InventoryDate, updateDto.WarehouseId);

            // Get the updated document with full details
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = updatedDocument!.Rows is not null && updatedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(updatedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
            {
                Id = updatedDocument.Id,
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
            return CreateInternalServerErrorProblem("An error occurred while updating inventory document.", ex);
        }
    }

    /// <summary>
    /// Updates an existing row in an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to update</param>
    /// <param name="rowDto">Updated row data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("inventory/document/{documentId:guid}/row/{rowId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInventoryDocumentRow(Guid documentId, Guid rowId, [FromBody] UpdateInventoryDocumentRowDto rowDto, CancellationToken cancellationToken = default)
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

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Active)
            {
                return CreateValidationProblemDetails("Cannot modify rows in a closed or cancelled inventory document.");
            }

            // Update the row via facade
            var updated = await warehouseFacade.UpdateInventoryRowAsync(
                rowId,
                rowDto.ProductId,
                rowDto.Quantity,
                rowDto.LocationId,
                rowDto.Notes,
                GetCurrentUser(),
                cancellationToken);

            if (!updated)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }

            // Get updated document
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
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
            return CreateInternalServerErrorProblem("An error occurred while updating row in inventory document.", ex);
        }
    }

    /// <summary>
    /// Deletes a row from an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated inventory document</returns>
    /// <response code="200">Returns the updated inventory document</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("inventory/document/{documentId:guid}/row/{rowId:guid}")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteInventoryDocumentRow(Guid documentId, Guid rowId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Check if document is still open
            if ((int)documentHeader.Status != (int)DocumentStatus.Active)
            {
                return CreateValidationProblemDetails("Cannot delete rows from a closed or cancelled inventory document.");
            }

            // Soft delete the row via facade
            var deleted = await warehouseFacade.DeleteInventoryRowAsync(rowId, GetCurrentUser(), cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }

            // Get updated document
            var updatedDocument = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);

            // Enrich all rows with product and location data
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
            return CreateInternalServerErrorProblem("An error occurred while deleting row from inventory document.", ex);
        }
    }

    /// <summary>
    /// Finalizes an inventory document and applies all stock adjustments.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Finalized inventory document</returns>
    /// <response code="200">Returns the finalized inventory document</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/document/{documentId:guid}/finalize")]
    [ProducesResponseType(typeof(InventoryDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FinalizeInventoryDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Get the document header with rows
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: true, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // Validate document is in Open status
            if (documentHeader.Status != Prym.DTOs.Common.DocumentStatus.Active)
            {
                logger.LogWarning(
                    "Cannot finalize inventory document {DocumentId}: status is {Status}, expected Active",
                    documentId, documentHeader.Status);

                return CreateValidationProblemDetails($"Cannot finalize document: status is '{documentHeader.Status}'. Only documents in 'Active' status can be finalized.");
            }

            // Validate document has rows
            if (documentHeader.Rows is null || !documentHeader.Rows.Any())
            {
                logger.LogWarning(
                    "Inventory document {DocumentId} has no rows to process",
                    documentId);

                return CreateValidationProblemDetails("Cannot finalize an inventory document with no rows.");
            }

            // Start timing and initialize counters
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var totalRows = documentHeader.Rows.Count;
            var processedRows = 0;
            var skippedRows = 0;


            // Validation: verify that all ProductId and LocationId exist before processing
            var productIds = documentHeader.Rows.Where(r => r.ProductId.HasValue).Select(r => r.ProductId!.Value).Distinct().ToList();
            var locationIds = documentHeader.Rows.Where(r => r.LocationId.HasValue).Select(r => r.LocationId!.Value).Distinct().ToList();

            var missingProducts = await warehouseFacade.ValidateProductsExistAsync(productIds, cancellationToken);
            if (missingProducts.Any())
            {
                return CreateValidationProblemDetails($"Document contains {missingProducts.Count} non-existent product(s). Cannot finalize.");
            }

            // Validate locations
            var missingLocations = await warehouseFacade.ValidateLocationsExistAsync(locationIds, cancellationToken);
            if (missingLocations.Any())
            {
                return CreateValidationProblemDetails($"Document contains {missingLocations.Count} non-existent location(s). Cannot finalize.");
            }

            // Process each row and apply stock adjustments
            if (documentHeader.Rows is not null && documentHeader.Rows.Any())
            {

                foreach (var row in documentHeader.Rows)
                {
                    try
                    {
                        // Use ProductId and LocationId directly from the row
                        Guid productId = row.ProductId ?? Guid.Empty;
                        Guid locationId = row.LocationId ?? Guid.Empty;

                        // DocumentRowDto does not contain LotId in current DTO.
                        // Preserve compilation and behaviour by treating lot as unknown here.
                        // If lot information is required, extend DocumentRowDto to include LotId at source.
                        Guid? lotId = null;

                        // Validate we have both IDs
                        if (productId == Guid.Empty || locationId == Guid.Empty)
                        {
                            logger.LogWarning("Row {RowId} missing ProductId or LocationId, skipping", row.Id);
                            skippedRows++;
                            continue;
                        }

                        var newQuantity = row.Quantity;

                        // Get current stock level
                        var existingStocks = await warehouseFacade.GetStockAsync(
                            page: 1,
                            pageSize: 1,
                            productId: productId,
                            locationId: locationId,
                            lotId: lotId,
                            cancellationToken: cancellationToken);

                        var currentQuantity = existingStocks.Items.FirstOrDefault()?.Quantity ?? 0;
                        var adjustmentQuantity = newQuantity - currentQuantity;

                        // Only apply adjustment if there's a difference
                        if (adjustmentQuantity != 0)
                        {
                            // 1) Create stock adjustment movement (keeps audit trail)
                            // Use the document's InventoryDate for the movement date
                            _ = await warehouseFacade.ProcessAdjustmentMovementAsync(
                                productId: productId,
                                locationId: locationId,
                                adjustmentQuantity: adjustmentQuantity,
                                reason: "Inventory Count",
                                lotId: lotId,
                                notes: $"Inventory adjustment from document {documentHeader.Number}. Previous: {currentQuantity}, New: {newQuantity}",
                                currentUser: GetCurrentUser(),
                                movementDate: documentHeader.Date,
                                cancellationToken: cancellationToken);


                            processedRows++;

                            // 2) Ensure the Stocks table is updated to reflect the counted quantity
                            var createStockDto = new CreateStockDto
                            {
                                ProductId = productId,
                                StorageLocationId = locationId,
                                LotId = lotId,
                                Quantity = newQuantity,
                                Notes = $"Adjusted by inventory document {documentHeader.Number}"
                                // Other fields (ReservedQuantity, MinimumLevel etc.) can be left null or set if known
                            };

                            // This call will create or update a Stock record
                            var updatedStock = await warehouseFacade.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser(), cancellationToken);

                            // Verify stock was successfully created/updated
                            if (updatedStock is not null)
                            {
                                await warehouseFacade.UpdateLastInventoryDateAsync(updatedStock.Id, DateTime.UtcNow, cancellationToken);
                            }
                            else
                            {
                                // If stock creation/update fails, this is a critical error - propagate it
                                throw new InvalidOperationException($"Failed to create or update stock for product {productId} at location {locationId}");
                            }
                        }
                        else
                        {
                            processedRows++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing inventory row {RowId} in document {DocumentId}",
                            row.Id, documentId);
                        // Continue processing other rows even if one fails
                    }
                }
            }

            // Now close the document
            var closedDocument = await warehouseFacade.ArchiveDocumentAsync(documentId, GetCurrentUser(), cancellationToken);

            // Enrich rows with product and location data
            var enrichedRows = closedDocument!.Rows is not null && closedDocument.Rows.Any()
                ? await warehouseFacade.EnrichInventoryDocumentRowsAsync(closedDocument.Rows, cancellationToken)
                : new List<InventoryDocumentRowDto>();

            var result = new InventoryDocumentDto
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
            };

            stopwatch.Stop();
            logger.LogInformation(
                "Completed finalization of inventory document {DocumentId} ({DocumentNumber}) in {ElapsedMs}ms. " +
                "Rows processed: {ProcessedRows}, Rows skipped: {SkippedRows}, Total: {TotalRows}",
                documentId, documentHeader.Number, stopwatch.ElapsedMilliseconds,
                processedRows, skippedRows, totalRows);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while finalizing inventory document.", ex);
        }
    }

}
