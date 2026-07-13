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
    /// Gets all suppliers for a specific product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product suppliers</returns>
    /// <response code="200">Returns the list of suppliers</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{productId:guid}/suppliers")]
    [ProducesResponseType(typeof(IEnumerable<ProductSupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductSupplierDto>>> GetProductSuppliers(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var suppliers = await productService.GetProductSuppliersAsync(productId, cancellationToken);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving product suppliers.", ex);
        }
    }

    /// <summary>
    /// Gets a specific product supplier by ID.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product supplier details</returns>
    /// <response code="200">Returns the product supplier</response>
    /// <response code="404">If the product supplier is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("product-suppliers/{id:guid}")]
    [ProducesResponseType(typeof(ProductSupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSupplierDto>> GetProductSupplier(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var supplier = await productService.GetProductSupplierByIdAsync(id, cancellationToken);
            if (supplier is null)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product supplier.", ex);
        }
    }

    /// <summary>
    /// Adds a new supplier to a product.
    /// </summary>
    /// <param name="createProductSupplierDto">Product supplier creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product supplier</returns>
    /// <response code="201">Returns the newly created product supplier</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("product-suppliers")]
    [ProducesResponseType(typeof(ProductSupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSupplierDto>> AddProductSupplier(
        [FromBody] CreateProductSupplierDto createProductSupplierDto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var currentUser = GetCurrentUser();
            var productSupplier = await productService.AddProductSupplierAsync(createProductSupplierDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetProductSupplier), new { id = productSupplier.Id }, productSupplier);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while adding product supplier.");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the product supplier.", ex);
        }
    }

    /// <summary>
    /// Updates an existing product supplier.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="updateProductSupplierDto">Product supplier update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product supplier</returns>
    /// <response code="200">Returns the updated product supplier</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the product supplier is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("product-suppliers/{id:guid}")]
    [ProducesResponseType(typeof(ProductSupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSupplierDto>> UpdateProductSupplier(
        Guid id,
        [FromBody] UpdateProductSupplierDto updateProductSupplierDto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var currentUser = GetCurrentUser();
            var productSupplier = await productService.UpdateProductSupplierAsync(id, updateProductSupplierDto, currentUser, cancellationToken);
            if (productSupplier is null)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return Ok(productSupplier);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating product supplier.");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the product supplier.", ex);
        }
    }

    /// <summary>
    /// Deletes a product supplier (soft delete).
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the product supplier was successfully deleted</response>
    /// <response code="404">If the product supplier is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("product-suppliers/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProductSupplier(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await productService.RemoveProductSupplierAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the product supplier.", ex);
        }
    }

    /// <summary>
    /// Gets all products with their association status for a specific supplier.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with association status</returns>
    /// <response code="200">Returns the list of products with association status</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If an error occurs while retrieving the products</response>
    [HttpGet("suppliers/{supplierId:guid}/products")]
    [ProducesResponseType(typeof(IEnumerable<ProductWithAssociationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductWithAssociationDto>>> GetProductsWithSupplierAssociation(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var products = await productService.GetProductsWithSupplierAssociationAsync(supplierId, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the products.", ex);
        }
    }

    /// <summary>
    /// Bulk updates product-supplier associations.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="productIds">List of product IDs to associate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of associations created</returns>
    /// <response code="200">Returns the number of associations created</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="500">If an error occurs while updating associations</response>
    [HttpPost("suppliers/{supplierId:guid}/products/bulk-update")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> BulkUpdateProductSupplierAssociations(
        Guid supplierId,
        [FromBody] IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var count = await productService.BulkUpdateProductSupplierAssociationsAsync(supplierId, productIds, currentUser, cancellationToken);
            return Ok(count);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the associations.", ex);
        }
    }

    /// <summary>
    /// Bulk updates catalog fields (UoM, VAT, brand, model, classification) for a set of products.
    /// Only non-null fields in the request body are applied. Products can be identified by explicit IDs
    /// or by filter criteria (brand, classification, VAT, UoM).
    /// </summary>
    /// <param name="dto">Bulk update specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success/failure counts</returns>
    /// <response code="200">Returns the bulk update result</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/bulk-catalog-update")]
    [ProducesResponseType(typeof(BulkUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkUpdateResult>> BulkUpdateProductCatalog(
        [FromBody] BulkUpdateProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (dto == null)
            return BadRequest("Request body is required.");

        if (!dto.VatRateId.HasValue && !dto.UnitOfMeasureId.HasValue && !dto.BrandId.HasValue &&
            !dto.ModelId.HasValue && !dto.CategoryNodeId.HasValue && !dto.FamilyNodeId.HasValue &&
            !dto.GroupNodeId.HasValue && !dto.Status.HasValue && !dto.IsVatIncluded.HasValue &&
            !dto.ReorderPoint.HasValue && !dto.SafetyStock.HasValue && !dto.TargetStockLevel.HasValue &&
            !dto.AverageDailyDemand.HasValue && !dto.PreferredSupplierId.HasValue && !dto.StationId.HasValue)
        {
            return BadRequest("At least one update field must be specified.");
        }

        try
        {
            var currentUser = GetCurrentUser();
            var result = await productService.BulkUpdateProductsAsync(dto, currentUser, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during the bulk catalog update.", ex);
        }
    }

    /// <summary>
    /// Returns the count of products that would be selected by the given filters,
    /// without performing any update. Use before executing a bulk update to preview the scope.
    /// </summary>
    /// <response code="200">Returns the matching product count.</response>
    /// <response code="400">If no selection criteria are specified.</response>
    /// <response code="403">If the user doesn't have access to the current tenant.</response>
    [HttpPost("products/bulk-catalog-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> GetBulkCatalogCount(
        [FromBody] BulkUpdateProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (dto == null)
            return BadRequest("Request body is required.");

        try
        {
            var count = await productService.CountProductsMatchingFiltersAsync(dto, cancellationToken);
            return Ok(count);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while counting matching products.", ex);
        }
    }

    /// <summary>
    /// Gets products by supplier with pagination, enriched with latest purchase data.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product suppliers</returns>
    /// <response code="200">Returns the paginated list of products</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("suppliers/{supplierId:guid}/supplied-products")]
    [ProducesResponseType(typeof(PagedResult<ProductSupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProductSupplierDto>>> GetProductsBySupplier(
        Guid supplierId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await productService.GetProductsBySupplierAsync(supplierId, pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving products.", ex);
        }
    }

}
