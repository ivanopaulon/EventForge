using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing lots and traceability operations.
/// </summary>
[Route("api/[controller]")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("InventoryManagement")]
public class LotsController : BaseApiController
{
    private readonly ILotService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LotsController> _logger;

    public LotsController(
        ILotService service,
        ITenantContext tenantContext,
        ILogger<LotsController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all lots with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of lots</returns>
    /// <response code="200">Successfully retrieved lots with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetLots(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetLotsAsync(pagination, cancellationToken: cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving lots.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving lots.", ex);
        }
    }

    /// <summary>
    /// Retrieves lots for a specific product with pagination
    /// </summary>
    /// <param name="productId">Product ID to filter lots</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of lots for the product</returns>
    /// <response code="200">Successfully retrieved lots with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetLotsByProduct(
        Guid productId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetLotsByProductAsync(productId, pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving lots for product {ProductId}.", productId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving lots for the product.", ex);
        }
    }

    /// <summary>
    /// Retrieves lots for a specific warehouse with pagination
    /// </summary>
    /// <param name="warehouseId">Warehouse ID to filter lots</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of lots in the warehouse</returns>
    /// <response code="200">Successfully retrieved lots with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("warehouse/{warehouseId}")]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetLotsByWarehouse(
        Guid warehouseId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetLotsByWarehouseAsync(warehouseId, pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving lots for warehouse {WarehouseId}.", warehouseId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving lots for the warehouse.", ex);
        }
    }

    /// <summary>
    /// Retrieves expired or expiring lots with pagination
    /// </summary>
    /// <param name="threshold">Optional threshold date. If null, uses DateTime.UtcNow</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of expired/expiring lots</returns>
    /// <response code="200">Successfully retrieved expired lots with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("expired")]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetExpiredLots(
        [FromQuery] DateTime? threshold,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetExpiredLotsAsync(threshold, pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving expired lots.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving expired lots.", ex);
        }
    }
}
