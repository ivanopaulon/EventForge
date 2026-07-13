using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class ProductManagementController
{

    /// <summary>
    /// Performs a bulk price update on multiple products.
    /// </summary>
    /// <param name="bulkUpdateDto">Bulk update request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk update operation</returns>
    /// <response code="200">Returns the result of the bulk update operation</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Prym.DTOs.Bulk.BulkUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Prym.DTOs.Bulk.BulkUpdateResultDto>> BulkUpdatePrices(
        [FromBody] Prym.DTOs.Bulk.BulkUpdatePricesDto bulkUpdateDto,
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
            var result = await productService.BulkUpdatePricesAsync(bulkUpdateDto, currentUser, cancellationToken);

            logger.LogInformation(
                "Bulk price update: {SuccessCount} successful, {FailedCount} failed",
                result.SuccessCount, result.FailedCount);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bulk update request");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during bulk price update.", ex);
        }
    }

}
