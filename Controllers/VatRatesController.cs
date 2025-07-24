using EventForge.DTOs.VatRates;
using EventForge.Services.VatRates;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for VAT rate management.
/// </summary>
[Route("api/v1/[controller]")]
public class VatRatesController : BaseApiController
{
    private readonly IVatRateService _vatRateService;

    public VatRatesController(IVatRateService vatRateService)
    {
        _vatRateService = vatRateService ?? throw new ArgumentNullException(nameof(vatRateService));
    }

    /// <summary>
    /// Gets all VAT rates with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of VAT rates</returns>
    /// <response code="200">Returns the paginated list of VAT rates</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VatRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<VatRateDto>>> GetVatRates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new { message = "Page number must be greater than 0." });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Page size must be between 1 and 100." });
        }

        try
        {
            var result = await _vatRateService.GetVatRatesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving VAT rates.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a VAT rate by ID.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT rate information</returns>
    /// <response code="200">Returns the VAT rate</response>
    /// <response code="404">If the VAT rate is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VatRateDto>> GetVatRate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vatRate = await _vatRateService.GetVatRateByIdAsync(id, cancellationToken);

            if (vatRate == null)
            {
                return NotFound(new { message = $"VAT rate with ID {id} not found." });
            }

            return Ok(vatRate);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the VAT rate.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new VAT rate.
    /// </summary>
    /// <param name="createVatRateDto">VAT rate creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VAT rate information</returns>
    /// <response code="201">Returns the created VAT rate</response>
    /// <response code="400">If the VAT rate data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VatRateDto>> CreateVatRate(
        [FromBody] CreateVatRateDto createVatRateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var vatRate = await _vatRateService.CreateVatRateAsync(createVatRateDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetVatRate),
                new { id = vatRate.Id },
                vatRate);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the VAT rate.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing VAT rate.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="updateVatRateDto">VAT rate update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VAT rate information</returns>
    /// <response code="200">Returns the updated VAT rate</response>
    /// <response code="400">If the VAT rate data is invalid</response>
    /// <response code="404">If the VAT rate is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VatRateDto>> UpdateVatRate(
        Guid id,
        [FromBody] UpdateVatRateDto updateVatRateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var vatRate = await _vatRateService.UpdateVatRateAsync(id, updateVatRateDto, currentUser, cancellationToken);

            if (vatRate == null)
            {
                return NotFound(new { message = $"VAT rate with ID {id} not found." });
            }

            return Ok(vatRate);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the VAT rate.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a VAT rate (soft delete).
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the VAT rate was successfully deleted</response>
    /// <response code="404">If the VAT rate is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVatRate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _vatRateService.DeleteVatRateAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"VAT rate with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the VAT rate.", error = ex.Message });
        }
    }
}