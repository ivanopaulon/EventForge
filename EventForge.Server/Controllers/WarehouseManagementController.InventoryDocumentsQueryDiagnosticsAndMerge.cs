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
    /// Gets paginated rows from an inventory document.
    /// Useful for loading large documents incrementally without timeouts.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of inventory document rows</returns>
    /// <response code="200">Returns the paginated rows</response>
    /// <response code="400">If pagination parameters are invalid</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("inventory/documents/{documentId:guid}/rows")]
    [ProducesResponseType(typeof(PagedResult<InventoryDocumentRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventoryDocumentRows(
        Guid documentId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {

            // 1. Verify document exists
            var documentHeader = await warehouseFacade.GetDocumentHeaderByIdAsync(documentId, includeRows: false, cancellationToken);
            if (documentHeader is null)
            {
                return CreateNotFoundProblem($"Inventory document with ID {documentId} was not found.");
            }

            // 2-3. Get paginated rows via facade
            var (documentRows, totalRows) = await warehouseFacade.GetDocumentRowsPagedAsync(
                documentId,
                pagination.Page,
                pagination.PageSize,
                cancellationToken);

            // 4. Enrich using optimized batch method
            var enrichedRows = await warehouseFacade.EnrichInventoryDocumentRowsAsync(documentRows, cancellationToken);

            var result = new PagedResult<InventoryDocumentRowDto>
            {
                Items = enrichedRows,
                TotalCount = totalRows,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };


            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while fetching inventory document rows.", ex);
        }
    }

    /// <summary>
    /// Cancels ALL open inventory documents without saving (changes status to "Cancelled").
    /// Does NOT apply stock adjustments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of inventory documents cancelled</returns>
    /// <response code="200">Returns the count of cancelled inventory documents</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/cancel-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CancelAllOpenInventories(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

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

            int cancelledCount = 0;

            if (documentsResult?.Items is not null && documentsResult.Items.Any())
            {
                var itemsList = documentsResult.Items.ToList();

                // Cancel all documents in batch via facade
                var documentIds = itemsList.Select(d => d.Id).ToList();
                cancelledCount = await warehouseFacade.CancelInventoryDocumentsBatchAsync(documentIds, GetCurrentUser(), cancellationToken);

                logger.LogInformation("Successfully cancelled {Count} inventory documents without applying adjustments", cancelledCount);
            }

            return Ok(cancelledCount);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while cancelling all open inventory documents.", ex);
        }
    }

    /// <summary>
    /// Diagnoses an inventory document to identify data quality issues.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagnostic report with issues and statistics</returns>
    /// <response code="200">Returns the diagnostic report</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/diagnose")]
    [ProducesResponseType(typeof(InventoryDiagnosticReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DiagnoseInventoryDocument(Guid documentId, CancellationToken cancellationToken)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var report = await warehouseFacade.DiagnoseDocumentAsync(documentId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while diagnosing the inventory document.", ex);
        }
    }

    /// <summary>
    /// Automatically repairs an inventory document based on the specified options.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="options">Repair options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Repair result with actions performed</returns>
    /// <response code="200">Returns the repair result</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/auto-repair")]
    [ProducesResponseType(typeof(InventoryRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AutoRepairInventoryDocument(Guid documentId, [FromBody] InventoryAutoRepairOptionsDto options, CancellationToken cancellationToken)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.AutoRepairDocumentAsync(documentId, options, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while auto-repairing the inventory document.", ex);
        }
    }

    /// <summary>
    /// Manually repairs a specific row in an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowId">Row ID to repair</param>
    /// <param name="repairData">Repair data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success indicator</returns>
    /// <response code="200">If the row was repaired successfully</response>
    /// <response code="404">If the document or row is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPatch("inventory/documents/{documentId:guid}/rows/{rowId:guid}/repair")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RepairInventoryRow(Guid documentId, Guid rowId, [FromBody] InventoryRowRepairDto repairData, CancellationToken cancellationToken)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var success = await warehouseFacade.RepairRowAsync(documentId, rowId, repairData, GetCurrentUser(), cancellationToken);
            if (!success)
            {
                return CreateNotFoundProblem($"Row with ID {rowId} was not found in document {documentId}.");
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while repairing the inventory row.", ex);
        }
    }

    /// <summary>
    /// Removes problematic rows from an inventory document.
    /// </summary>
    /// <param name="documentId">Inventory document ID</param>
    /// <param name="rowIds">List of row IDs to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows removed</returns>
    /// <response code="200">Returns the number of rows removed</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/{documentId:guid}/remove-problematic-rows")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveProblematicRows(Guid documentId, [FromBody] List<Guid> rowIds, CancellationToken cancellationToken)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var removedCount = await warehouseFacade.RemoveProblematicRowsAsync(documentId, rowIds, GetCurrentUser(), cancellationToken);
            return Ok(removedCount);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing problematic rows.", ex);
        }
    }

    /// <summary>
    /// Returns a preview of the merge operation for the selected inventory documents.
    /// Use this before calling /merge to show the user what will happen.
    /// No data is modified by this call.
    /// </summary>
    /// <param name="documentIds">List of inventory document IDs to preview</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview of the merge operation</returns>
    /// <response code="200">Returns the merge preview</response>
    /// <response code="400">If validation fails</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/merge-preview")]
    [ProducesResponseType(typeof(MergeInventoryDocumentsPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewMergeInventoryDocuments(
        [FromBody] List<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            if (documentIds is null || documentIds.Count < 2)
            {
                ModelState.AddModelError("documentIds", "At least 2 documents are required to preview a merge.");
                return CreateValidationProblemDetails();
            }


            var preview = await warehouseFacade.PreviewMergeInventoryDocumentsAsync(documentIds, cancellationToken);

            if (preview.SourceDocuments.Count != documentIds.Count)
            {
                ModelState.AddModelError("documentIds", "One or more source documents not found.");
                return CreateValidationProblemDetails();
            }

            return Ok(preview);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing the inventory document merge.", ex);
        }
    }

    /// <summary>
    /// Merges the selected inventory documents into one finalized document.
    /// Source documents (excluding the target/base) are soft-deleted.
    /// Row merging: ProductId + LocationId matching => quantities summed.
    /// </summary>
    /// <param name="mergeDto">Merge request with source document IDs and optional target document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the merge operation</returns>
    /// <response code="200">Returns the merge result</response>
    /// <response code="400">If validation fails</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("inventory/documents/merge")]
    [ProducesResponseType(typeof(MergeInventoryDocumentsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MergeInventoryDocuments(
        [FromBody] MergeInventoryDocumentsDto mergeDto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            if (mergeDto.SourceDocumentIds.Count < 2)
            {
                ModelState.AddModelError("SourceDocumentIds", "At least 2 documents are required to merge.");
                return CreateValidationProblemDetails();
            }

            if (mergeDto.TargetDocumentId.HasValue && !mergeDto.SourceDocumentIds.Contains(mergeDto.TargetDocumentId.Value))
            {
                ModelState.AddModelError("TargetDocumentId", "TargetDocumentId must be included in SourceDocumentIds. SourceDocumentIds should contain all documents to merge, including the target.");
                return CreateValidationProblemDetails();
            }


            var result = await warehouseFacade.MergeInventoryDocumentsAsync(mergeDto, GetCurrentUser(), cancellationToken);

            logger.LogInformation(
                "Merged inventory documents into {MergedNumber}. TotalRows: {TotalRows}, MergedRows: {MergedRows}, CopiedRows: {CopiedRows}, SoftDeleted: {SoftDeleted}.",
                result.MergedDocumentNumber, result.TotalRows, result.MergedRows, result.CopiedRows, result.SoftDeletedDocumentIds.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while merging inventory documents.", ex);
        }
    }

}
