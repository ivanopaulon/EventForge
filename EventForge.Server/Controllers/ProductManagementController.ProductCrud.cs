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
    /// Gets all products with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="searchTerm">Optional search term to filter products by code, name, or description</param>
    /// <param name="classificationNodeId">Optional classification node ID to filter products by category</param>
    /// <param name="includeInactive">If true, includes inactive products in the result</param>
    /// <param name="quickFilter">Quick filter string for product name or code</param>
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
        [FromQuery] Guid? classificationNodeId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? quickFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await productService.GetProductsAsync(pagination, searchTerm, classificationNodeId, includeInactive, quickFilter, cancellationToken);

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


    /// <param name="id">Product unique identifier</param>
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
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
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

}
