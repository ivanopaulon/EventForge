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

    // -------------------------------------------------------------------------
    //  Network scan (wizard Step 2A)
    // -------------------------------------------------------------------------

}
