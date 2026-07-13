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
    /// Returns all StockMovement rows with Quantity &lt; 0 (legacy anomalies).
    /// </summary>
    [HttpGet("stock-reconciliation/negative-movements")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(NegativeMovementsReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNegativeMovements(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await warehouseFacade.GetNegativeMovementsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving negative movements.", ex);
        }
    }

    /// <summary>
    /// Normalises negative-quantity StockMovements by flipping their sign to positive.
    /// Stock levels are corrected accordingly.
    /// Pass dryRun=true to preview without persisting.
    /// </summary>
    [HttpPost("stock-reconciliation/negative-movements/fix")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    [ProducesResponseType(typeof(FixNegativeMovementsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> FixNegativeMovements(
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await warehouseFacade.FixNegativeMovementsAsync(dryRun, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while fixing negative movements.", ex);
        }
    }

    /// <summary>
    /// Recalculates ALL stock quantities in the current tenant from the full movement history,
    /// independently of any document rebuild run.
    /// Use this when stock balances are wrong but the underlying movements are already correct.
    /// Pass dryRun=true to preview the impact without persisting any changes.
    /// </summary>
    [HttpPost("stock-reconciliation/recalculate-all-stocks")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(RecalculateAllStocksResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecalculateAllStocks(
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ct = dryRun ? cancellationToken : CancellationToken.None;
            var result = await warehouseFacade.RecalculateAllStocksFromMovementsAsync(dryRun, GetCurrentUser(), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while recalculating all stock quantities.", ex);
        }
    }

}
