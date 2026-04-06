using EventForge.DTOs.FiscalPrinting;
using EventForge.DTOs.Station;
using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.Station;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for fiscal printer operations.
/// Supports receipt printing, refunds, daily closure, real-time status, cash drawer management,
/// network scanning, printer info reading, and wizard setup.
/// All operations are authorised for the <c>Admin</c> and <c>Manager</c> roles.
/// </summary>
[Route("api/v1/fiscal-printing")]
[Authorize(Roles = "Admin,Manager")]
public class FiscalPrintingController(
    IFiscalPrinterService fiscalPrinterService,
    FiscalPrinterStatusCache statusCache,
    IStationService stationService,
    ITenantContext tenantContext,
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

    // -------------------------------------------------------------------------
    //  Network scan (wizard Step 2A)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scans the specified subnet prefix for devices responding on the given TCP port.
    /// Probes addresses from <c>{subnetPrefix}.1</c> to <c>{subnetPrefix}.254</c> concurrently.
    /// Results include the IP, round-trip time, and whether the device answered a Custom ENQ frame.
    /// </summary>
    /// <param name="subnetPrefix">Subnet to scan (e.g., <c>192.168.1</c>).</param>
    /// <param name="port">TCP port to probe (default 9100).</param>
    /// <param name="timeoutMs">Per-host timeout in milliseconds (default 300).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("scan-network")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<NetworkScanResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<NetworkScanResultDto>>> ScanNetworkAsync(
        [FromQuery] string subnetPrefix,
        [FromQuery] int port = 9100,
        [FromQuery] int timeoutMs = 300,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subnetPrefix))
            return CreateValidationProblemDetails();

        try
        {
            logger.LogInformation(
                "ScanNetworkAsync | Subnet={Subnet} Port={Port} TimeoutMs={Timeout} User={User}",
                subnetPrefix, port, timeoutMs, GetCurrentUser());

            var results = await fiscalPrinterService.ScanNetworkAsync(
                subnetPrefix, port, timeoutMs, cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error scanning network {subnetPrefix}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Printer info by address (wizard Step 3)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads model, firmware, fiscal serial, and memory usage from a printer
    /// identified by IP and port, without requiring a printer record in the database.
    /// Used by the setup wizard after a successful TCP connection test.
    /// </summary>
    /// <param name="ipAddress">IP address of the printer.</param>
    /// <param name="port">TCP port (default 9100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("printer-info")]
    [ProducesResponseType(typeof(FiscalPrinterInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrinterInfoDto>> GetPrinterInfoAsync(
        [FromQuery] string ipAddress,
        [FromQuery] int port = 9100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return CreateValidationProblemDetails();

        try
        {
            var info = await fiscalPrinterService.GetPrinterInfoByAddressAsync(ipAddress, port, cancellationToken);
            return Ok(info);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error reading printer info from {ipAddress}:{port}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Ad-hoc connection tests (wizard Step 2A/2B)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests a TCP connection to an arbitrary IP/port (wizard Step 2A).
    /// Does not require a printer DB record.
    /// </summary>
    [HttpPost("test-tcp")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> TestTcpConnectionAsync(
        [FromQuery] string ipAddress,
        [FromQuery] int port = 9100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return CreateValidationProblemDetails();

        try
        {
            var result = await fiscalPrinterService.TestTcpConnectionAsync(ipAddress, port, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error testing TCP connection to {ipAddress}:{port}.", ex);
        }
    }

    /// <summary>
    /// Tests a serial connection to an arbitrary port/baud rate (wizard Step 2B).
    /// Does not require a printer DB record.
    /// </summary>
    [HttpPost("test-serial")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> TestSerialConnectionAsync(
        [FromQuery] string serialPortName,
        [FromQuery] int baudRate = 9600,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialPortName))
            return CreateValidationProblemDetails();

        try
        {
            var result = await fiscalPrinterService.TestSerialConnectionAsync(
                serialPortName, baudRate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error testing serial connection to {serialPortName}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Wizard – save full configuration (Step 7)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Saves the complete fiscal printer configuration produced by the setup wizard.
    /// Creates the printer record, persists fiscal code mapping overrides, and
    /// associates the printer to the specified POS stations as default fiscal printer.
    /// </summary>
    /// <param name="setup">Complete wizard configuration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("setup")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrinterDto>> SaveSetupAsync(
        [FromBody] FiscalPrinterSetupDto setup,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            logger.LogInformation(
                "SaveSetupAsync | Name={Name} ConnectionType={Type} User={User}",
                setup.Name, setup.ConnectionType, GetCurrentUser());

            // Build the CreatePrinterDto from wizard data
            var createDto = new CreatePrinterDto
            {
                Name = setup.Name,
                Type = "Fiscal",
                Location = setup.Location,
                IsFiscalPrinter = true,
                ProtocolType = setup.ProtocolType,
                Status = EventForge.DTOs.Common.PrinterConfigurationStatus.Active
            };

            if (setup.ConnectionType == "TCP")
            {
                createDto.Address = setup.IpAddress;
                createDto.Port = setup.TcpPort;
            }
            else
            {
                createDto.SerialPortName = setup.SerialPortName;
                createDto.BaudRate = setup.BaudRate;
            }

            var printer = await stationService.CreatePrinterAsync(
                createDto, GetCurrentUser(), cancellationToken);

            // Note: VAT/Payment fiscal code overrides and POS associations
            // are persisted via the existing FiscalMappingService and StationsController.
            // The wizard client handles these in separate calls after receiving the printer ID.
            // This keeps the setup endpoint focused and avoids cross-service transactions.

            logger.LogInformation(
                "Fiscal printer {Name} created via wizard | PrinterId={Id}",
                printer.Name, printer.Id);

            return CreatedAtAction(
                actionName: null,
                routeValues: new { },
                value: printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                "Unexpected error saving fiscal printer setup.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure – pre-check (5B.4)
    // -------------------------------------------------------------------------

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
    //  Closure history (5B.4)
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    //  PDF download (5B.4) – stub (PDF generation is Sprint 5C scope)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Downloads the PDF Z-report for the specified closure.
    /// Note: PDF generation is scheduled for Sprint 5C. This endpoint currently
    /// returns 404 if no PDF has been stored for the closure.
    /// </summary>
    [HttpGet("closures/{closureId:guid}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult DownloadClosurePdf(Guid closureId)
    {
        // PDF storage/generation is deferred to Sprint 5C.
        // Returning 404 with a descriptive message so the client can handle it gracefully.
        return CreateNotFoundProblem($"PDF for closure {closureId} is not yet available. PDF generation is planned for Sprint 5C.");
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
}
