using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for fiscal printer operations.
/// Supports receipt printing, refunds, daily closure, real-time status, and cash drawer management.
/// All operations are authorised for the <c>Admin</c> and <c>Manager</c> roles.
/// </summary>
[Route("api/v1/fiscal-printing")]
[Authorize(Roles = "Admin,Manager")]
public class FiscalPrintingController(
    IFiscalPrinterService fiscalPrinterService,
    FiscalPrinterStatusCache statusCache,
    ILogger<FiscalPrintingController> logger) : BaseApiController
{
    // -------------------------------------------------------------------------
    //  Print receipt
    // -------------------------------------------------------------------------

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
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Returns the most recent cached status of the specified fiscal printer.
    /// Status is updated every 10 seconds by the background monitoring service.
    /// Returns <c>null</c> fields when no cached entry is available (printer never polled).
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    [HttpGet("status/{printerId:guid}")]
    [ProducesResponseType(typeof(FiscalPrinterStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<FiscalPrinterStatus> GetStatus(Guid printerId)
    {
        var cached = statusCache.GetCachedStatus(printerId);
        if (cached is null)
        {
            return CreateNotFoundProblem(
                $"No status available for printer {printerId}. " +
                "The printer may not be configured or has not been polled yet.");
        }

        return Ok(cached);
    }

    // -------------------------------------------------------------------------
    //  Test connection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests the TCP/serial connection to the specified fiscal printer.
    /// Sends an ENQ enquiry frame and verifies the printer responds.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("test/{printerId:guid}")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> TestConnectionAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "TestConnectionAsync called | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var result = await fiscalPrinterService.TestConnectionAsync(printerId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error testing connection to printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Open drawer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Opens the cash drawer connected to the specified fiscal printer.
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("open-drawer/{printerId:guid}")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> OpenDrawerAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "OpenDrawerAsync called | PrinterId={PrinterId} User={User}",
                printerId, GetCurrentUser());

            var result = await fiscalPrinterService.OpenDrawerAsync(printerId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error opening drawer on printer {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Health check
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a health summary for the specified fiscal printer combining the cached status
    /// and a live connection test.
    /// The response indicates whether the printer is online, the paper level,
    /// and any critical conditions (fiscal memory full, daily closure required).
    /// </summary>
    /// <param name="printerId">Unique identifier of the target fiscal printer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("health/{printerId:guid}")]
    [ProducesResponseType(typeof(FiscalPrinterHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrinterHealthDto>> GetHealthAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a live connection test
            var testResult = await fiscalPrinterService.TestConnectionAsync(printerId, cancellationToken);

            // Use cached status if available, otherwise the live test result is enough
            var cachedStatus = statusCache.GetCachedStatus(printerId);

            var health = new FiscalPrinterHealthDto
            {
                PrinterId = printerId,
                IsOnline = testResult.Success,
                ConnectionError = testResult.Success ? null : testResult.ErrorMessage,
                CachedStatus = cachedStatus,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error checking health for printer {printerId}.", ex);
        }
    }
}
