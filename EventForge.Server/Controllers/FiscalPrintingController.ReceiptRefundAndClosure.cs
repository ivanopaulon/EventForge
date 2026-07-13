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
    /// Prints a complete fiscal receipt on the specified printer.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="receipt">Receipt data including items, payments, and optional loyalty information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with <see cref="FiscalPrintResult"/> on success or controlled failure (e.g., NAK received).
    /// 400 Bad Request if <paramref name="receipt"/> is invalid.
    /// 500 Internal Server Error if an unhandled exception occurs.
    /// </returns>
    [HttpPost("print-receipt")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> PrintReceiptAsync(
        [FromQuery] Guid printerId,
        [FromBody] FiscalReceiptData receipt,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            logger.LogInformation(
                "PrintReceiptAsync called | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var result = await fiscalPrinterService.PrintReceiptAsync(printerId, receipt, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error printing receipt on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Cancel receipt
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cancels the currently open receipt on the specified printer (annullo scontrino).
    /// Only valid when a receipt is open on the printer.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("cancel-receipt/{printerId:guid}")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> CancelReceiptAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "CancelReceiptAsync called | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var result = await fiscalPrinterService.CancelCurrentReceiptAsync(printerId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error cancelling receipt on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Print refund (full)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prints a full refund receipt (reso totale) referencing the original receipt.
    /// All items are printed with negative quantities.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="refund">Refund data including original receipt reference, items, and payments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("print-refund")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> PrintRefundAsync(
        [FromQuery] Guid printerId,
        [FromBody] FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            logger.LogInformation(
                "PrintRefundAsync called | PrinterId={PrinterId} OriginalReceipt={OriginalReceipt} User={User}",
                printerId, refund.OriginalReceiptNumber, GetCurrentUser());

            var result = await fiscalPrinterService.PrintRefundReceiptAsync(printerId, refund, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error printing refund receipt on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Print partial refund
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prints a partial refund receipt, refunding only selected items from the original receipt.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="refund">Partial refund data. Only items listed are refunded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("partial-refund")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> PrintPartialRefundAsync(
        [FromQuery] Guid printerId,
        [FromBody] FiscalRefundData refund,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            logger.LogInformation(
                "PrintPartialRefundAsync called | PrinterId={PrinterId} Items={Count} User={User}",
                printerId, refund.Items.Count, GetCurrentUser());

            var result = await fiscalPrinterService.PrintPartialRefundAsync(printerId, refund, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error printing partial refund on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes the daily fiscal closure (Z-report / chiusura giornaliera) on the specified printer.
    /// This operation is irreversible and resets the daily totals.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("daily-closure/{printerId:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> DailyClosureAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "DailyClosureAsync called | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var result = await fiscalPrinterService.DailyClosureAsync(printerId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error executing daily closure on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Get status (cached)
    // -------------------------------------------------------------------------

}
