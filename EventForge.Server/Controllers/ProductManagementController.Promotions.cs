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
    /// Duplicates an existing promotion, optionally copying its rules.
    /// </summary>
    /// <param name="id">Source promotion ID</param>
    /// <param name="dto">Duplication options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created promotion and the number of rules copied</returns>
    /// <response code="201">Promotion duplicated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the source promotion is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("promotions/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(DuplicatePromotionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DuplicatePromotion(
        Guid id,
        [FromBody] DuplicatePromotionDto dto,
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
            var result = await promotionService.DuplicatePromotionAsync(id, dto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPromotion), new { id = result.NewPromotion.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while duplicating the promotion.", ex);
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

}
