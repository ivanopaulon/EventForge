using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;


namespace EventForge.Server.Controllers;

public partial class ChatController
{

    /// <summary>
    /// Gets chat system health status and metrics.
    /// Provides real-time system monitoring and alerting information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat system health status and metrics</returns>
    /// <response code="200">System health retrieved successfully</response>
    [HttpGet("system/health")]
    [ProducesResponseType(typeof(ChatSystemHealthDto), StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,SuperAdmin")] // Restrict to administrators
    public async Task<ActionResult<ChatSystemHealthDto>> GetChatSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.GetChatSystemHealthAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving system health", ex);
        }
    }

}
