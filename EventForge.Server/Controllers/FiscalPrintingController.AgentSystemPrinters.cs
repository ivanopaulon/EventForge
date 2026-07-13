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
    /// Returns all printers installed at OS level on the machine running the specified agent.
    /// The server proxies the request to the agent's <c>/api/printer-proxy/system-printers</c> endpoint.
    /// </summary>
    /// <param name="agentId">GUID of the UpdateAgent (must be configured in <c>AgentProxies:{id}</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a <c>printers</c> string array; 404 if agent not configured; 502 on proxy failure.</returns>
    [HttpGet("agent-system-printers")]
    [ProducesResponseType(typeof(AgentSystemPrintersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetAgentSystemPrintersAsync(
        [FromQuery] Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var agentUrl = configuration[$"AgentProxies:{agentId}"];

        if (string.IsNullOrWhiteSpace(agentUrl))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not configured",
                Detail = $"No base URL found for agent ID '{agentId}'. " +
                         "Add 'AgentProxies:{agentId}' to application configuration."
            });
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{agentUrl.TrimEnd('/')}/api/printer-proxy/system-printers";
            logger.LogDebug("Proxying system-printers request to agent {AgentId} at {Url}", agentId, url);

            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Agent {AgentId} returned HTTP {StatusCode} for system-printers",
                    agentId, (int)response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Agent proxy error",
                    Detail = $"Agent returned HTTP {(int)response.StatusCode}."
                });
            }

            var json = await response.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Ok(json);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to reach agent {AgentId} for system-printers", agentId);
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Agent unreachable",
                Detail = ex.Message
            });
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout reaching agent {AgentId} for system-printers", agentId);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
            {
                Title = "Agent timeout",
                Detail = "The request to the agent timed out."
            });
        }
    }
}
