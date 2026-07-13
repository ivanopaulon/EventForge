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
    /// Gets all units for a specific product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product units</returns>
    /// <response code="200">Returns the list of product units</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{productId:guid}/units")]
    [ProducesResponseType(typeof(IEnumerable<ProductUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductUnitDto>>> GetProductUnits(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var units = await productService.GetProductUnitsAsync(productId, cancellationToken);
            return Ok(units);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving product units.", ex);
        }
    }

    /// <summary>
    /// Adds a new unit to a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="createProductUnitDto">Product unit creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product unit</returns>
    /// <response code="201">Returns the created product unit</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/{productId:guid}/units")]
    [ProducesResponseType(typeof(ProductUnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductUnitDto>> AddProductUnit(
        Guid productId,
        [FromBody] CreateProductUnitDto createProductUnitDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Ensure the productId in the DTO matches the route parameter
        createProductUnitDto.ProductId = productId;

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productUnit = await productService.AddProductUnitAsync(createProductUnitDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductUnits),
                new { productId = productId },
                productUnit);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the product unit.", ex);
        }
    }

    /// <summary>
    /// Updates an existing product unit.
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="updateProductUnitDto">Product unit update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product unit</returns>
    /// <response code="200">Returns the updated product unit</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the product unit is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("products/units/{id:guid}")]
    [ProducesResponseType(typeof(ProductUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductUnitDto>> UpdateProductUnit(
        Guid id,
        [FromBody] UpdateProductUnitDto updateProductUnitDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productUnit = await productService.UpdateProductUnitAsync(id, updateProductUnitDto, currentUser, cancellationToken);

            if (productUnit is null)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} was not found.");
            }

            return Ok(productUnit);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the product unit.", ex);
        }
    }

    /// <summary>
    /// Deletes a product unit.
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Product unit deleted successfully</response>
    /// <response code="404">If the product unit is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("products/units/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProductUnit(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await productService.RemoveProductUnitAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} was not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the product unit.", ex);
        }
    }

}
