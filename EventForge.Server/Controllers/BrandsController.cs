using EventForge.DTOs.Products;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing product brands.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("ProductManagement")]
public class BrandsController(
    IBrandService service,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Retrieves all brands with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of brands</returns>
    /// <response code="200">Successfully retrieved brands with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BrandDto>>> GetBrands(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await service.GetBrandsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving brands.", ex);
        }
    }

    /// <summary>
    /// Retrieves all active brands with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of active brands</returns>
    /// <response code="200">Successfully retrieved active brands with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("active")]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BrandDto>>> GetActiveBrands(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await service.GetActiveBrandsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active brands.", ex);
        }
    }
}
