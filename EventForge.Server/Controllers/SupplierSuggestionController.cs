using EventForge.DTOs.Products.SupplierSuggestion;
using EventForge.Server.Filters;
using EventForge.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// API controller for intelligent supplier recommendations.
/// </summary>
[Route("api/v1/supplier-suggestions")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class SupplierSuggestionController : BaseApiController
{
    private readonly ISupplierSuggestionService _suggestionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SupplierSuggestionController> _logger;

    public SupplierSuggestionController(
        ISupplierSuggestionService suggestionService,
        ITenantContext tenantContext,
        ILogger<SupplierSuggestionController> logger)
    {
        _suggestionService = suggestionService ?? throw new ArgumentNullException(nameof(suggestionService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid product ID",
                Detail = "Product ID cannot be empty."
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var suggestions = await _suggestionService.GetSupplierSuggestionsAsync(productId, cancellationToken);
            return Ok(suggestions);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Product {ProductId} not found for supplier suggestions", productId);
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier suggestions for product {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while getting supplier suggestions."
            });
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
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid product ID",
                Detail = "Product ID cannot be empty."
            });
        }

        if (request.SupplierId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid supplier ID",
                Detail = "Supplier ID cannot be empty."
            });
        }

        // Ensure route and body product IDs match
        if (request.ProductId != Guid.Empty && request.ProductId != productId)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Product ID mismatch",
                Detail = "Product ID in route must match product ID in request body."
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _suggestionService.ApplySuggestedSupplierAsync(
                productId, request.SupplierId, request.Reason, cancellationToken);

            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not found",
                    Detail = "Product or supplier not found."
                });
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
            _logger.LogError(ex, "Error applying suggested supplier {SupplierId} for product {ProductId}",
                request.SupplierId, productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while applying the suggested supplier."
            });
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
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid supplier ID",
                Detail = "Supplier ID cannot be empty."
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var reliability = await _suggestionService.GetSupplierReliabilityAsync(supplierId, cancellationToken);
            return Ok(reliability);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Supplier {SupplierId} not found for reliability metrics", supplierId);
            return NotFound(new ProblemDetails
            {
                Title = "Supplier not found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reliability for supplier {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while getting supplier reliability."
            });
        }
    }
}
