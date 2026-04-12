using EventForge.DTOs.PriceLists;
using EventForge.DTOs.Products;
using EventForge.DTOs.Promotions;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.Interfaces;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Consolidated REST API controller for product management with multi-tenant support.
/// Provides unified CRUD operations for products, units of measure, price lists, promotions, and barcodes
/// within the authenticated user's tenant context.
/// This controller consolidates ProductsController, UnitOfMeasuresController, PriceListsController, 
/// PromotionsController, and BarcodeController to reduce endpoint fragmentation and improve maintainability.
/// </summary>
[Route("api/v1/product-management")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class ProductManagementController(
    IProductService productService,
    IBrandService brandService,
    IModelService modelService,
    IUMService umService,
    IPriceListService priceListService,
    IPriceListGenerationService priceListGenerationService,
    IPriceCalculationService priceCalculationService,
    IPriceListBusinessPartyService priceListBusinessPartyService,
    IPromotionService promotionService,
    IBarcodeService barcodeService,
    IDocumentHeaderService documentHeaderService,
    IStockMovementService stockMovementService,
    ITenantContext tenantContext,
    ILogger<ProductManagementController> logger,
    IExportService exportService) : BaseApiController
{

    #region Product CRUD Operations

    /// <summary>
    /// Gets all products with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="searchTerm">Optional search term to filter products by code, name, or description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    /// <response code="200">Returns the paginated list of products</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await productService.GetProductsAsync(pagination, searchTerm, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving products.", ex);
        }
    }

    /// <summary>
    /// Gets a lean product catalog for POS display.
    /// Returns only the fields needed by the POS grid (excludes Codes, Units, BundleItems for faster loading).
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Slim paginated list of products optimized for POS</returns>
    /// <response code="200">Returns the paginated POS product catalog</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/pos-catalog")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPosCatalog(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await productService.GetProductsForPosCatalogAsync(pagination, searchTerm, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the POS catalog.", ex);
        }
    }


    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product information</returns>
    /// <response code="200">Returns the product</response>
    /// <response code="404">If the product is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return Ok(product);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product.", ex);
        }
    }

    /// <summary>
    /// Gets a product by barcode/code value with code context.
    /// </summary>
    /// <param name="code">Barcode or code value to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product information with code context if found</returns>
    /// <response code="200">Returns the product with code context</response>
    /// <response code="404">If no product with the given code is found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/by-code/{code}")]
    [ProducesResponseType(typeof(ProductWithCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductWithCodeDto>> GetProductByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var productWithCode = await productService.GetProductWithCodeByCodeAsync(code, cancellationToken);
            if (productWithCode is null)
                return CreateNotFoundProblem($"Product with code '{code}' not found.");

            return Ok(productWithCode);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product.", ex);
        }
    }

    /// <summary>
    /// Performs unified product search with exact code match priority.
    /// First searches for exact match on barcode/product code (case-insensitive).
    /// If no exact match found, performs text search on product name, description, and brand.
    /// </summary>
    /// <param name="q">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with exact match and/or text-based results</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/search")]
    [ProducesResponseType(typeof(ProductSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSearchResultDto>> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return CreateValidationProblemDetails("Query parameter 'q' is required.");
        }

        if (maxResults < 1 || maxResults > 100)
        {
            return CreateValidationProblemDetails("maxResults must be between 1 and 100.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await productService.SearchProductsAsync(q, maxResults, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while searching products.", ex);
        }
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product information</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto createProductDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await productService.CreateProductAsync(createProductDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the product.", ex);
        }
    }

    /// <summary>
    /// Creates a new product with multiple codes and units of measure in a single transaction.
    /// Used for quick product creation during inventory procedures.
    /// </summary>
    /// <param name="createDto">Product creation data with codes and units</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product with full details</returns>
    /// <response code="201">Product created successfully with codes and units</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/create-with-codes-units")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDetailDto>> CreateProductWithCodesAndUnits(
        [FromBody] CreateProductWithCodesAndUnitsDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await productService.CreateProductWithCodesAndUnitsAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the product with codes and units.", ex);
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product information</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the product is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("products/{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductDto updateProductDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await productService.UpdateProductAsync(id, updateProductDto, currentUser, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return Ok(product);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the product.", ex);
        }
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="404">If the product is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("products/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await productService.DeleteProductAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the product.", ex);
        }
    }

    /// <summary>
    /// Uploads a product image.
    /// </summary>
    /// <param name="file">Image file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image URL</returns>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">If the file is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/upload-image")]
    [ProducesResponseType(typeof(ImageUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ImageUploadResultDto>> UploadProductImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file is null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            // Generate a unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"product_{Guid.NewGuid()}{extension}";

            // For now, save to wwwroot/images/products (in a real implementation, use cloud storage)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            _ = Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var imageUrl = $"/images/products/{fileName}";

            return Ok(new ImageUploadResultDto { ImageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the product image.", ex);
        }
    }

    /// <summary>
    /// Uploads an image as a DocumentReference for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="file">Image file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product with ImageDocumentId</returns>
    /// <response code="200">Image uploaded successfully and linked to product</response>
    /// <response code="400">If the file is invalid</response>
    /// <response code="404">If product not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("products/{id}/image")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> UploadProductImageDocument(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file is null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            var updatedProduct = await productService.UploadProductImageAsync(id, file, cancellationToken);
            if (updatedProduct is null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the product image.", ex);
        }
    }

    /// <summary>
    /// Gets the image DocumentReference for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image document reference</returns>
    /// <response code="200">Returns the image document</response>
    /// <response code="404">If product not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/{id}/image")]
    [ProducesResponseType(typeof(EventForge.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetProductImageDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var imageDocument = await productService.GetProductImageDocumentAsync(id, cancellationToken);
            if (imageDocument is null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found or has no image.");
            }

            return Ok(imageDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product image.", ex);
        }
    }

    /// <summary>
    /// Deletes the image DocumentReference for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Image deleted successfully</response>
    /// <response code="404">If product not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("products/{id}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteProductImageDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var success = await productService.DeleteProductImageAsync(id, cancellationToken);
            if (!success)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found or has no image.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the product image.", ex);
        }
    }

    #endregion

    #region Product Codes Management

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

    #endregion

    #region Product Unit Management

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

    #endregion

    #region Unit of Measures Management

    /// <summary>
    /// Gets all units of measure with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of units of measure</returns>
    /// <response code="200">Returns the paginated list of units of measure</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("units")]
    [ProducesResponseType(typeof(PagedResult<UMDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UMDto>>> GetUnitOfMeasures(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await umService.GetUMsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving units of measure.", ex);
        }
    }

    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unit of measure information</returns>
    /// <response code="200">Returns the unit of measure</response>
    /// <response code="404">If the unit of measure is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("units/{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UMDto>> GetUnitOfMeasure(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var unit = await umService.GetUMByIdAsync(id, cancellationToken);
            if (unit is null)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return Ok(unit);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="createUMDto">Unit of measure creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created unit of measure information</returns>
    /// <response code="201">Unit of measure created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("units")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UMDto>> CreateUnitOfMeasure(
        [FromBody] CreateUMDto createUMDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await umService.CreateUMAsync(createUMDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetUnitOfMeasure), new { id = unit.Id }, unit);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="updateUMDto">Unit of measure update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated unit of measure information</returns>
    /// <response code="200">Unit of measure updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the unit of measure is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("units/{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UMDto>> UpdateUnitOfMeasure(
        Guid id,
        [FromBody] UpdateUMDto updateUMDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await umService.UpdateUMAsync(id, updateUMDto, currentUser, cancellationToken);
            if (unit is null)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return Ok(unit);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Deletes a unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Unit of measure deleted successfully</response>
    /// <response code="404">If the unit of measure is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("units/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUnitOfMeasure(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await umService.DeleteUMAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the unit of measure.", ex);
        }
    }

    #endregion

    #region Price Lists Management

    /// <summary>
    /// Gets all price lists with pagination and optional filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="direction">Optional filter by direction (Input/Output)</param>
    /// <param name="status">Optional filter by status (Active/Suspended/Deleted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of price lists</returns>
    /// <response code="200">Returns the paginated list of price lists</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("price-lists")]
    [ProducesResponseType(typeof(PagedResult<PriceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PriceListDto>>> GetPriceLists(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] PriceListDirection? direction = null,
        [FromQuery] DTOs.Common.PriceListStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var result = await priceListService.GetPriceListsAsync(pagination, direction, status, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price lists.", ex);
        }
    }

    /// <summary>
    /// Gets a price list by ID.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price list information</returns>
    /// <response code="200">Returns the price list</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("price-lists/{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> GetPriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var priceList = await priceListService.GetPriceListByIdAsync(id, cancellationToken);
            if (priceList is null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the price list.", ex);
        }
    }

    /// <summary>
    /// Creates a new price list.
    /// </summary>
    /// <param name="createPriceListDto">Price list creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created price list information</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("price-lists")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> CreatePriceList(
        [FromBody] CreatePriceListDto createPriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await priceListService.CreatePriceListAsync(createPriceListDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the price list.", ex);
        }
    }

    /// <summary>
    /// Updates an existing price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="updatePriceListDto">Price list update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated price list information</returns>
    /// <response code="200">Price list updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("price-lists/{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> UpdatePriceList(
        Guid id,
        [FromBody] UpdatePriceListDto updatePriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await priceListService.UpdatePriceListAsync(id, updatePriceListDto, currentUser, cancellationToken);
            if (priceList is null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the price list.", ex);
        }
    }

    /// <summary>
    /// Deletes a price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Price list deleted successfully</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("price-lists/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await priceListService.DeletePriceListAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the price list.", ex);
        }
    }

    /// <summary>
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    /// <param name="id">ID del listino da duplicare</param>
    /// <param name="dto">Opzioni di duplicazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dettagli del listino duplicato</returns>
    /// <response code="201">Listino duplicato con successo</response>
    /// <response code="400">Se i parametri di duplicazione non sono validi</response>
    /// <response code="404">Se il listino sorgente non esiste</response>
    /// <response code="403">Se l'utente non ha accesso al tenant corrente</response>
    [HttpPost("price-lists/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(DuplicatePriceListResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DuplicatePriceList(
        Guid id,
        [FromBody] DuplicatePriceListDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var result = await priceListGenerationService.DuplicatePriceListAsync(
                id, dto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = result.NewPriceList.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation during price list duplication");
            return CreateNotFoundProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error during price list duplication");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while duplicating the price list.", ex);
        }
    }

    #endregion

    #region Price List - BusinessParty Management

    /// <summary>
    /// Assegna un BusinessParty a un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="dto">Dati assegnazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Relazione creata</returns>
    [HttpPost("price-lists/{id}/business-parties")]
    [ProducesResponseType(typeof(PriceListBusinessPartyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignBusinessPartyToPriceList(
        Guid id,
        [FromBody] AssignBusinessPartyToPriceListDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await priceListBusinessPartyService.AssignBusinessPartyAsync(id, dto, currentUser, cancellationToken);
            return CreatedAtAction(
                nameof(GetBusinessPartiesForPriceList),
                new { id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rimuove un BusinessParty da un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="businessPartyId">ID del BusinessParty</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("price-lists/{id}/business-parties/{businessPartyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveBusinessPartyFromPriceList(
        Guid id,
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await priceListBusinessPartyService.RemoveBusinessPartyAsync(id, businessPartyId, currentUser, cancellationToken);

        if (!result)
            return NotFound(new { error = "Business party assignment not found" });

        return NoContent();
    }

    /// <summary>
    /// Ottiene tutti i BusinessParty assegnati a un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("price-lists/{id}/business-parties")]
    [ProducesResponseType(typeof(IEnumerable<PriceListBusinessPartyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBusinessPartiesForPriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListBusinessPartyService.GetBusinessPartiesForPriceListAsync(id, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Price List - Advanced Queries

    /// <summary>
    /// Ottiene i listini filtrati per tipo (Sales/Purchase).
    /// </summary>
    /// <param name="type">Tipo listino (Sales o Purchase)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("price-lists/by-type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListsByType(
        PriceListType type,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListService.GetPriceListsByTypeAsync(type, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Ottiene tutti i listini assegnati a un BusinessParty.
    /// </summary>
    /// <param name="id">ID del BusinessParty</param>
    /// <param name="type">Tipo listino (opzionale)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("business-parties/{id}/price-lists")]
    [ProducesResponseType(typeof(IEnumerable<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListsByBusinessParty(
        Guid id,
        [FromQuery] PriceListType? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListBusinessPartyService.GetPriceListsByBusinessPartyAsync(id, type, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Preview generazione listino da documenti di acquisto
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Preview con statistiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If supplier not found</response>
    [HttpPost("price-lists/generate-from-purchases/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewGenerateFromPurchases(
        [FromBody] GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await priceListGenerationService.PreviewGenerateFromPurchasesAsync(dto, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>ID del listino creato</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If supplier not found</response>
    [HttpPost("price-lists/generate-from-purchases")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> GenerateFromPurchases(
        [FromBody] GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";
            var priceListId = await priceListGenerationService.GenerateFromPurchasesAsync(dto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = priceListId },
                priceListId);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Preview aggiornamento listino esistente da documenti
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Preview con statistiche modifiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If price list not found</response>
    [HttpPost("price-lists/update-from-purchases/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewUpdateFromPurchases(
        [FromBody] UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await priceListGenerationService.PreviewUpdateFromPurchasesAsync(dto, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Risultato aggiornamento con statistiche</returns>
    /// <response code="200">Price list updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If price list not found</response>
    [HttpPost("price-lists/update-from-purchases")]
    [ProducesResponseType(typeof(UpdatePriceListResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdatePriceListResultDto>> UpdateFromPurchases(
        [FromBody] UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";
            var result = await priceListGenerationService.UpdateFromPurchasesAsync(dto, currentUser, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Preview generation of price list from product default prices
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview con statistiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("price-lists/generate-from-defaults/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewGenerateFromDefaults(
        [FromBody] GenerateFromDefaultPricesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Map from GenerateFromDefaultPricesDto to GeneratePriceListFromProductsDto
            var productsDto = new GeneratePriceListFromProductsDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Code = dto.Code,
                Type = PriceListType.Sales,
                Direction = PriceListDirection.Output,
                Priority = dto.Priority,
                IsDefault = dto.IsDefault,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                EventId = null,
                MarkupPercentage = dto.MarkupPercentage,
                RoundingStrategy = dto.RoundingStrategy ?? EventForge.DTOs.Common.RoundingStrategy.None,
                OnlyActiveProducts = dto.OnlyActiveProducts,
                OnlyProductsWithPrice = true,
                MinimumPrice = dto.MinimumPrice,
                FilterByCategoryIds = null,
                BusinessPartyIds = null
            };

            var result = await priceListService.PreviewGenerateFromProductPricesAsync(productsDto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Generate price list from product default prices
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID del listino creato</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("price-lists/generate-from-defaults")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> GenerateFromDefaults(
        [FromBody] GenerateFromDefaultPricesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";

            // Map from GenerateFromDefaultPricesDto to GeneratePriceListFromProductsDto
            var productsDto = new GeneratePriceListFromProductsDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Code = dto.Code,
                Type = PriceListType.Sales,
                Direction = PriceListDirection.Output,
                Priority = dto.Priority,
                IsDefault = dto.IsDefault,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                EventId = null,
                MarkupPercentage = dto.MarkupPercentage,
                RoundingStrategy = dto.RoundingStrategy ?? EventForge.DTOs.Common.RoundingStrategy.None,
                OnlyActiveProducts = dto.OnlyActiveProducts,
                OnlyProductsWithPrice = true,
                MinimumPrice = dto.MinimumPrice,
                FilterByCategoryIds = null,
                BusinessPartyIds = null
            };

            var priceListId = await priceListService.GenerateFromProductPricesAsync(productsDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = priceListId },
                priceListId);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    #endregion

    #region Promotions Management

    /// <summary>
    /// Gets all promotions with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of promotions</returns>
    /// <response code="200">Returns the paginated list of promotions</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("promotions")]
    [ProducesResponseType(typeof(PagedResult<PromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PromotionDto>>> GetPromotions(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var result = await promotionService.GetPromotionsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving promotions.", ex);
        }
    }

    /// <summary>
    /// Gets a promotion by ID.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Promotion information</returns>
    /// <response code="200">Returns the promotion</response>
    /// <response code="404">If the promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("promotions/{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> GetPromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var promotion = await promotionService.GetPromotionByIdAsync(id, cancellationToken);
            if (promotion is null)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return Ok(promotion);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the promotion.", ex);
        }
    }

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    /// <param name="createPromotionDto">Promotion creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created promotion information</returns>
    /// <response code="201">Promotion created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("promotions")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(
        [FromBody] CreatePromotionDto createPromotionDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await promotionService.CreatePromotionAsync(createPromotionDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the promotion.", ex);
        }
    }

    /// <summary>
    /// Updates an existing promotion.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="updatePromotionDto">Promotion update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated promotion information</returns>
    /// <response code="200">Promotion updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("promotions/{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(
        Guid id,
        [FromBody] UpdatePromotionDto updatePromotionDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await promotionService.UpdatePromotionAsync(id, updatePromotionDto, currentUser, cancellationToken);
            if (promotion is null)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return Ok(promotion);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the promotion.", ex);
        }
    }

    /// <summary>
    /// Deletes a promotion.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Promotion deleted successfully</response>
    /// <response code="404">If the promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("promotions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await promotionService.DeletePromotionAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the promotion.", ex);
        }
    }

    /// <summary>
    /// Validates a coupon code and returns the promotion details if valid.
    /// </summary>
    /// <param name="request">The coupon validation request containing the coupon code and optional customer ID.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Promotion details if the coupon is valid; 404 if invalid, expired, or max uses reached.</returns>
    /// <response code="200">Returns the promotion details for the valid coupon</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the coupon is invalid, expired, or has reached its usage limit</response>
    [HttpPost("promotions/validate-coupon")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDto>> ValidateCoupon(
        [FromBody] ValidateCouponRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var promotion = await promotionService.ValidateCouponAsync(request.CouponCode, request.CustomerId, cancellationToken);
            if (promotion is null)
                return CreateNotFoundProblem($"Coupon code '{request.CouponCode}' is invalid, expired, or has reached its usage limit.");

            return Ok(promotion);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while validating the coupon code.", ex);
        }
    }

    /// <summary>
    /// Applies promotions and coupon codes to a cart and returns the updated prices and discounts.
    /// </summary>
    /// <param name="applyDto">Cart items and context required for promotion evaluation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Promotion application result with updated line prices and applied discount details</returns>
    /// <response code="200">Returns the promotion application result</response>
    /// <response code="400">If the request payload is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("promotions/apply")]
    [ProducesResponseType(typeof(PromotionApplicationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionApplicationResultDto>> ApplyPromotions(
        [FromBody] ApplyPromotionRulesDto applyDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null) return tenantValidation;
        try
        {
            var result = await promotionService.ApplyPromotionRulesAsync(applyDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while applying promotions.", ex);
        }
    }

    /// <summary>
    /// Gets all rules for a promotion.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of promotion rules</returns>
    /// <response code="200">Returns the list of rules</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("promotions/{id:guid}/rules")]
    [ProducesResponseType(typeof(IEnumerable<PromotionRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PromotionRuleDto>>> GetPromotionRules(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var rules = await promotionService.GetPromotionRulesAsync(id, cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving promotion rules.", ex);
        }
    }

    /// <summary>
    /// Adds a new rule to a promotion.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="createDto">Rule creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created rule</returns>
    /// <response code="201">Rule created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the promotion is not found</response>
    [HttpPost("promotions/{id:guid}/rules")]
    [ProducesResponseType(typeof(PromotionRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionRuleDto>> AddPromotionRule(
        Guid id,
        [FromBody] CreatePromotionRuleDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var rule = await promotionService.AddPromotionRuleAsync(id, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPromotionRules), new { id }, rule);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the promotion rule.", ex);
        }
    }

    /// <summary>
    /// Updates an existing promotion rule.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="updateDto">Rule update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated rule</returns>
    /// <response code="200">Rule updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the rule is not found</response>
    [HttpPut("promotions/{id:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(typeof(PromotionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionRuleDto>> UpdatePromotionRule(
        Guid id,
        Guid ruleId,
        [FromBody] UpdatePromotionRuleDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var rule = await promotionService.UpdatePromotionRuleAsync(id, ruleId, updateDto, currentUser, cancellationToken);
            if (rule is null)
                return CreateNotFoundProblem($"Rule with ID {ruleId} not found for promotion {id}.");

            return Ok(rule);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the promotion rule.", ex);
        }
    }

    /// <summary>
    /// Deletes a promotion rule.
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Rule deleted successfully</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the rule is not found</response>
    [HttpDelete("promotions/{id:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePromotionRule(
        Guid id,
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await promotionService.DeletePromotionRuleAsync(id, ruleId, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Rule with ID {ruleId} not found for promotion {id}.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the promotion rule.", ex);
        }
    }

    /// <summary>
    /// Gets all products associated with a promotion rule.
    /// </summary>
    [HttpGet("promotions/{id:guid}/rules/{ruleId:guid}/products")]
    [ProducesResponseType(typeof(IEnumerable<PromotionRuleProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PromotionRuleProductDto>>> GetRuleProducts(
        Guid id, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null) return tenantValidation;
        try
        {
            var products = await promotionService.GetRuleProductsAsync(id, ruleId, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving rule products.", ex);
        }
    }

    /// <summary>
    /// Adds a product to a promotion rule.
    /// </summary>
    [HttpPost("promotions/{id:guid}/rules/{ruleId:guid}/products")]
    [ProducesResponseType(typeof(PromotionRuleProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionRuleProductDto>> AddRuleProduct(
        Guid id, Guid ruleId, [FromBody] CreatePromotionRuleProductDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null) return tenantValidation;
        try
        {
            var currentUser = GetCurrentUser();
            var result = await promotionService.AddRuleProductAsync(id, ruleId, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetRuleProducts), new { id, ruleId }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the product to the rule.", ex);
        }
    }

    /// <summary>
    /// Removes a product from a promotion rule.
    /// </summary>
    [HttpDelete("promotions/{id:guid}/rules/{ruleId:guid}/products/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRuleProduct(
        Guid id, Guid ruleId, Guid productId, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null) return tenantValidation;
        try
        {
            var currentUser = GetCurrentUser();
            var success = await promotionService.RemoveRuleProductAsync(id, ruleId, productId, currentUser, cancellationToken);
            if (!success) return CreateNotFoundProblem($"Product {productId} not found in rule {ruleId}.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing the product from the rule.", ex);
        }
    }

    #endregion

    #region Barcode Generation

    /// <summary>
    /// Generates a barcode or QR code based on the provided parameters.
    /// </summary>
    /// <param name="request">The barcode generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated barcode as base64 image</returns>
    /// <response code="200">Returns the generated barcode</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("barcodes/generate")]
    [ProducesResponseType(typeof(BarcodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BarcodeResponseDto>> GenerateBarcode(
        [FromBody] BarcodeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await barcodeService.GenerateBarcodeAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while generating the barcode.", ex);
        }
    }

    #endregion

    #region Brands Management

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

    #endregion

    #region Models Management

    /// <summary>
    /// Gets all models with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="brandId">Optional brand ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models</returns>
    /// <response code="200">Returns the paginated list of models</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("models")]
    [ProducesResponseType(typeof(PagedResult<ModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ModelDto>>> GetModels(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? brandId = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = brandId.HasValue
                ? await modelService.GetModelsByBrandIdAsync(brandId.Value, pagination, cancellationToken)
                : await modelService.GetModelsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving models.", ex);
        }
    }

    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model information</returns>
    /// <response code="200">Returns the model</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("models/{id:guid}")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> GetModel(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var model = await modelService.GetModelByIdAsync(id, cancellationToken);
            if (model is null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the model.", ex);
        }
    }

    /// <summary>
    /// Creates a new model.
    /// </summary>
    /// <param name="createModelDto">Model creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created model information</returns>
    /// <response code="201">Model created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("models")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> CreateModel(
        [FromBody] CreateModelDto createModelDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await modelService.CreateModelAsync(createModelDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetModel), new { id = model.Id }, model);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the model.", ex);
        }
    }

    /// <summary>
    /// Updates an existing model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="updateModelDto">Model update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated model information</returns>
    /// <response code="200">Model updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("models/{id:guid}")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> UpdateModel(
        Guid id,
        [FromBody] UpdateModelDto updateModelDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await modelService.UpdateModelAsync(id, updateModelDto, currentUser, cancellationToken);
            if (model is null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the model.", ex);
        }
    }

    /// <summary>
    /// Deletes a model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Model deleted successfully</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("models/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteModel(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await modelService.DeleteModelAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the model.", ex);
        }
    }

    #endregion

    #region Product Suppliers Management

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

    #region Product Document Movements and Stock Trend

    /// <summary>
    /// Gets paginated document movements for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="fromDate">Optional filter: start date</param>
    /// <param name="toDate">Optional filter: end date</param>
    /// <param name="businessPartyName">Optional filter: customer/supplier name</param>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product document movements</returns>
    /// <response code="200">Returns the paginated list of movements</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/document-movements")]
    [ProducesResponseType(typeof(PagedResult<ProductDocumentMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ProductDocumentMovementDto>>> GetProductDocumentMovements(
        Guid id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? businessPartyName = null,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination = null!,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            // Get document movements using DocumentHeaderService
            var queryParameters = new EventForge.DTOs.Documents.DocumentHeaderQueryParameters
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                ProductId = id,
                FromDate = fromDate,
                ToDate = toDate,
                CustomerName = businessPartyName,
                SortBy = "Date",
                SortDirection = "desc",
                IncludeRows = true
            };

            var documentsResult = await documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

            // Transform documents to ProductDocumentMovementDto
            var movements = new List<ProductDocumentMovementDto>();
            foreach (var doc in documentsResult.Items)
            {
                if (doc.Rows is null) continue;

                // Find rows that contain this product
                var productRows = doc.Rows.Where(r => r.ProductId == id);
                foreach (var row in productRows)
                {
                    // Determine if this is a stock increase based on document type
                    // This is a simplified logic - you may need to adjust based on your DocumentType configuration
                    bool isStockIncrease = DetermineStockIncrease(doc.DocumentTypeName);

                    movements.Add(new ProductDocumentMovementDto
                    {
                        DocumentHeaderId = doc.Id,
                        DocumentNumber = doc.Number,
                        DocumentDate = doc.Date,
                        DocumentTypeName = doc.DocumentTypeName ?? "Unknown",
                        BusinessPartyName = doc.BusinessPartyName ?? doc.CustomerName,
                        Status = doc.Status.ToString(),
                        Quantity = row.Quantity,
                        UnitOfMeasure = row.UnitOfMeasure,
                        UnitPrice = row.UnitPrice,
                        LineTotal = row.LineTotal,
                        IsStockIncrease = isStockIncrease,
                        WarehouseId = isStockIncrease ? doc.DestinationWarehouseId : doc.SourceWarehouseId,
                        WarehouseName = isStockIncrease ? doc.DestinationWarehouseName : doc.SourceWarehouseName
                    });
                }
            }

            var result = new PagedResult<ProductDocumentMovementDto>
            {
                Items = movements,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = documentsResult.TotalCount
            };

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document movements.", ex);
        }
    }

    /// <summary>
    /// Gets stock trend data for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="year">Year for trend data (defaults to current year)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stock trend data including data points and statistics</returns>
    /// <response code="200">Returns the stock trend data</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/stock-trend")]
    [ProducesResponseType(typeof(StockTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTrendDto>> GetProductStockTrend(
        Guid id,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            // Get stock movements for the year
            var movementsResult = await stockMovementService.GetMovementsAsync(
                page: 1,
                pageSize: 10000, // Get all movements for the year
                productId: id,
                fromDate: startDate,
                toDate: endDate,
                cancellationToken: cancellationToken);

            // Build data points - aggregate by day
            var dataPointsDict = new Dictionary<DateTime, (decimal Quantity, string? MovementType)>();
            var stockIncreasesList = new List<StockMovementPoint>();
            var stockDecreasesList = new List<StockMovementPoint>();
            decimal runningTotal = 0;

            // Order movements by date
            var orderedMovements = movementsResult.Items.OrderBy(m => m.MovementDate).ToList();

            foreach (var movement in orderedMovements)
            {
                var dateKey = movement.MovementDate.Date;

                // Update running total based on movement type
                if (movement.MovementType.Contains("Inbound", StringComparison.OrdinalIgnoreCase) ||
                    (movement.MovementType.Contains("Adjustment", StringComparison.OrdinalIgnoreCase) && movement.Quantity > 0))
                {
                    runningTotal += movement.Quantity;
                    stockIncreasesList.Add(new StockMovementPoint
                    {
                        Date = dateKey,
                        Quantity = movement.Quantity,
                        MovementType = movement.MovementType
                    });
                }
                else if (movement.MovementType.Contains("Outbound", StringComparison.OrdinalIgnoreCase) ||
                         (movement.MovementType.Contains("Adjustment", StringComparison.OrdinalIgnoreCase) && movement.Quantity < 0))
                {
                    runningTotal -= Math.Abs(movement.Quantity);
                    stockDecreasesList.Add(new StockMovementPoint
                    {
                        Date = dateKey,
                        Quantity = Math.Abs(movement.Quantity),
                        MovementType = movement.MovementType
                    });
                }

                dataPointsDict[dateKey] = (runningTotal, movement.MovementType);
            }

            var dataPoints = dataPointsDict
                .Select(kvp => new StockTrendDataPoint
                {
                    Date = kvp.Key,
                    Quantity = kvp.Value.Quantity,
                    MovementType = kvp.Value.MovementType
                })
                .OrderBy(dp => dp.Date)
                .ToList();

            // Calculate statistics
            var quantities = dataPoints.Select(dp => dp.Quantity).ToList();
            var currentStock = quantities.Any() ? quantities.Last() : 0;
            var minStock = quantities.Any() ? quantities.Min() : 0;
            var maxStock = quantities.Any() ? quantities.Max() : 0;
            var avgStock = quantities.Any() ? quantities.Average() : 0;

            var trendDto = new StockTrendDto
            {
                ProductId = id,
                Year = targetYear,
                DataPoints = dataPoints,
                StockIncreases = stockIncreasesList,
                StockDecreases = stockDecreasesList,
                CurrentStock = currentStock,
                MinStock = minStock,
                MaxStock = maxStock,
                AverageStock = avgStock
            };

            return Ok(trendDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock trend.", ex);
        }
    }

    /// <summary>
    /// Gets price trend data for a specific product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="year">Year for trend data (defaults to current year)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price trend data including purchase and sale prices with statistics</returns>
    /// <response code="200">Returns the price trend data</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{id:guid}/price-trend")]
    [ProducesResponseType(typeof(PriceTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceTrendDto>> GetProductPriceTrend(
        Guid id,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var product = await productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            // Get document movements for the year to extract price data
            var queryParameters = new EventForge.DTOs.Documents.DocumentHeaderQueryParameters
            {
                Page = 1,
                PageSize = 10000, // Get all movements for the year
                ProductId = id,
                FromDate = startDate,
                ToDate = endDate,
                SortBy = "Date",
                SortDirection = "asc",
                IncludeRows = true
            };

            var documentsResult = await documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

            var purchasePricesList = new List<PriceTrendDataPoint>();
            var salePricesList = new List<PriceTrendDataPoint>();

            // Process document movements to extract price data
            foreach (var doc in documentsResult.Items)
            {
                if (doc.Rows is null) continue;

                // Find rows that contain this product
                var productRows = doc.Rows.Where(r => r.ProductId == id);

                foreach (var row in productRows)
                {
                    // Normalize unit price to base unit if available
                    decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;

                    // Weight quantity: prefer BaseQuantity if available
                    decimal weightQuantity = row.BaseQuantity ?? row.Quantity;

                    // Calculate discount per unit
                    decimal unitDiscount = 0m;
                    if (row.Quantity > 0)
                    {
                        if (row.DiscountType == EventForge.DTOs.Common.DiscountType.Percentage)
                        {
                            unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
                        }
                        else // DiscountType.Value
                        {
                            unitDiscount = row.LineDiscountValue / row.Quantity;
                        }
                        // Clamp to prevent negative unit price
                        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
                    }

                    // Effective unit price after discount (net price)
                    // For purchase documents, this is considered VAT-exempt
                    decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

                    // Skip if effective price is zero or negative
                    if (effectiveUnitPrice <= 0) continue;

                    bool isStockIncrease = DetermineStockIncrease(doc.DocumentTypeName);

                    var pricePoint = new PriceTrendDataPoint
                    {
                        Date = doc.Date.Date,
                        Price = Math.Round(effectiveUnitPrice, 4),
                        Quantity = weightQuantity,
                        DocumentType = doc.DocumentTypeName,
                        BusinessPartyName = doc.BusinessPartyName ?? doc.CustomerName
                    };

                    if (isStockIncrease)
                    {
                        purchasePricesList.Add(pricePoint);
                    }
                    else
                    {
                        salePricesList.Add(pricePoint);
                    }
                }
            }

            // Calculate statistics for purchase prices
            var purchasePrices = purchasePricesList.Select(p => p.Price).Where(p => p > 0).ToList();
            var minPurchasePrice = purchasePrices.Any() ? purchasePrices.Min() : 0;
            var maxPurchasePrice = purchasePrices.Any() ? purchasePrices.Max() : 0;
            var avgPurchasePrice = purchasePrices.Any() ? purchasePrices.Average() : 0;

            // Calculate weighted average purchase price (by quantity)
            var totalPurchaseValue = purchasePricesList.Sum(p => p.Price * (p.Quantity ?? 0));
            var totalPurchaseQuantity = purchasePricesList.Sum(p => p.Quantity ?? 0);
            var currentAvgPurchasePrice = totalPurchaseQuantity > 0 ? totalPurchaseValue / totalPurchaseQuantity : 0;

            // Calculate statistics for sale prices
            var salePrices = salePricesList.Select(p => p.Price).Where(p => p > 0).ToList();
            var minSalePrice = salePrices.Any() ? salePrices.Min() : 0;
            var maxSalePrice = salePrices.Any() ? salePrices.Max() : 0;
            var avgSalePrice = salePrices.Any() ? salePrices.Average() : 0;

            // Calculate weighted average sale price (by quantity)
            var totalSaleValue = salePricesList.Sum(p => p.Price * (p.Quantity ?? 0));
            var totalSaleQuantity = salePricesList.Sum(p => p.Quantity ?? 0);
            var currentAvgSalePrice = totalSaleQuantity > 0 ? totalSaleValue / totalSaleQuantity : 0;

            var trendDto = new PriceTrendDto
            {
                ProductId = id,
                Year = targetYear,
                PurchasePrices = purchasePricesList.OrderBy(p => p.Date).ToList(),
                SalePrices = salePricesList.OrderBy(p => p.Date).ToList(),
                CurrentAveragePurchasePrice = currentAvgPurchasePrice,
                CurrentAverageSalePrice = currentAvgSalePrice,
                MinPurchasePrice = minPurchasePrice,
                MaxPurchasePrice = maxPurchasePrice,
                MinSalePrice = minSalePrice,
                MaxSalePrice = maxSalePrice,
                AveragePurchasePrice = avgPurchasePrice,
                AverageSalePrice = avgSalePrice
            };

            return Ok(trendDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price trend.", ex);
        }
    }

    /// <summary>
    /// Helper method to determine if a document type increases stock.
    /// This is a simplified implementation - adjust based on your DocumentType configuration.
    /// </summary>
    private bool DetermineStockIncrease(string? documentTypeName)
    {
        if (string.IsNullOrEmpty(documentTypeName))
            return false;

        // Common patterns for stock increase (purchases, receipts, returns from customers)
        var increaseKeywords = new[] { "purchase", "receipt", "return", "acquisto", "carico", "reso" };

        // Common patterns for stock decrease (sales, shipments, returns to suppliers)
        var decreaseKeywords = new[] { "sale", "invoice", "shipment", "delivery", "vendita", "fattura", "scarico", "consegna" };

        var lowerName = documentTypeName.ToLower();

        if (increaseKeywords.Any(k => lowerName.Contains(k)))
            return true;

        if (decreaseKeywords.Any(k => lowerName.Contains(k)))
            return false;

        // Default to false if uncertain
        return false;
    }

    #endregion

    #region Product Recent Transactions

    /// <summary>
    /// Gets recent product transactions for price suggestions.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="type">Transaction type: "purchase" or "sale" (default: "purchase")</param>
    /// <param name="partyId">Optional business party ID to filter results</param>
    /// <param name="top">Number of recent transactions to return (default: 3, max: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent product transactions</returns>
    /// <response code="200">Returns the list of recent transactions</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{productId}/recent-transactions")]
    [ProducesResponseType(typeof(IEnumerable<RecentProductTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RecentProductTransactionDto>>> GetRecentProductTransactions(
        Guid productId,
        [FromQuery] string type = "purchase",
        [FromQuery] Guid? partyId = null,
        [FromQuery] int top = 3,
        CancellationToken cancellationToken = default)
    {
        // Validate type parameter
        if (!type.Equals("purchase", StringComparison.OrdinalIgnoreCase) &&
            !type.Equals("sale", StringComparison.OrdinalIgnoreCase))
        {
            return CreateValidationProblemDetails("Type parameter must be either 'purchase' or 'sale'.");
        }

        // Validate top parameter
        if (top < 1 || top > 10)
        {
            return CreateValidationProblemDetails("Top parameter must be between 1 and 10.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var productExists = await productService.ProductExistsAsync(productId, cancellationToken);
            if (!productExists)
            {
                return CreateNotFoundProblem($"Product with ID {productId} not found.");
            }

            var transactions = await productService.GetRecentProductTransactionsAsync(
                productId,
                type,
                partyId,
                top,
                cancellationToken);

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving recent transactions.", ex);
        }
    }

    #endregion

    #region Price Calculation

    /// <summary>
    /// Calcola il prezzo applicato per un prodotto considerando listini e BusinessParty.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="eventId">ID evento</param>
    /// <param name="businessPartyId">ID BusinessParty (opzionale)</param>
    /// <param name="quantity">Quantità</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId}/applied-price")]
    [ProducesResponseType(typeof(AppliedPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppliedPrice(
        Guid productId,
        [FromQuery] Guid eventId,
        [FromQuery] Guid? businessPartyId = null,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await priceCalculationService.GetAppliedPriceAsync(
            productId,
            eventId,
            businessPartyId,
            null,
            quantity,
            cancellationToken);

        if (result is null)
            return NotFound(new { error = "No applicable price found for this product" });

        return Ok(result);
    }

    /// <summary>
    /// Confronta i prezzi di acquisto per un prodotto da tutti i fornitori.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="quantity">Quantità</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId}/purchase-price-comparison")]
    [ProducesResponseType(typeof(List<PurchasePriceComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchasePriceComparison(
        Guid productId,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await priceCalculationService.GetPurchasePriceComparisonAsync(
            productId,
            quantity,
            null,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// Supporta modalità: Automatico, Listino Forzato, Manuale, Ibrido.
    /// </summary>
    /// <param name="request">Parametri per il calcolo del prezzo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dettagli del prezzo applicato</returns>
    [HttpPost("products/price")]
    [ProducesResponseType(typeof(ProductPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductPrice(
        [FromBody] GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await priceListService.GetProductPriceAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Export Operations

    /// <summary>
    /// Export all products to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("products/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await productService.GetProductsForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await exportService.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Products_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await exportService.ExportToExcelAsync(data, "Products", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Products_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Performs a bulk price update on multiple products.
    /// </summary>
    /// <param name="bulkUpdateDto">Bulk update request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk update operation</returns>
    /// <response code="200">Returns the result of the bulk update operation</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(EventForge.DTOs.Bulk.BulkUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventForge.DTOs.Bulk.BulkUpdateResultDto>> BulkUpdatePrices(
        [FromBody] EventForge.DTOs.Bulk.BulkUpdatePricesDto bulkUpdateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "System";
            var result = await productService.BulkUpdatePricesAsync(bulkUpdateDto, currentUser, cancellationToken);

            logger.LogInformation(
                "Bulk price update: {SuccessCount} successful, {FailedCount} failed",
                result.SuccessCount, result.FailedCount);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid bulk update request");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during bulk price update.", ex);
        }
    }

    #endregion

    #endregion
}

/// <summary>
/// DTO for image upload result.
/// </summary>
public class ImageUploadResultDto
{
    /// <summary>
    /// URL of the uploaded image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}