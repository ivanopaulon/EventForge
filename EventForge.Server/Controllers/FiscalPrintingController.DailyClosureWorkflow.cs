using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Station;
using System.Text.Json;


namespace EventForge.Server.Controllers;

public partial class FiscalPrintingController
{
    /// <summary>
    /// Lightweight "morning check": returns whether the previous business day's daily
    /// closure was performed. DB-only — safe to call even when the printer is offline.
    /// </summary>
    [HttpGet("daily-closure/morning-check/{printerId:guid}")]
    [ProducesResponseType(typeof(PreviousDayClosureStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PreviousDayClosureStatusDto>> GetPreviousDayClosureStatusAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug(
                "GetPreviousDayClosureStatusAsync | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var status = await fiscalPrinterService.GetPreviousDayClosureStatusAsync(printerId, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error checking previous-day closure status for printer {printerId}.", ex);
        }
    }

    /// <summary>
    /// Returns a pre-check summary for the daily fiscal closure of the specified printer.
    /// Includes whether there is an open receipt (blocks closure), drawer state, and
    /// today's receipt/total summary.
    /// </summary>
    [HttpGet("daily-closure/precheck/{printerId:guid}")]
    [ProducesResponseType(typeof(DailyClosurePreCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailyClosurePreCheckDto>> GetDailyClosurePreCheckAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "GetDailyClosurePreCheckAsync | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var preCheck = await fiscalPrinterService.GetDailyClosurePreCheckAsync(printerId, cancellationToken);
            return Ok(preCheck);
        }
        catch (InvalidOperationException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error getting pre-check for printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure – execute (5B.4)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes the daily fiscal closure (Z-report) for the specified printer.
    /// This operation is irreversible. Caller should invoke the pre-check endpoint first.
    /// </summary>
    [HttpPost("daily-closure/execute/{printerId:guid}")]
    [ProducesResponseType(typeof(DailyClosureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailyClosureResultDto>> ExecuteDailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operatorName = GetCurrentUser();
            logger.LogInformation(
                "ExecuteDailyClosureAsync | PrinterId={PrinterId} Operator={Op}",
                printerId, operatorName);

            var result = await fiscalPrinterService.ExecuteDailyClosureAsync(
                printerId, operatorName, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error executing daily closure for printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure – no-printer path (NonFiscale / SoloDatabase)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes a daily closure for a POS terminal that has no fiscal printer configured.
    /// Aggregates today's session totals from the database and saves a closure record with
    /// <c>ClosureType = NonFiscale</c>. No hardware communication occurs.
    /// </summary>
    [HttpPost("daily-closure/execute-no-printer/{posId:guid}")]
    [ProducesResponseType(typeof(DailyClosureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailyClosureResultDto>> ExecuteNoPrinterDailyClosureAsync(
        Guid posId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operatorName = GetCurrentUser();
            logger.LogInformation(
                "ExecuteNoPrinterDailyClosureAsync | PosId={PosId} Operator={Op}", posId, operatorName);

            var router = (FiscalPrinterServiceRouter)fiscalPrinterService;
            var result = await router.ExecuteNoPrinterDailyClosureAsync(posId, operatorName, cancellationToken);

            if (!result.Success && result.ErrorMessage?.Contains("non trovato") == true)
                return CreateNotFoundProblem(result.ErrorMessage);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error executing no-printer daily closure for POS {posId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Retry pending fiscal closure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retries the hardware fiscal Z-report for a closure record whose
    /// <c>FiscalClosurePending</c> flag is <c>true</c>.
    /// Should be called once the printer is back online.
    /// On success, clears the pending flag and updates the closure type to <c>Fiscale</c>.
    /// </summary>
    [HttpPost("daily-closure/{closureId:guid}/retry-fiscal")]
    [ProducesResponseType(typeof(DailyClosureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailyClosureResultDto>> RetryFiscalClosureAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "RetryFiscalClosureAsync | ClosureId={ClosureId} User={User}", closureId, GetCurrentUser());

            var result = await fiscalPrinterService.RetryFiscalClosureAsync(closureId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error retrying fiscal closure {closureId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Closure history (5B.4)
    // -------------------------------------------------------------------------

}
