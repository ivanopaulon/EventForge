using EventForge.DTOs.Business;
using EventForge.Services.Business;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for payment term management.
/// </summary>
[Route("api/v1/[controller]")]
public class PaymentTermsController : BaseApiController
{
    private readonly IPaymentTermService _paymentTermService;

    public PaymentTermsController(IPaymentTermService paymentTermService)
    {
        _paymentTermService = paymentTermService ?? throw new ArgumentNullException(nameof(paymentTermService));
    }

    /// <summary>
    /// Gets all payment terms with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of payment terms</returns>
    /// <response code="200">Returns the paginated list of payment terms</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentTermDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PaymentTermDto>>> GetPaymentTerms(
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
            var result = await _paymentTermService.GetPaymentTermsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving payment terms.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a payment term by ID.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment term information</returns>
    /// <response code="200">Returns the payment term</response>
    /// <response code="404">If the payment term is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentTermDto>> GetPaymentTerm(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentTerm = await _paymentTermService.GetPaymentTermByIdAsync(id, cancellationToken);

            if (paymentTerm == null)
            {
                return NotFound(new { message = $"Payment term with ID {id} not found." });
            }

            return Ok(paymentTerm);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the payment term.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new payment term.
    /// </summary>
    /// <param name="createPaymentTermDto">Payment term creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment term</returns>
    /// <response code="201">Returns the newly created payment term</response>
    /// <response code="400">If the payment term data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentTermDto>> CreatePaymentTerm(
        [FromBody] CreatePaymentTermDto createPaymentTermDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var paymentTerm = await _paymentTermService.CreatePaymentTermAsync(createPaymentTermDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetPaymentTerm), new { id = paymentTerm.Id }, paymentTerm);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the payment term.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing payment term.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="updatePaymentTermDto">Payment term update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment term</returns>
    /// <response code="200">Returns the updated payment term</response>
    /// <response code="400">If the payment term data is invalid</response>
    /// <response code="404">If the payment term is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentTermDto>> UpdatePaymentTerm(
        Guid id,
        [FromBody] UpdatePaymentTermDto updatePaymentTermDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var paymentTerm = await _paymentTermService.UpdatePaymentTermAsync(id, updatePaymentTermDto, currentUser, cancellationToken);

            if (paymentTerm == null)
            {
                return NotFound(new { message = $"Payment term with ID {id} not found." });
            }

            return Ok(paymentTerm);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the payment term.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a payment term (soft delete).
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Payment term deleted successfully</response>
    /// <response code="404">If the payment term is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePaymentTerm(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _paymentTermService.DeletePaymentTermAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Payment term with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the payment term.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current user from the request context.
    /// In production, this would extract from authentication context.
    /// </summary>
    /// <returns>Current user identifier</returns>
}