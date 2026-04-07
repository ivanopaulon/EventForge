using Prym.DTOs.Products.SupplierSuggestion;
using Prym.Server.Filters;
using Prym.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

/// <summary>
/// API controller for intelligent supplier recommendations.
/// </summary>
[Route("api/v1/supplier-suggestions")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class SupplierSuggestionController(
    ISupplierSuggestionService suggestionService,
    ITenantContext tenantContext,
    ILogger<SupplierSuggestionController> logger) : BaseApiController
{

    /// <summary>
    /// Gets ranked supplier suggestions for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier suggestions with detailed scoring.</returns>
    /// <response code="200">Returns supplier suggestions successfully</response>
    /// <response code="400">If the product ID is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{productId:guid}")]
    [ProducesResponseType(typeof(SupplierSuggestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierSuggestionResponse>> GetSupplierSuggestions(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return CreateValidationProblemDetails("Product ID cannot be empty.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var suggestions = await suggestionService.GetSupplierSuggestionsAsync(productId, cancellationToken);
            return Ok(suggestions);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            logger.LogWarning(ex, "Product {ProductId} not found for supplier suggestions", productId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting supplier suggestions for product {ProductId}", productId);
            return CreateInternalServerErrorProblem("An error occurred while getting supplier suggestions.", ex);
        }
    }

    /// <summary>
    /// Applies a suggested supplier as preferred for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="request">Apply suggestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success/error message.</returns>
    /// <response code="200">Supplier applied successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product or supplier is not found</response>
    [HttpPost("products/{productId:guid}/apply")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApplySuggestedSupplier(
        Guid productId,
        [FromBody] ApplySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return CreateValidationProblemDetails("Product ID cannot be empty.");
        }

        if (request.SupplierId == Guid.Empty)
        {
            return CreateValidationProblemDetails("Supplier ID cannot be empty.");
        }

        // Ensure route and body product IDs match
        if (request.ProductId != Guid.Empty && request.ProductId != productId)
        {
            return CreateValidationProblemDetails("Product ID in route must match product ID in request body.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var success = await suggestionService.ApplySuggestedSupplierAsync(
                productId, request.SupplierId, request.Reason, cancellationToken);

            if (!success)
            {
                return CreateNotFoundProblem("Product or supplier not found.");
            }

            return Ok(new
            {
                message = "Supplier successfully applied as preferred.",
                productId,
                supplierId = request.SupplierId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying suggested supplier {SupplierId} for product {ProductId}",
                request.SupplierId, productId);
            return CreateInternalServerErrorProblem("An error occurred while applying the suggested supplier.", ex);
        }
    }

    /// <summary>
    /// Gets detailed reliability metrics for a supplier.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier reliability response.</returns>
    /// <response code="200">Returns reliability metrics successfully</response>
    /// <response code="400">If the supplier ID is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpGet("suppliers/{supplierId:guid}/reliability")]
    [ProducesResponseType(typeof(SupplierReliabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierReliabilityResponse>> GetSupplierReliability(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        if (supplierId == Guid.Empty)
        {
            return CreateValidationProblemDetails("Supplier ID cannot be empty.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var reliability = await suggestionService.GetSupplierReliabilityAsync(supplierId, cancellationToken);
            return Ok(reliability);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            logger.LogWarning(ex, "Supplier {SupplierId} not found for reliability metrics", supplierId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting reliability for supplier {SupplierId}", supplierId);
            return CreateInternalServerErrorProblem("An error occurred while getting supplier reliability.", ex);
        }
    }
}
