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
    /// Gets all bundle items for a specific product.
    /// </summary>
    /// <param name="productId">Bundle product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bundle items</returns>
    /// <response code="200">Returns the list of bundle items</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{productId:guid}/bundle-items")]
    [ProducesResponseType(typeof(IEnumerable<ProductBundleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductBundleItemDto>>> GetProductBundleItems(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var items = await productService.GetProductBundleItemsAsync(productId, cancellationToken);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving bundle items.", ex);
        }
    }

    /// <summary>
    /// Gets a specific bundle item by ID.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bundle item DTO or 404 if not found</returns>
    /// <response code="200">Returns the bundle item</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpGet("product-bundle-items/{id:guid}")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductBundleItemDto>> GetProductBundleItemById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var item = await productService.GetProductBundleItemByIdAsync(id, cancellationToken);
            if (item is null) return NotFound();
            return Ok(item);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the bundle item.", ex);
        }
    }

    /// <summary>
    /// Adds a new component to a product bundle.
    /// </summary>
    /// <param name="createProductBundleItemDto">Bundle item creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bundle item</returns>
    /// <response code="201">Returns the created bundle item</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("product-bundle-items")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductBundleItemDto>> AddProductBundleItem(
        [FromBody] CreateProductBundleItemDto createProductBundleItemDto,
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
            var item = await productService.AddProductBundleItemAsync(createProductBundleItemDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductBundleItemById),
                new { id = item.Id },
                item);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bundle item creation request");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation during bundle item creation");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the bundle item.", ex);
        }
    }

    /// <summary>
    /// Updates an existing bundle item.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="updateProductBundleItemDto">Bundle item update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bundle item or 404 if not found</returns>
    /// <response code="200">Returns the updated bundle item</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpPut("product-bundle-items/{id:guid}")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductBundleItemDto>> UpdateProductBundleItem(
        Guid id,
        [FromBody] UpdateProductBundleItemDto updateProductBundleItemDto,
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
            var item = await productService.UpdateProductBundleItemAsync(id, updateProductBundleItemDto, currentUser, cancellationToken);
            if (item is null) return NotFound();
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bundle item update request for ID {BundleItemId}", id);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the bundle item.", ex);
        }
    }

    /// <summary>
    /// Removes a bundle item (soft delete).
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>204 No Content on success, 404 if not found</returns>
    /// <response code="204">Bundle item successfully removed</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpDelete("product-bundle-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveProductBundleItem(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var removed = await productService.RemoveProductBundleItemAsync(id, currentUser, cancellationToken);
            if (!removed) return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing the bundle item.", ex);
        }
    }

}
