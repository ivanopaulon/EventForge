using Prym.DTOs.Products;
using Prym.Server.Filters;
using Prym.Server.ModelBinders;
using Prym.Server.Services.Caching;
using Prym.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Prym.Server.Controllers;

/// <summary>
/// Controller for managing product models.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("ProductManagement")]
public class ModelsController(
    IModelService service,
    ITenantContext tenantContext) : BaseApiController
{

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
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetModelsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
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
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetModelsByBrandIdAsync(brandId, pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving models for the brand.", ex);
        }
    }
}
