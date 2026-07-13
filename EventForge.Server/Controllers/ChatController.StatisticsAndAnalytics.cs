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
    /// Gets comprehensive chat statistics and analytics.
    /// Supports real-time metrics, historical analysis, and tenant-specific insights.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter for statistics</param>
    /// <param name="fromDate">Optional start date for statistics range</param>
    /// <param name="toDate">Optional end date for statistics range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive chat analytics and metrics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ChatStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatStatsDto>> GetChatStatisticsAsync(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = fromDate.HasValue && toDate.HasValue
                ? new DateRange { StartDate = fromDate.Value, EndDate = toDate.Value }
                : null;

            var result = await chatService.GetChatStatisticsAsync(tenantId, dateRange, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving statistics", ex);
        }
    }

}
