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
    /// Tests a TCP connection to a network printer on an agent's local network (wizard Step 2A – TcpViaAgent).
    /// The request is forwarded to the agent which opens the TCP socket on its side.
    /// </summary>
    /// <param name="agentId">GUID of the UpdateAgent that will perform the TCP test.</param>
    /// <param name="ipAddress">Printer IP address on the agent's local network.</param>
    /// <param name="port">Printer TCP port (default 9100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("test-tcp-via-agent")]
    [ProducesResponseType(typeof(FiscalPrintResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrintResult>> TestTcpViaAgentAsync(
        [FromQuery] Guid agentId,
        [FromQuery] string ipAddress,
        [FromQuery] int port = 9100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return CreateValidationProblemDetails();

        var agentUrl = configuration[$"AgentProxies:{agentId}"];
        if (string.IsNullOrWhiteSpace(agentUrl))
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found.",
                Detail = $"No base URL configured for agent ID '{agentId}'. Add 'AgentProxies:{agentId}' to application configuration."
            });

        try
        {
            logger.LogDebug(
                "TestTcpViaAgentAsync | Agent={AgentId} Printer={Ip}:{Port}",
                agentId, ipAddress, port);

            var url = $"{agentUrl.TrimEnd('/')}/api/printer-proxy/tcp-test" +
                      $"?host={Uri.EscapeDataString(ipAddress)}&port={port}";

            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            using var response = await client.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow });
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Ok(new FiscalPrintResult
                {
                    Success = false,
                    ErrorMessage = $"Agent {agentId} cannot reach printer {ipAddress}:{port}: {body}"
                });
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout testing TCP via agent {AgentId} to {Ip}:{Port}", agentId, ipAddress, port);
            return Ok(new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = $"Connection test to {ipAddress}:{port} via agent timed out."
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error testing TCP connection to {ipAddress}:{port} via agent {agentId}.", ex);
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
    //  Wizard – save full configuration (Step 8)
    // -------------------------------------------------------------------------

}
