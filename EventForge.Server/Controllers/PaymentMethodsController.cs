using EventForge.DTOs.Sales;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for payment method management with multi-tenant support.
/// Provides CRUD operations for payment methods used in sales transactions.
/// </summary>
[Route("api/v1/payment-methods")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("SalesManagement")]
public class PaymentMethodsController : BaseApiController
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PaymentMethodsController> _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public PaymentMethodsController(
        IPaymentMethodService paymentMethodService,
        ITenantContext tenantContext,
        ILogger<PaymentMethodsController> logger,
        ICacheInvalidationService cacheInvalidation)
    {
        _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    }

    /// <summary>
    /// Gets all payment methods with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of payment methods</returns>
    /// <response code="200">Successfully retrieved payment methods with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PaymentMethodDto>>> GetPaymentMethods(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentMethodService.GetPaymentMethodsAsync(pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving payment methods.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving payment methods.", ex);
        }
    }

    /// <summary>
    /// Gets only active payment methods with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of active payment methods ordered by display order</returns>
    /// <response code="200">Successfully retrieved active payment methods with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("active")]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PaymentMethodDto>>> GetActivePaymentMethods(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentMethodService.GetActivePaymentMethodsAsync(pagination, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving active payment methods.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving active payment methods.", ex);
        }
    }

    /// <summary>
    /// Gets a specific payment method by ID.
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment method details</returns>
    /// <response code="200">Returns the payment method</response>
    /// <response code="404">If the payment method is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethodById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, cancellationToken);
            if (paymentMethod == null)
            {
                return CreateNotFoundProblem($"Payment method with ID {id} not found.");
            }

            return Ok(paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving payment method {PaymentMethodId}.", id);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving payment method {id}.", ex);
        }
    }

    /// <summary>
    /// Gets a specific payment method by code.
    /// </summary>
    /// <param name="code">Payment method code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment method details</returns>
    /// <response code="200">Returns the payment method</response>
    /// <response code="404">If the payment method is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethodByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var paymentMethod = await _paymentMethodService.GetPaymentMethodByCodeAsync(code, cancellationToken);
            if (paymentMethod == null)
            {
                return CreateNotFoundProblem($"Payment method with code '{code}' not found.");
            }

            return Ok(paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving payment method by code {Code}.", code);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving payment method by code '{code}'.", ex);
        }
    }

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    /// <param name="createDto">Payment method creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment method</returns>
    /// <response code="201">Returns the newly created payment method</response>
    /// <response code="400">If the data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If a payment method with the same code already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentMethodDto>> CreatePaymentMethod(
        [FromBody] CreatePaymentMethodDto createDto,
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
            var paymentMethod = await _paymentMethodService.CreatePaymentMethodAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPaymentMethodById),
                new { id = paymentMethod.Id },
                paymentMethod);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Attempt to create duplicate payment method code.");
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating payment method.");
            return CreateInternalServerErrorProblem("An error occurred while creating payment method.", ex);
        }
    }

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="updateDto">Payment method update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment method</returns>
    /// <response code="200">Returns the updated payment method</response>
    /// <response code="400">If the data is invalid</response>
    /// <response code="404">If the payment method is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentMethodDto>> UpdatePaymentMethod(
        Guid id,
        [FromBody] UpdatePaymentMethodDto updateDto,
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
            var paymentMethod = await _paymentMethodService.UpdatePaymentMethodAsync(id, updateDto, currentUser, cancellationToken);

            if (paymentMethod == null)
            {
                return CreateNotFoundProblem($"Payment method with ID {id} not found.");
            }

            await _cacheInvalidation.InvalidateSemiStaticEntitiesAsync(cancellationToken);
            return Ok(paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating payment method {PaymentMethodId}.", id);
            return CreateInternalServerErrorProblem($"An error occurred while updating payment method {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes a payment method (soft delete).
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the payment method was successfully deleted</response>
    /// <response code="404">If the payment method is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePaymentMethod(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _paymentMethodService.DeletePaymentMethodAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Payment method with ID {id} not found.");
            }

            await _cacheInvalidation.InvalidateSemiStaticEntitiesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting payment method {PaymentMethodId}.", id);
            return CreateInternalServerErrorProblem($"An error occurred while deleting payment method {id}.", ex);
        }
    }

    /// <summary>
    /// Checks if a payment method code already exists.
    /// </summary>
    /// <param name="code">Payment method code to check</param>
    /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code exists, false otherwise</returns>
    /// <response code="200">Returns whether the code exists</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("check-code/{code}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CheckCodeExists(
        string code,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _paymentMethodService.CodeExistsAsync(code, excludeId, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking payment method code.");
            return CreateInternalServerErrorProblem("An error occurred while checking payment method code.", ex);
        }
    }

    /// <summary>
    /// Creates a ProblemDetails response for conflict errors.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>Conflict result with ProblemDetails</returns>
    private new ActionResult CreateConflictProblem(string message)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = message,
            Instance = HttpContext.Request.Path
        };

        if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return Conflict(problemDetails);
    }

    /// <summary>
    /// Validates tenant access for the current request.
    /// </summary>
    private new async Task<ActionResult?> ValidateTenantAccessAsync(ITenantContext tenantContext)
    {
        if (!tenantContext.CurrentTenantId.HasValue)
        {
            return Forbid();
        }

        // Additional tenant access validation could be added here
        await Task.CompletedTask;
        return null;
    }
}
