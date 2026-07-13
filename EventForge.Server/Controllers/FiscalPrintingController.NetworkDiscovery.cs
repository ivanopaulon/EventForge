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
    /// Scans the specified subnet prefix for devices responding on the given TCP port.
    /// Probes addresses from <c>{subnetPrefix}.1</c> to <c>{subnetPrefix}.254</c> concurrently.
    /// Results include the IP, round-trip time, and whether the device answered a Custom ENQ frame.
    /// </summary>
    /// <param name="subnetPrefix">Subnet to scan (e.g., <c>192.168.1</c>).</param>
    /// <param name="port">TCP port to probe (default 9100).</param>
    /// <param name="timeoutMs">Per-host timeout in milliseconds (default 300).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("scan-network")]
    [Authorize(Roles = "SuperAdmin,Admin")]
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

}
