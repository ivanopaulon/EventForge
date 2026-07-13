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
    /// Calculates stock reconciliation preview.
    /// Analyzes stock discrepancies based on documents, inventories, and manual movements.
    /// This endpoint does NOT modify data - it only calculates and returns preview.
    /// </summary>
    /// <param name="request">Reconciliation request with filters and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation result with calculated quantities and discrepancies</returns>
    [HttpPost("stock-reconciliation/calculate")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(StockReconciliationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateStockReconciliation(
        [FromBody] StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await warehouseFacade.CalculateReconciledStockAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating stock reconciliation.", ex);
        }
    }

    /// <summary>
    /// Returns the stock ids that match the reconciliation filters.
    /// </summary>
    /// <param name="request">Reconciliation request with filters and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock ids that should be processed by the client</returns>
    [HttpGet("stock-reconciliation/stock-ids")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockReconciliationStockIds(
        [FromQuery] StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await warehouseFacade.GetStockIdsForReconciliationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock reconciliation ids.", ex);
        }
    }

    /// <summary>
    /// Calculates stock reconciliation for a specific batch of stock ids.
    /// </summary>
    /// <param name="request">Batch request with stock ids and shared filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation result for the requested batch</returns>
    [HttpPost("stock-reconciliation/calculate-batch")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(StockReconciliationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateStockReconciliationBatch(
        [FromBody] StockReconciliationBatchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.Filters is null)
            {
                return BadRequest(new { message = "Filters payload is required." });
            }

            if (request.StockIds is null || request.StockIds.Count == 0)
            {
                return BadRequest(new { message = "At least one stock id is required." });
            }

            var result = await warehouseFacade.CalculateReconciledStockForStocksAsync(
                request.StockIds,
                request.Filters,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating stock reconciliation batch.", ex);
        }
    }

    /// <summary>
    /// Applies stock reconciliation corrections to selected items.
    /// Updates stock quantities and creates adjustment movements with full audit trail.
    /// This operation is atomic - either all updates succeed or all fail.
    /// </summary>
    /// <param name="request">Apply request with items to update and reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the apply operation</returns>
    [HttpPost("stock-reconciliation/apply")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(StockReconciliationApplyResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyStockReconciliation(
        [FromBody] StockReconciliationApplyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Unknown";
            var result = await warehouseFacade.ApplyReconciliationAsync(request, currentUser, cancellationToken);

            if (!result.Success)
            {
                return CreateValidationProblemDetails(result.ErrorMessage ?? "An error occurred while applying stock reconciliation.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while applying stock reconciliation.", ex);
        }
    }

    /// <summary>
    /// Exports stock reconciliation report as Excel file.
    /// Includes summary, details, and source movements.
    /// </summary>
    /// <param name="request">Reconciliation request with filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file</returns>
    [HttpGet("stock-reconciliation/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportStockReconciliation(
        [FromQuery] StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var fileBytes = await warehouseFacade.ExportReconciliationReportAsync(request, cancellationToken);

            if (fileBytes is null || fileBytes.Length == 0)
            {
                logger.LogWarning("Export generated no data or feature not yet implemented");
                return CreateNotImplementedProblem("Excel export feature not yet implemented");
            }

            var fileName = $"StockReconciliation_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while exporting stock reconciliation report.", ex);
        }
    }

    /// <summary>
    /// Previews which stock movements would be rebuilt from approved/closed documents (dry-run).
    /// Does NOT create any movements - returns a preview of what would be created.
    /// </summary>
    /// <param name="request">Rebuild request with optional filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview result showing rows that would have movements created</returns>
    [HttpPost("stock-reconciliation/rebuild-movements/preview")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(RebuildMovementsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RebuildMovementsPreview(
        [FromBody] RebuildMovementsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.DryRun = true; // force dry-run for preview
            var result = await warehouseFacade.RebuildMissingMovementsFromDocumentsAsync(
                request, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing stock movement rebuild.", ex);
        }
    }

    /// <summary>
    /// Rebuilds missing stock movements from approved/closed documents.
    /// Creates stock movements for document rows that do not yet have a corresponding movement.
    /// </summary>
    /// <param name="request">Rebuild request with optional filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result showing created, skipped, and failed movements</returns>
    [HttpPost("stock-reconciliation/rebuild-movements/execute")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(RebuildMovementsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RebuildMovementsExecute(
        [FromBody] RebuildMovementsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.DryRun = false; // force execute
            // Use CancellationToken.None for write operations: once the rebuild begins
            // committing movements we must not abort mid-flight if the client disconnects
            // or times out, as that would leave stock data in an inconsistent state.
            var result = await warehouseFacade.RebuildMissingMovementsFromDocumentsAsync(
                request, GetCurrentUser(), CancellationToken.None);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while executing stock movement rebuild.", ex);
        }
    }

}
