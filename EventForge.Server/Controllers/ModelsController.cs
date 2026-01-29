using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing product models.
/// </summary>
[Route("api/[controller]")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("ProductManagement")]
public class ModelsController : BaseApiController
{
    private readonly IModelService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ModelsController> _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public ModelsController(
        IModelService service,
        ITenantContext tenantContext,
        ILogger<ModelsController> logger,
        ICacheInvalidationService cacheInvalidation)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    }

    /// <summary>
    /// Retrieves all models with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models</returns>
    /// <response code="200">Successfully retrieved models with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<ModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ModelDto>>> GetModels(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetModelsAsync(pagination, cancellationToken);

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
            _logger.LogError(ex, "An error occurred while retrieving models.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving models.", ex);
        }
    }

    /// <summary>
    /// Retrieves models for a specific brand with pagination
    /// </summary>
    /// <param name="brandId">Brand ID to filter models</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models for the brand</returns>
    /// <response code="200">Successfully retrieved models with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("brand/{brandId}")]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<ModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ModelDto>>> GetModelsByBrand(
        Guid brandId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetModelsByBrandIdAsync(brandId, pagination, cancellationToken);

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
            _logger.LogError(ex, "An error occurred while retrieving models for brand {BrandId}.", brandId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving models for the brand.", ex);
        }
    }
}
