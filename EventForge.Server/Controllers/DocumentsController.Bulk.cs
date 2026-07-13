using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Performs a bulk status change operation on multiple documents.
    /// </summary>
    /// <param name="bulkStatusChangeDto">Bulk status change request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk status change operation</returns>
    /// <response code="200">Returns the result of the bulk status change operation</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("bulk-status-change")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Prym.DTOs.Bulk.BulkStatusChangeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Prym.DTOs.Bulk.BulkStatusChangeResultDto>> BulkStatusChange(
        [FromBody] Prym.DTOs.Bulk.BulkStatusChangeDto bulkStatusChangeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "System";
            var result = await documentFacade.BulkStatusChangeAsync(bulkStatusChangeDto, currentUser, cancellationToken);

            logger.LogInformation(
                "Bulk status change: {SuccessCount} successful, {FailedCount} failed",
                result.SuccessCount, result.FailedCount);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bulk status change request");
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during bulk status change.", ex);
        }
    }

}
