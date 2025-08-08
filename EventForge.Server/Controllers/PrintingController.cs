using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Printing;
using EventForge.Server.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for QZ Tray printing operations.
/// Provides endpoints for printer discovery, status checking, and print job management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PrintingController : BaseApiController
{
    private readonly IQzPrintingService _qzPrintingService;
    private readonly ILogger<PrintingController> _logger;

    /// <summary>
    /// Initializes a new instance of the PrintingController.
    /// </summary>
    /// <param name="qzPrintingService">QZ printing service</param>
    /// <param name="logger">Logger instance</param>
    public PrintingController(
        IQzPrintingService qzPrintingService,
        ILogger<PrintingController> logger)
    {
        _qzPrintingService = qzPrintingService;
        _logger = logger;
    }

    /// <summary>
    /// Discovers available printers through QZ Tray.
    /// </summary>
    /// <param name="request">Discovery request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered printers</returns>
    /// <response code="200">Returns the discovered printers</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error during discovery</response>
    [HttpPost("discover")]
    [ProducesResponseType(typeof(PrinterDiscoveryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrinterDiscoveryResponseDto>> DiscoverPrinters(
        [FromBody] PrinterDiscoveryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            _logger.LogInformation("Printer discovery requested by user: {User}", GetCurrentUser());

            var response = await _qzPrintingService.DiscoverPrintersAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogWarning("Printer discovery failed: {Error}", response.ErrorMessage);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in printer discovery endpoint");
            return CreateInternalServerErrorProblem("An error occurred while discovering printers", ex);
        }
    }

    /// <summary>
    /// Checks the status of a specific printer.
    /// </summary>
    /// <param name="request">Status check request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Printer status information</returns>
    /// <response code="200">Returns the printer status</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Printer not found</response>
    /// <response code="500">Internal server error during status check</response>
    [HttpPost("status")]
    [ProducesResponseType(typeof(PrinterStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrinterStatusResponseDto>> CheckPrinterStatus(
        [FromBody] PrinterStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            if (string.IsNullOrWhiteSpace(request.PrinterId))
            {
                return CreateValidationProblemDetails("Printer ID is required");
            }

            _logger.LogInformation("Printer status check requested for: {PrinterId} by user: {User}", 
                request.PrinterId, GetCurrentUser());

            var response = await _qzPrintingService.CheckPrinterStatusAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogWarning("Printer status check failed for {PrinterId}: {Error}", 
                    request.PrinterId, response.ErrorMessage);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in printer status check endpoint for printer: {PrinterId}", request?.PrinterId);
            return CreateInternalServerErrorProblem("An error occurred while checking printer status", ex);
        }
    }

    /// <summary>
    /// Submits a print job to QZ Tray.
    /// </summary>
    /// <param name="request">Print job submission request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Print job submission result</returns>
    /// <response code="200">Print job submitted successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error during submission</response>
    [HttpPost("print")]
    [ProducesResponseType(typeof(SubmitPrintJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmitPrintJobResponseDto>> SubmitPrintJob(
        [FromBody] SubmitPrintJobRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            if (request.PrintJob == null)
            {
                return CreateValidationProblemDetails("Print job is required");
            }

            if (string.IsNullOrWhiteSpace(request.PrintJob.PrinterId))
            {
                return CreateValidationProblemDetails("Printer ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.PrintJob.Content))
            {
                return CreateValidationProblemDetails("Print content is required");
            }

            // Set user information
            request.PrintJob.Username = GetCurrentUser();

            _logger.LogInformation("Print job submission requested: {JobTitle} to printer: {PrinterId} by user: {User}",
                request.PrintJob.Title, request.PrintJob.PrinterId, GetCurrentUser());

            var response = await _qzPrintingService.SubmitPrintJobAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogWarning("Print job submission failed for {JobTitle}: {Error}", 
                    request.PrintJob.Title, response.ErrorMessage);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in print job submission endpoint for job: {JobTitle}", request?.PrintJob?.Title);
            return CreateInternalServerErrorProblem("An error occurred while submitting the print job", ex);
        }
    }

    /// <summary>
    /// Gets the status of a specific print job.
    /// </summary>
    /// <param name="jobId">Print job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Print job status information</returns>
    /// <response code="200">Returns the print job status</response>
    /// <response code="404">Print job not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(PrintJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrintJobDto>> GetPrintJobStatus(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Print job status requested for: {JobId} by user: {User}", 
                jobId, GetCurrentUser());

            var printJob = await _qzPrintingService.GetPrintJobStatusAsync(jobId, cancellationToken);
            
            if (printJob == null)
            {
                return CreateNotFoundProblem($"Print job with ID {jobId} not found");
            }

            return Ok(printJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting print job status for job: {JobId}", jobId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving print job status", ex);
        }
    }

    /// <summary>
    /// Cancels a pending or active print job.
    /// </summary>
    /// <param name="jobId">Print job ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    /// <response code="200">Print job cancelled successfully</response>
    /// <response code="404">Print job not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("jobs/{jobId:guid}/cancel")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> CancelPrintJob(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Print job cancellation requested for: {JobId} by user: {User}", 
                jobId, GetCurrentUser());

            var result = await _qzPrintingService.CancelPrintJobAsync(jobId, cancellationToken);
            
            if (!result)
            {
                return CreateNotFoundProblem($"Print job with ID {jobId} not found or cannot be cancelled");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling print job: {JobId}", jobId);
            return CreateInternalServerErrorProblem("An error occurred while cancelling the print job", ex);
        }
    }

    /// <summary>
    /// Tests the connection to a QZ Tray instance.
    /// </summary>
    /// <param name="qzUrl">QZ Tray URL to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test result</returns>
    /// <response code="200">Returns the connection test result</response>
    /// <response code="400">Invalid QZ URL</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> TestConnection(
        [FromBody, Required] string qzUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(qzUrl))
            {
                return CreateValidationProblemDetails("QZ URL is required");
            }

            if (!Uri.TryCreate(qzUrl, UriKind.Absolute, out _))
            {
                return CreateValidationProblemDetails("Invalid QZ URL format");
            }

            _logger.LogInformation("QZ connection test requested for: {QzUrl} by user: {User}", 
                qzUrl, GetCurrentUser());

            var result = await _qzPrintingService.TestQzConnectionAsync(qzUrl, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing QZ connection for URL: {QzUrl}", qzUrl);
            return CreateInternalServerErrorProblem("An error occurred while testing the QZ connection", ex);
        }
    }

    /// <summary>
    /// Gets QZ Tray version information.
    /// </summary>
    /// <param name="qzUrl">QZ Tray URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QZ version information</returns>
    /// <response code="200">Returns the QZ version</response>
    /// <response code="400">Invalid QZ URL</response>
    /// <response code="404">QZ not accessible</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("version")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> GetQzVersion(
        [FromBody, Required] string qzUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(qzUrl))
            {
                return CreateValidationProblemDetails("QZ URL is required");
            }

            if (!Uri.TryCreate(qzUrl, UriKind.Absolute, out _))
            {
                return CreateValidationProblemDetails("Invalid QZ URL format");
            }

            _logger.LogInformation("QZ version request for: {QzUrl} by user: {User}", 
                qzUrl, GetCurrentUser());

            var version = await _qzPrintingService.GetQzVersionAsync(qzUrl, cancellationToken);
            
            if (version == null)
            {
                return CreateNotFoundProblem("Could not retrieve QZ version information");
            }

            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QZ version for URL: {QzUrl}", qzUrl);
            return CreateInternalServerErrorProblem("An error occurred while retrieving QZ version", ex);
        }
    }
}