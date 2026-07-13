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
    /// Returns the history of all daily closures across every printer and POS terminal
    /// (including non-fiscal closures performed without a printer), with optional date filters.
    /// </summary>
    [HttpGet("closures")]
    [ProducesResponseType(typeof(List<DailyClosureHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DailyClosureHistoryDto>>> GetAllClosureHistoryAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await fiscalPrinterService.GetAllClosureHistoryAsync(
                page, pageSize, fromDate, toDate, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                "Unexpected error retrieving all closure history.", ex);
        }
    }

    /// <summary>
    /// Returns the history of daily closures for the specified printer, with optional date filters.
    /// </summary>
    [HttpGet("closures/{printerId:guid}")]
    [ProducesResponseType(typeof(List<DailyClosureHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DailyClosureHistoryDto>>> GetClosureHistoryAsync(
        Guid printerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await fiscalPrinterService.GetClosureHistoryAsync(
                printerId, page, pageSize, fromDate, toDate, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error retrieving closure history for printer {printerId}.", ex);
        }
    }

    /// <summary>
    /// Downloads the PDF Z-report for the specified closure.
    /// On first request the PDF is generated on-demand and stored for future calls.
    /// </summary>
    [HttpGet("closures/{closureId:guid}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadClosurePdf(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "DownloadClosurePdf | ClosureId={ClosureId} User={User}",
                closureId, GetCurrentUser());

            var pdfBytes = await fiscalPrinterService.GenerateZReportPdfAsync(closureId, cancellationToken);

            if (pdfBytes is null || pdfBytes.Length == 0)
                return CreateNotFoundProblem($"Closure {closureId} not found or PDF could not be generated.");

            var fileName = $"ZReport_{closureId:N}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error generating PDF for closure {closureId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Reprint Z-report (5B.4)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reprints the Z-report for a previously executed closure.
    /// </summary>
    [HttpPost("closures/{closureId:guid}/reprint")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> ReprintZReportAsync(
        Guid closureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "ReprintZReportAsync | ClosureId={ClosureId} User={User}",
                closureId, GetCurrentUser());

            var result = await fiscalPrinterService.ReprintZReportAsync(closureId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error reprinting Z-report for closure {closureId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Agent System Printers proxy
    // -------------------------------------------------------------------------

}
