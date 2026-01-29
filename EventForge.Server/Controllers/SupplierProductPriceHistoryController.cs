using EventForge.DTOs.Common;
using EventForge.DTOs.PriceHistory;
using EventForge.Server.Filters;
using EventForge.Server.Services.PriceHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for supplier product price history management.
/// Provides endpoints for querying price change history, statistics, and trend data.
/// </summary>
[Route("api/v1/price-history")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class SupplierProductPriceHistoryController : BaseApiController
{
    private readonly ISupplierProductPriceHistoryService _priceHistoryService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SupplierProductPriceHistoryController> _logger;

    public SupplierProductPriceHistoryController(
        ISupplierProductPriceHistoryService priceHistoryService,
        ITenantContext tenantContext,
        ILogger<SupplierProductPriceHistoryController> logger)
    {
        _priceHistoryService = priceHistoryService ?? throw new ArgumentNullException(nameof(priceHistoryService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets price history for a specific supplier product.
    /// </summary>
    /// <param name="supplierId">Supplier identifier</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="request">Query parameters with filters and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated price history response</returns>
    /// <response code="200">Returns the price history for the specified supplier product</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier or product is not found</response>
    [HttpGet("suppliers/{supplierId}/products/{productId}")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceHistoryResponse>> GetProductPriceHistory(
        Guid supplierId,
        Guid productId,
        [FromQuery] PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(request.Page, request.PageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _priceHistoryService.GetProductPriceHistoryAsync(
                supplierId,
                productId,
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Price history not found for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            return CreateNotFoundProblem($"Price history not found for the specified supplier and product.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving price history.", ex);
        }
    }

    /// <summary>
    /// Gets aggregated price history for all products from a supplier.
    /// </summary>
    /// <param name="supplierId">Supplier identifier</param>
    /// <param name="request">Query parameters with filters and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated price history response</returns>
    /// <response code="200">Returns the price history for all products from the supplier</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpGet("suppliers/{supplierId}")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceHistoryResponse>> GetSupplierPriceHistory(
        Guid supplierId,
        [FromQuery] PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(request.Page, request.PageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _priceHistoryService.GetSupplierPriceHistoryAsync(
                supplierId,
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Price history not found for Supplier {SupplierId}", supplierId);
            return CreateNotFoundProblem($"Price history not found for the specified supplier.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Supplier {SupplierId}", supplierId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving price history.", ex);
        }
    }

    /// <summary>
    /// Gets price history for a product across all suppliers.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="request">Query parameters with filters and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated price history response</returns>
    /// <response code="200">Returns the price history for the product across all suppliers</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{productId}/all-suppliers")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceHistoryResponse>> GetProductAllSuppliersPriceHistory(
        Guid productId,
        [FromQuery] PriceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(request.Page, request.PageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _priceHistoryService.GetProductAllSuppliersPriceHistoryAsync(
                productId,
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Price history not found for Product {ProductId}", productId);
            return CreateNotFoundProblem($"Price history not found for the specified product.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for Product {ProductId}", productId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving price history.", ex);
        }
    }

    /// <summary>
    /// Gets price history statistics for a supplier, optionally filtered by product.
    /// </summary>
    /// <param name="supplierId">Supplier identifier</param>
    /// <param name="productId">Optional product identifier to filter statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price history statistics</returns>
    /// <response code="200">Returns the price history statistics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpGet("suppliers/{supplierId}/statistics")]
    [ProducesResponseType(typeof(PriceHistoryStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceHistoryStatistics>> GetPriceHistoryStatistics(
        Guid supplierId,
        [FromQuery] Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _priceHistoryService.GetPriceHistoryStatisticsAsync(
                supplierId,
                productId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Statistics not found for Supplier {SupplierId}", supplierId);
            return CreateNotFoundProblem($"Statistics not found for the specified supplier.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for Supplier {SupplierId}", supplierId);
            return CreateInternalServerErrorProblem("An error occurred while calculating statistics.", ex);
        }
    }

    /// <summary>
    /// Gets price trend data points for charting.
    /// </summary>
    /// <param name="supplierId">Supplier identifier</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of price trend data points</returns>
    /// <response code="200">Returns the price trend data</response>
    /// <response code="400">If the date range is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier or product is not found</response>
    [HttpGet("suppliers/{supplierId}/products/{productId}/trend")]
    [ProducesResponseType(typeof(List<PriceTrendDataPoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PriceTrendDataPoint>>> GetPriceTrendData(
        Guid supplierId,
        Guid productId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return CreateValidationProblemDetails("Start date must be before end date.");
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _priceHistoryService.GetPriceTrendDataAsync(
                supplierId,
                productId,
                fromDate,
                toDate,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Trend data not found for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            return CreateNotFoundProblem($"Trend data not found for the specified supplier and product.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trend data for Supplier {SupplierId} and Product {ProductId}", supplierId, productId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving trend data.", ex);
        }
    }
}
