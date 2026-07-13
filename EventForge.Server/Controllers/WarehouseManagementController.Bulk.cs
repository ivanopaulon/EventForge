using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class WarehouseManagementController
{

    /// <summary>
    /// Performs a bulk warehouse transfer operation.
    /// </summary>
    /// <param name="bulkTransferDto">Bulk transfer request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk transfer operation</returns>
    /// <response code="200">Returns the result of the bulk transfer operation</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("bulk-transfer")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Prym.DTOs.Bulk.BulkTransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Prym.DTOs.Bulk.BulkTransferResultDto>> BulkTransfer(
        [FromBody] Prym.DTOs.Bulk.BulkTransferDto bulkTransferDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "System";
            var result = await warehouseFacade.BulkTransferAsync(bulkTransferDto, currentUser, cancellationToken);

            logger.LogInformation(
                "Bulk transfer: {SuccessCount} successful, {FailedCount} failed",
                result.SuccessCount, result.FailedCount);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bulk transfer request");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during bulk transfer.", ex);
        }
    }

}
