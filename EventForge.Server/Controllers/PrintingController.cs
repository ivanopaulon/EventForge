using EventForge.DTOs.Printing;
using EventForge.Server.Services;
using EventForge.Server.Services.Interfaces;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Printing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Controllers;

/// <summary>
/// Request model for QZ Tray signing demonstration
/// </summary>
public class QzSigningDemoRequest
{
    /// <summary>
    /// QZ Tray function name to call
    /// </summary>
    public string CallName { get; set; } = "qz.printers.find";

    /// <summary>
    /// Parameters for the QZ Tray function
    /// </summary>
    public object[] Parameters { get; set; } = Array.Empty<object>();
}

/// <summary>
/// Controller for QZ Tray printing operations.
/// Provides endpoints for printer discovery, status checking, and print job management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PrintingController : BaseApiController
{
    private readonly IQzPrintingService _qzPrintingService;
    private readonly ITenantContext _tenantContext;
    private readonly QzDigitalSignatureService _signatureService;
    private readonly QzSigner _qzSigner;
    private readonly QzWebSocketClient _qzWebSocketClient;
    private readonly ILogger<PrintingController> _logger;

    /// <summary>
    /// Initializes a new instance of the PrintingController.
    /// </summary>
    /// <param name="qzPrintingService">QZ printing service</param>
    /// <param name="signatureService">QZ digital signature service</param>
    /// <param name="qzSigner">QZ SHA512 signer service</param>
    /// <param name="qzWebSocketClient">QZ WebSocket client service</param>
    /// <param name="logger">Logger instance</param>
    public PrintingController(
        IQzPrintingService qzPrintingService,
        QzDigitalSignatureService signatureService,
        QzSigner qzSigner,
        QzWebSocketClient qzWebSocketClient,
        ILogger<PrintingController> logger,
        ITenantContext tenantContext)
    {
        _qzPrintingService = qzPrintingService;
        _signatureService = signatureService;
        _qzSigner = qzSigner;
        _qzWebSocketClient = qzWebSocketClient;
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

    /// <summary>
    /// Tests the enhanced QZ Tray digital signature functionality with complete certificate chain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result with signed payload structure</returns>
    /// <response code="200">Returns the signature test result</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("test-signature")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> TestEnhancedSignature(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enhanced signature test requested by user: {User}", GetCurrentUser());

            // Test configuration validation
            var isValidConfig = await _qzPrintingService.ValidateSignatureConfigurationAsync();

            if (!isValidConfig)
            {
                var configException = new InvalidOperationException("QZ Tray signature configuration is not valid");
                return CreateInternalServerErrorProblem("QZ Tray signature configuration is not valid", configException);
            }

            // Create a sample print payload for testing
            var samplePrintJob = new PrintJobDto
            {
                Id = Guid.NewGuid(),
                PrinterId = "TEST_PRINTER",
                PrinterName = "Test Receipt Printer",
                Title = "Signature Test Receipt",
                ContentType = PrintContentType.Raw,
                Content = "SIGNATURE TEST\n=============\nTimestamp: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\nTest successful!",
                Copies = 1,
                Priority = PrintJobPriority.Normal,
                Status = PrintJobStatus.Queued,
                SubmittedAt = DateTime.UtcNow
            };

            var submitRequest = new SubmitPrintJobRequestDto
            {
                PrintJob = samplePrintJob,
                ValidatePrinter = false, // Skip printer validation for test
                WaitForCompletion = false
            };

            // This will internally use the enhanced signature service
            var result = await _qzPrintingService.SubmitPrintJobAsync(submitRequest, cancellationToken);

            var testResult = new
            {
                ConfigurationValid = isValidConfig,
                SignatureTestResult = result.Success,
                ErrorMessage = result.ErrorMessage,
                TestPayload = new
                {
                    JobId = samplePrintJob.Id,
                    PrinterId = samplePrintJob.PrinterId,
                    Content = samplePrintJob.Content,
                    Timestamp = DateTime.UtcNow
                },
                Message = result.Success
                    ? "Enhanced QZ Tray signature test completed successfully!"
                    : "Signature test failed - check error message for details",
                Features = new[]
                {
                    "Complete certificate chain with intermediate markers",
                    "UTC timestamp in milliseconds",
                    "Short base64 UID generation",
                    "RSA-SHA256 signature on complete payload",
                    "Position field as per QZ Tray demo",
                    "Payload structure matches QZ Tray requirements"
                }
            };

            return Ok(testResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing enhanced QZ signature");
            return CreateInternalServerErrorProblem("An error occurred while testing the enhanced signature", ex);
        }
    }

    /// <summary>
    /// Gets the QZ Tray certificate chain for qz.api.setCertificatePromise.
    /// Standard QZ Tray endpoint compatible with text/plain content type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete certificate chain as text/plain</returns>
    /// <response code="200">Returns the certificate chain</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("qz/certificate")]
    [Authorize]
    [Produces("text/plain")]
    [ResponseCache(Duration = 300)] // 5 minute cache
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetQzCertificate(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("QZ certificate requested by user: {User}", GetCurrentUser());

            var certificateChain = await _signatureService.GetCertificateChainAsync();

            return Content(certificateChain, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QZ certificate chain");
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Content("Internal server error occurred while retrieving certificate", "text/plain");
        }
    }

    /// <summary>
    /// Signs a challenge string for qz.api.setSignaturePromise.
    /// Standard QZ Tray endpoint compatible with text/plain content type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded signature as text/plain</returns>
    /// <response code="200">Returns the signature</response>
    /// <response code="400">Invalid or empty challenge</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("qz/sign")]
    [Authorize]
    [Consumes("text/plain")]
    [Produces("text/plain")]
    [RequestSizeLimit(32 * 1024)] // 32KB limit
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SignQzChallenge(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("QZ challenge signing requested by user: {User}", GetCurrentUser());

            // Read challenge from request body
            using var reader = new StreamReader(Request.Body);
            var challenge = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(challenge))
            {
                _logger.LogWarning("Empty challenge received for QZ signing");
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Content("Challenge cannot be empty", "text/plain");
            }

            var signature = await _signatureService.SignChallengeAsync(challenge);

            _logger.LogDebug("QZ challenge signed successfully for user: {User}", GetCurrentUser());
            return Content(signature, "text/plain");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid challenge for QZ signing");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Content($"Invalid challenge: {ex.Message}", "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing QZ challenge");
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Content("Internal server error occurred while signing challenge", "text/plain");
        }
    }

    /// <summary>
    /// Demonstrates the new QZ Tray SHA512withRSA signing capability
    /// </summary>
    /// <param name="request">Test signing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signing demonstration results</returns>
    /// <response code="200">Returns the signing demonstration results</response>
    /// <response code="500">Internal server error during signing</response>
    [HttpPost("qz/demo-sha512-signing")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> DemoSha512Signing([FromBody] QzSigningDemoRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("QZ Tray SHA512withRSA signing demonstration requested by user: {User}", GetCurrentUser());

            // Use provided request or create a default one
            var callName = request?.CallName ?? "qz.printers.find";
            var parameters = request?.Parameters ?? new object[] { };
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Demonstrate the new QzSigner service
            var signature = await _qzSigner.Sign(callName, parameters, timestamp);

            var result = new
            {
                SignatureMethod = "SHA512withRSA with PKCS#1 v1.5 padding",
                Request = new
                {
                    Call = callName,
                    Parameters = parameters,
                    Timestamp = timestamp,
                    TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
                },
                Signature = signature,
                SignatureLength = signature.Length,
                JsonPayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    call = callName,
                    @params = parameters,
                    timestamp = timestamp
                }),
                EnvironmentConfiguration = new
                {
                    PrivateKeyPath = Environment.GetEnvironmentVariable("QZ_PRIVATE_KEY_PATH") ?? "private-key.pem (default)",
                    WebSocketUri = Environment.GetEnvironmentVariable("QZ_WS_URI") ?? "ws://localhost:8181 (default)"
                },
                Documentation = "See /docs/QZ_TRAY_INTEGRATION.md for usage examples and configuration"
            };

            _logger.LogInformation("QZ Tray SHA512withRSA signing demonstration completed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during QZ Tray SHA512withRSA signing demonstration");
            return CreateInternalServerErrorProblem("An error occurred during the signing demonstration", ex);
        }
    }
}