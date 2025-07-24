using EventForge.DTOs.Promotions;
using EventForge.Services.Promotions;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for promotion management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionsController(IPromotionService promotionService)
    {
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
    }

    /// <summary>
    /// Gets all promotions with optional pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PromotionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PromotionDto>>> GetPromotions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest(new { message = "Page number must be greater than 0." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "Page size must be between 1 and 100." });

        try
        {
            var result = await _promotionService.GetPromotionsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving promotions.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets active promotions.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetActivePromotions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotions = await _promotionService.GetActivePromotionsAsync(cancellationToken);
            return Ok(promotions);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving active promotions.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a promotion by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDto>> GetPromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id, cancellationToken);

            if (promotion == null)
                return NotFound(new { message = $"Promotion with ID {id} not found." });

            return Ok(promotion);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the promotion.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(
        [FromBody] CreatePromotionDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var promotion = await _promotionService.CreatePromotionAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPromotion),
                new { id = promotion.Id },
                promotion);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the promotion.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing promotion.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(
        Guid id,
        [FromBody] UpdatePromotionDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var promotion = await _promotionService.UpdatePromotionAsync(id, updateDto, currentUser, cancellationToken);

            if (promotion == null)
                return NotFound(new { message = $"Promotion with ID {id} not found." });

            return Ok(promotion);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the promotion.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a promotion (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePromotion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _promotionService.DeletePromotionAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Promotion with ID {id} not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the promotion.", error = ex.Message });
        }
    }
}