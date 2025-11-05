using EventForge.DTOs.PriceLists;
using EventForge.DTOs.Products;
using EventForge.DTOs.Promotions;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
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
public class ProductManagementController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IBrandService _brandService;
    private readonly IModelService _modelService;
    private readonly IUMService _umService;
    private readonly IPriceListService _priceListService;
    private readonly IPromotionService _promotionService;
    private readonly IBarcodeService _barcodeService;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IStockMovementService _stockMovementService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProductManagementController> _logger;

    public ProductManagementController(
        IProductService productService,
        IBrandService brandService,
        IModelService modelService,
        IUMService umService,
        IPriceListService priceListService,
        IPromotionService promotionService,
        IBarcodeService barcodeService,
        IDocumentHeaderService documentHeaderService,
        IStockMovementService stockMovementService,
        ITenantContext tenantContext,
        ILogger<ProductManagementController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _brandService = brandService ?? throw new ArgumentNullException(nameof(brandService));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _umService = umService ?? throw new ArgumentNullException(nameof(umService));
        _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Product CRUD Operations

    /// <summary>
    /// Gets all products with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _productService.GetProductsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving products.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving products.", ex);
        }
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    /// <param name="id">Product ID</param>
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            if (product == null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the product.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product.", ex);
        }
    }

    /// <summary>
    /// Gets a product by barcode/code value.
    /// </summary>
    /// <param name="code">Barcode or code value to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product information if found</returns>
    /// <response code="200">Returns the product</response>
    /// <response code="404">If no product with the given code is found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("products/by-code/{code}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> GetProductByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var product = await _productService.GetProductByCodeAsync(code, cancellationToken);
            if (product == null)
                return CreateNotFoundProblem($"Product with code '{code}' not found.");

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the product.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product.", ex);
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await _productService.CreateProductAsync(createProductDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the product.");
            return CreateInternalServerErrorProblem("An error occurred while creating the product.", ex);
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await _productService.UpdateProductAsync(id, updateProductDto, currentUser, cancellationToken);
            if (product == null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return Ok(product);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the product.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _productService.DeleteProductAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the product.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (file == null || file.Length == 0)
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
            _logger.LogError(ex, "An error occurred while uploading the product image.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (file == null || file.Length == 0)
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
            var updatedProduct = await _productService.UploadProductImageAsync(id, file, cancellationToken);
            if (updatedProduct == null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while uploading the product image for product {ProductId}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var imageDocument = await _productService.GetProductImageDocumentAsync(id, cancellationToken);
            if (imageDocument == null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found or has no image.");
            }

            return Ok(imageDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the product image for product {ProductId}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _productService.DeleteProductImageAsync(id, cancellationToken);
            if (!success)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found or has no image.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the product image for product {ProductId}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var codes = await _productService.GetProductCodesAsync(productId, cancellationToken);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving product codes.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productCode = await _productService.AddProductCodeAsync(createProductCodeDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductCodes),
                new { productId = productId },
                productCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding the product code.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var units = await _productService.GetProductUnitsAsync(productId, cancellationToken);
            return Ok(units);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving product units.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productUnit = await _productService.AddProductUnitAsync(createProductUnitDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductUnits),
                new { productId = productId },
                productUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding the product unit.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var productUnit = await _productService.UpdateProductUnitAsync(id, updateProductUnitDto, currentUser, cancellationToken);

            if (productUnit == null)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} was not found.");
            }

            return Ok(productUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the product unit.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _productService.RemoveProductUnitAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} was not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the product unit.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the product unit.", ex);
        }
    }

    #endregion

    #region Unit of Measures Management

    /// <summary>
    /// Gets all units of measure with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _umService.GetUMsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving units of measure.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var unit = await _umService.GetUMByIdAsync(id, cancellationToken);
            if (unit == null)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return Ok(unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the unit of measure.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await _umService.CreateUMAsync(createUMDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetUnitOfMeasure), new { id = unit.Id }, unit);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the unit of measure.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await _umService.UpdateUMAsync(id, updateUMDto, currentUser, cancellationToken);
            if (unit == null)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return Ok(unit);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the unit of measure.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _umService.DeleteUMAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the unit of measure.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the unit of measure.", ex);
        }
    }

    #endregion

    #region Price Lists Management

    /// <summary>
    /// Gets all price lists with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _priceListService.GetPriceListsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving price lists.");
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
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var priceList = await _priceListService.GetPriceListByIdAsync(id, cancellationToken);
            if (priceList == null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the price list.");
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

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await _priceListService.CreatePriceListAsync(createPriceListDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the price list.");
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

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await _priceListService.UpdatePriceListAsync(id, updatePriceListDto, currentUser, cancellationToken);
            if (priceList == null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the price list.");
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
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _priceListService.DeletePriceListAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the price list.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the price list.", ex);
        }
    }

    #endregion

    #region Promotions Management

    /// <summary>
    /// Gets all promotions with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _promotionService.GetPromotionsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving promotions.");
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
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id, cancellationToken);
            if (promotion == null)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return Ok(promotion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the promotion.");
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

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await _promotionService.CreatePromotionAsync(createPromotionDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the promotion.");
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

        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await _promotionService.UpdatePromotionAsync(id, updatePromotionDto, currentUser, cancellationToken);
            if (promotion == null)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return Ok(promotion);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the promotion.");
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
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _promotionService.DeletePromotionAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the promotion.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the promotion.", ex);
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _barcodeService.GenerateBarcodeAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating the barcode.");
            return CreateInternalServerErrorProblem("An error occurred while generating the barcode.", ex);
        }
    }

    #endregion

    #region Brands Management

    /// <summary>
    /// Gets all brands with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _brandService.GetBrandsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving brands.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id, cancellationToken);
            if (brand == null)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return Ok(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the brand.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var brand = await _brandService.CreateBrandAsync(createBrandDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the brand.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var brand = await _brandService.UpdateBrandAsync(id, updateBrandDto, currentUser, cancellationToken);
            if (brand == null)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return Ok(brand);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the brand.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _brandService.DeleteBrandAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Brand with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the brand.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the brand.", ex);
        }
    }

    #endregion

    #region Models Management

    /// <summary>
    /// Gets all models with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? brandId = null,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = brandId.HasValue
                ? await _modelService.GetModelsByBrandIdAsync(brandId.Value, page, pageSize, cancellationToken)
                : await _modelService.GetModelsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving models.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var model = await _modelService.GetModelByIdAsync(id, cancellationToken);
            if (model == null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the model.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await _modelService.CreateModelAsync(createModelDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetModel), new { id = model.Id }, model);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the model.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await _modelService.UpdateModelAsync(id, updateModelDto, currentUser, cancellationToken);
            if (model == null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the model.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _modelService.DeleteModelAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the model.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var suppliers = await _productService.GetProductSuppliersAsync(productId, cancellationToken);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving suppliers for product {ProductId}.", productId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var supplier = await _productService.GetProductSupplierByIdAsync(id, cancellationToken);
            if (supplier == null)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving product supplier {Id}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var currentUser = GetCurrentUser();
            var productSupplier = await _productService.AddProductSupplierAsync(createProductSupplierDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetProductSupplier), new { id = productSupplier.Id }, productSupplier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while adding product supplier.");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding product supplier.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var currentUser = GetCurrentUser();
            var productSupplier = await _productService.UpdateProductSupplierAsync(id, updateProductSupplierDto, currentUser, cancellationToken);
            if (productSupplier == null)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return Ok(productSupplier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating product supplier.");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating product supplier {Id}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await _productService.RemoveProductSupplierAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Product supplier with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting product supplier {Id}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var products = await _productService.GetProductsWithSupplierAssociationAsync(supplierId, cancellationToken);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving products for supplier {SupplierId}.", supplierId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var count = await _productService.BulkUpdateProductSupplierAssociationsAsync(supplierId, productIds, currentUser, cancellationToken);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while bulk updating product-supplier associations for supplier {SupplierId}.", supplierId);
            return CreateInternalServerErrorProblem("An error occurred while updating the associations.", ex);
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
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if product exists
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            if (product == null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            // Get document movements using DocumentHeaderService
            var queryParameters = new EventForge.DTOs.Documents.DocumentHeaderQueryParameters
            {
                Page = page,
                PageSize = pageSize,
                ProductId = id,
                FromDate = fromDate,
                ToDate = toDate,
                CustomerName = businessPartyName,
                SortBy = "Date",
                SortDirection = "desc",
                IncludeRows = true
            };

            var documentsResult = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

            // Transform documents to ProductDocumentMovementDto
            var movements = new List<ProductDocumentMovementDto>();
            foreach (var doc in documentsResult.Items)
            {
                if (doc.Rows == null) continue;

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

            return Ok(new PagedResult<ProductDocumentMovementDto>
            {
                Items = movements,
                Page = page,
                PageSize = pageSize,
                TotalCount = documentsResult.TotalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document movements for product {ProductId}.", id);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if product exists
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            if (product == null)
                return CreateNotFoundProblem($"Product with ID {id} not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            // Get stock movements for the year
            var movementsResult = await _stockMovementService.GetMovementsAsync(
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
            _logger.LogError(ex, "An error occurred while retrieving stock trend for product {ProductId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while retrieving stock trend.", ex);
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