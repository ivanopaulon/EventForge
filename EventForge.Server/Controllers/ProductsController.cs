using EventForge.DTOs.Products;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for product management with multi-tenant support.
/// Provides comprehensive CRUD operations for products, product codes, units, and bundles
/// within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly ITenantContext _tenantContext;

    public ProductsController(IProductService productService, ITenantContext tenantContext)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        // Validate tenant access
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _productService.GetProductsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
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
    [HttpGet("{id:guid}")]
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
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product.", ex);
        }
    }

    /// <summary>
    /// Gets detailed product information including codes, units, and bundle items.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed product information</returns>
    /// <response code="200">Returns the detailed product information</response>
    /// <response code="404">If the product is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDetailDto>> GetProductDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var productDetail = await _productService.GetProductDetailAsync(id, cancellationToken);

            if (productDetail == null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(productDetail);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the product details.", ex);
        }
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product</returns>
    /// <response code="201">Returns the newly created product</response>
    /// <response code="400">If the product data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto createProductDto,
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
            var product = await _productService.CreateProductAsync(createProductDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the product.", ex);
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product</returns>
    /// <response code="200">Returns the updated product</response>
    /// <response code="400">If the product data is invalid</response>
    /// <response code="404">If the product is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
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
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var product = await _productService.UpdateProductAsync(id, updateProductDto, currentUser, cancellationToken);

            if (product == null)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the product.", ex);
        }
    }

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="404">If the product is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _productService.DeleteProductAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the product.", error = ex.Message });
        }
    }

    #endregion

    #region Product Code Management Operations

    /// <summary>
    /// Gets all codes for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product codes</returns>
    /// <response code="200">Returns the list of product codes</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("{productId:guid}/codes")]
    [ProducesResponseType(typeof(IEnumerable<ProductCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductCodeDto>>> GetProductCodes(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _productService.ProductExistsAsync(productId, cancellationToken))
            {
                return CreateNotFoundProblem($"Product with ID {productId} not found.");
            }

            var codes = await _productService.GetProductCodesAsync(productId, cancellationToken);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving product codes.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a product code by ID.
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product code information</returns>
    /// <response code="200">Returns the product code</response>
    /// <response code="404">If the product code is not found</response>
    [HttpGet("codes/{id:guid}")]
    [ProducesResponseType(typeof(ProductCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductCodeDto>> GetProductCode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = await _productService.GetProductCodeByIdAsync(id, cancellationToken);

            if (code == null)
            {
                return CreateNotFoundProblem($"Product code with ID {id} not found.");
            }

            return Ok(code);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the product code.", error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a new code to a product.
    /// </summary>
    /// <param name="createProductCodeDto">Product code creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product code</returns>
    /// <response code="201">Returns the newly created product code</response>
    /// <response code="400">If the product code data is invalid</response>
    [HttpPost("codes")]
    [ProducesResponseType(typeof(ProductCodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductCodeDto>> AddProductCode(
        [FromBody] CreateProductCodeDto createProductCodeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var code = await _productService.AddProductCodeAsync(createProductCodeDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetProductCode), new { id = code.Id }, code);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the product code.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing product code.
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="updateProductCodeDto">Product code update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product code</returns>
    /// <response code="200">Returns the updated product code</response>
    /// <response code="400">If the product code data is invalid</response>
    /// <response code="404">If the product code is not found</response>
    [HttpPut("codes/{id:guid}")]
    [ProducesResponseType(typeof(ProductCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductCodeDto>> UpdateProductCode(
        Guid id,
        [FromBody] UpdateProductCodeDto updateProductCodeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var code = await _productService.UpdateProductCodeAsync(id, updateProductCodeDto, currentUser, cancellationToken);

            if (code == null)
            {
                return CreateNotFoundProblem($"Product code with ID {id} not found.");
            }

            return Ok(code);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the product code.", error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a code from a product (soft delete).
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Product code deleted successfully</response>
    /// <response code="404">If the product code is not found</response>
    [HttpDelete("codes/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductCode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _productService.RemoveProductCodeAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Product code with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the product code.", error = ex.Message });
        }
    }

    #endregion

    #region Product Unit Management Operations

    /// <summary>
    /// Gets all units for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product units</returns>
    /// <response code="200">Returns the list of product units</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("{productId:guid}/units")]
    [ProducesResponseType(typeof(IEnumerable<ProductUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductUnitDto>>> GetProductUnits(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _productService.ProductExistsAsync(productId, cancellationToken))
            {
                return CreateNotFoundProblem($"Product with ID {productId} not found.");
            }

            var units = await _productService.GetProductUnitsAsync(productId, cancellationToken);
            return Ok(units);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving product units.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a product unit by ID.
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product unit information</returns>
    /// <response code="200">Returns the product unit</response>
    /// <response code="404">If the product unit is not found</response>
    [HttpGet("units/{id:guid}")]
    [ProducesResponseType(typeof(ProductUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductUnitDto>> GetProductUnit(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var unit = await _productService.GetProductUnitByIdAsync(id, cancellationToken);

            if (unit == null)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} not found.");
            }

            return Ok(unit);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the product unit.", error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a new unit to a product.
    /// </summary>
    /// <param name="createProductUnitDto">Product unit creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product unit</returns>
    /// <response code="201">Returns the newly created product unit</response>
    /// <response code="400">If the product unit data is invalid</response>
    [HttpPost("units")]
    [ProducesResponseType(typeof(ProductUnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductUnitDto>> AddProductUnit(
        [FromBody] CreateProductUnitDto createProductUnitDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await _productService.AddProductUnitAsync(createProductUnitDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetProductUnit), new { id = unit.Id }, unit);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the product unit.", error = ex.Message });
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
    /// <response code="400">If the product unit data is invalid</response>
    /// <response code="404">If the product unit is not found</response>
    [HttpPut("units/{id:guid}")]
    [ProducesResponseType(typeof(ProductUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductUnitDto>> UpdateProductUnit(
        Guid id,
        [FromBody] UpdateProductUnitDto updateProductUnitDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var unit = await _productService.UpdateProductUnitAsync(id, updateProductUnitDto, currentUser, cancellationToken);

            if (unit == null)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} not found.");
            }

            return Ok(unit);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the product unit.", error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a unit from a product (soft delete).
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Product unit deleted successfully</response>
    /// <response code="404">If the product unit is not found</response>
    [HttpDelete("units/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductUnit(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _productService.RemoveProductUnitAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Product unit with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the product unit.", error = ex.Message });
        }
    }

    #endregion

    #region Product Bundle Item Management Operations

    /// <summary>
    /// Gets all bundle items for a product.
    /// </summary>
    /// <param name="bundleProductId">Bundle product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bundle items</returns>
    /// <response code="200">Returns the list of bundle items</response>
    /// <response code="404">If the bundle product is not found</response>
    [HttpGet("{bundleProductId:guid}/bundle-items")]
    [ProducesResponseType(typeof(IEnumerable<ProductBundleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductBundleItemDto>>> GetProductBundleItems(
        Guid bundleProductId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _productService.ProductExistsAsync(bundleProductId, cancellationToken))
            {
                return CreateNotFoundProblem($"Bundle product with ID {bundleProductId} not found.");
            }

            var bundleItems = await _productService.GetProductBundleItemsAsync(bundleProductId, cancellationToken);
            return Ok(bundleItems);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving bundle items.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a bundle item by ID.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bundle item information</returns>
    /// <response code="200">Returns the bundle item</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpGet("bundle-items/{id:guid}")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductBundleItemDto>> GetProductBundleItem(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bundleItem = await _productService.GetProductBundleItemByIdAsync(id, cancellationToken);

            if (bundleItem == null)
            {
                return CreateNotFoundProblem($"Bundle item with ID {id} not found.");
            }

            return Ok(bundleItem);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the bundle item.", error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a new bundle item to a product.
    /// </summary>
    /// <param name="createProductBundleItemDto">Bundle item creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bundle item</returns>
    /// <response code="201">Returns the newly created bundle item</response>
    /// <response code="400">If the bundle item data is invalid</response>
    [HttpPost("bundle-items")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductBundleItemDto>> AddProductBundleItem(
        [FromBody] CreateProductBundleItemDto createProductBundleItemDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var bundleItem = await _productService.AddProductBundleItemAsync(createProductBundleItemDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetProductBundleItem), new { id = bundleItem.Id }, bundleItem);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the bundle item.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing bundle item.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="updateProductBundleItemDto">Bundle item update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bundle item</returns>
    /// <response code="200">Returns the updated bundle item</response>
    /// <response code="400">If the bundle item data is invalid</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpPut("bundle-items/{id:guid}")]
    [ProducesResponseType(typeof(ProductBundleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductBundleItemDto>> UpdateProductBundleItem(
        Guid id,
        [FromBody] UpdateProductBundleItemDto updateProductBundleItemDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var bundleItem = await _productService.UpdateProductBundleItemAsync(id, updateProductBundleItemDto, currentUser, cancellationToken);

            if (bundleItem == null)
            {
                return CreateNotFoundProblem($"Bundle item with ID {id} not found.");
            }

            return Ok(bundleItem);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the bundle item.", error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a bundle item from a product (soft delete).
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Bundle item deleted successfully</response>
    /// <response code="404">If the bundle item is not found</response>
    [HttpDelete("bundle-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductBundleItem(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _productService.RemoveProductBundleItemAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Bundle item with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the bundle item.", error = ex.Message });
        }
    }

    #endregion

    #region Image Upload Operations

    /// <summary>
    /// Uploads an image for a product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="file">Image file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product with new image URL</returns>
    /// <response code="200">Returns the updated product with new image URL</response>
    /// <response code="400">If the file is invalid or validation fails</response>
    /// <response code="404">If the product is not found</response>
    [HttpPost("{id:guid}/image")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UploadProductImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file presence
        if (file == null || file.Length == 0)
        {
            return CreateValidationProblemDetails("No file was uploaded.");
        }

        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails("File size exceeds the maximum limit of 5MB.");
        }

        // Validate file format
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
        {
            return CreateValidationProblemDetails("Invalid file format. Only JPG, JPEG, PNG, and GIF files are allowed.");
        }

        // Validate content type
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return CreateValidationProblemDetails("Invalid file content type. Only image files are allowed.");
        }

        try
        {
            // Check if product exists
            if (!await _productService.ProductExistsAsync(id, cancellationToken))
            {
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            // Create directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var uniqueFileName = $"{id}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Generate relative URL for the image
            var relativeUrl = $"/images/products/{uniqueFileName}";

            // Update product with new image URL
            var currentUser = GetCurrentUser();
            var updatedProduct = await _productService.UpdateProductImageAsync(id, relativeUrl, currentUser, cancellationToken);

            if (updatedProduct == null)
            {
                // Clean up uploaded file if product update failed
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return CreateNotFoundProblem($"Product with ID {id} not found.");
            }

            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while uploading the image.", error = ex.Message });
        }
    }

    #endregion
}