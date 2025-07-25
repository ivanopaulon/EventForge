using EventForge.DTOs.Banks;
using EventForge.Services.Banks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for bank management.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class BanksController : BaseApiController
{
    private readonly IBankService _bankService;

    public BanksController(IBankService bankService)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
    }

    /// <summary>
    /// Gets all banks with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of banks</returns>
    /// <response code="200">Returns the paginated list of banks</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BankDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BankDto>>> GetBanks(
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
            var result = await _bankService.GetBanksAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving banks.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a bank by ID.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bank information</returns>
    /// <response code="200">Returns the bank</response>
    /// <response code="404">If the bank is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDto>> GetBank(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bank = await _bankService.GetBankByIdAsync(id, cancellationToken);

            if (bank == null)
            {
                return NotFound(new { message = $"Bank with ID {id} not found." });
            }

            return Ok(bank);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the bank.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new bank.
    /// </summary>
    /// <param name="createBankDto">Bank creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bank information</returns>
    /// <response code="201">Returns the created bank</response>
    /// <response code="400">If the bank data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BankDto>> CreateBank(
        [FromBody] CreateBankDto createBankDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var bank = await _bankService.CreateBankAsync(createBankDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetBank),
                new { id = bank.Id },
                bank);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the bank.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing bank.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="updateBankDto">Bank update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bank information</returns>
    /// <response code="200">Returns the updated bank</response>
    /// <response code="400">If the bank data is invalid</response>
    /// <response code="404">If the bank is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDto>> UpdateBank(
        Guid id,
        [FromBody] UpdateBankDto updateBankDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var bank = await _bankService.UpdateBankAsync(id, updateBankDto, currentUser, cancellationToken);

            if (bank == null)
            {
                return NotFound(new { message = $"Bank with ID {id} not found." });
            }

            return Ok(bank);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the bank.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a bank (soft delete).
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the bank was successfully deleted</response>
    /// <response code="404">If the bank is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBank(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _bankService.DeleteBankAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Bank with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the bank.", error = ex.Message });
        }
    }
}