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
    /// Gets all codes for a specific product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product codes</returns>
    /// <response code="200">Returns the list of product codes</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{productId:guid}/codes")]
    [ProducesResponseType(typeof(IEnumerable<ProductCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductCodeDto>>> GetProductCodes(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var codes = await productService.GetProductCodesAsync(productId, cancellationToken);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving product codes.", ex);
        }
    }

    /// <summary>
    /// Adds a new code to a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="createProductCodeDto">Product code creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product code</returns>
    /// <response code="201">Returns the created product code</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/{productId:guid}/codes")]
    [ProducesResponseType(typeof(ProductCodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductCodeDto>> AddProductCode(
        Guid productId,
        [FromBody] CreateProductCodeDto createProductCodeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Ensure the productId in the DTO matches the route parameter
        createProductCodeDto.ProductId = productId;

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productCode = await productService.AddProductCodeAsync(createProductCodeDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductCodes),
                new { productId = productId },
                productCode);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the product code.", ex);
        }
    }

}
