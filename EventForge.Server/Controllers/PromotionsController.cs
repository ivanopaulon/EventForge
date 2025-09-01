using EventForge.DTOs.Promotions;
using EventForge.Server.Services.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for promotion management with multi-tenant support.
/// Provides CRUD operations for promotions within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class PromotionsController : BaseApiController
{
    private readonly IPromotionService _promotionService;
    private readonly ITenantContext _tenantContext;

    public PromotionsController(IPromotionService promotionService, ITenantContext tenantContext)
    {
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PromotionDto>>> GetPromotions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        // Validate tenant access
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
            return CreateInternalServerErrorProblem("An error occurred while retrieving promotions.", ex);
        }
    }

    /// <summary>
    /// Gets active promotions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active promotions</returns>
    /// <response code="200">Returns the list of active promotions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetActivePromotions(
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var promotions = await _promotionService.GetActivePromotionsAsync(cancellationToken);
            return Ok(promotions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active promotions.", ex);
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
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> GetPromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
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
            return CreateInternalServerErrorProblem("An error occurred while retrieving the promotion.", ex);
        }
    }

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    /// <param name="createDto">Promotion creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created promotion</returns>
    /// <response code="201">Returns the newly created promotion</response>
    /// <response code="400">If the promotion data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(
        [FromBody] CreatePromotionDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await _promotionService.CreatePromotionAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPromotion),
                new { id = promotion.Id },
                promotion);
        }
        catch (ArgumentException ex)
        {
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
    /// <param name="updateDto">Promotion update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated promotion</returns>
    /// <response code="200">Returns the updated promotion</response>
    /// <response code="400">If the promotion data is invalid</response>
    /// <response code="404">If the promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(
        Guid id,
        [FromBody] UpdatePromotionDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var promotion = await _promotionService.UpdatePromotionAsync(id, updateDto, currentUser, cancellationToken);

            if (promotion == null)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return Ok(promotion);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the promotion.", ex);
        }
    }

    /// <summary>
    /// Deletes a promotion (soft delete).
    /// </summary>
    /// <param name="id">Promotion ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Promotion deleted successfully</response>
    /// <response code="404">If the promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _promotionService.DeletePromotionAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Promotion with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the promotion.", ex);
        }
    }

    /// <summary>
    /// Applies promotion rules to a cart or order.
    /// </summary>
    /// <param name="applyDto">Cart/order data for promotion application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with applied discounts and affected items</returns>
    /// <response code="200">Returns the promotion application result</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(PromotionApplicationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromotionApplicationResultDto>> ApplyPromotions(
        [FromBody] ApplyPromotionRulesDto applyDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _promotionService.ApplyPromotionRulesAsync(applyDto, cancellationToken);
            
            if (!result.Success)
            {
                return CreateValidationProblemDetails("One or more validation errors occurred while applying promotions: " + string.Join(", ", result.Messages));
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while applying promotions.", ex);
        }
    }
}