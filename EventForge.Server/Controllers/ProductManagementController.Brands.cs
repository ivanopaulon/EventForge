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
    /// Gets all brands with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of brands</returns>
    /// <response code="200">Returns the paginated list of brands</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(PagedResult<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BrandDto>>> GetBrands(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await brandService.GetBrandsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving brands.", ex);
        }
    }

    /// <summary>
    /// Gets a brand by ID.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Brand information</returns>
    /// <response code="200">Returns the brand</response>
    /// <response code="404">If the brand is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("brands/{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandDto>> GetBrand(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var brand = await brandService.GetBrandByIdAsync(id, cancellationToken);
            if (brand is null)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return Ok(brand);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the brand.", ex);
        }
    }

    /// <summary>
    /// Creates a new brand.
    /// </summary>
    /// <param name="createBrandDto">Brand creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created brand information</returns>
    /// <response code="201">Brand created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("brands")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandDto>> CreateBrand(
        [FromBody] CreateBrandDto createBrandDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var brand = await brandService.CreateBrandAsync(createBrandDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the brand.", ex);
        }
    }

    /// <summary>
    /// Updates an existing brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="updateBrandDto">Brand update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated brand information</returns>
    /// <response code="200">Brand updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the brand is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("brands/{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandDto>> UpdateBrand(
        Guid id,
        [FromBody] UpdateBrandDto updateBrandDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var brand = await brandService.UpdateBrandAsync(id, updateBrandDto, currentUser, cancellationToken);
            if (brand is null)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return Ok(brand);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the brand.", ex);
        }
    }

    /// <summary>
    /// Deletes a brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Brand deleted successfully</response>
    /// <response code="404">If the brand is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("brands/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBrand(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await brandService.DeleteBrandAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the brand.", ex);
        }
    }

}
