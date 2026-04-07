using Prym.Server.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Prym.Server.Controllers;

/// <summary>
/// Controller for direct VIES VAT validation using the simplified REST API.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/vat")]
public class VatValidationController(
    IViesValidationService viesService,
    ILogger<VatValidationController> logger) : BaseApiController
{
    /// <summary>
    /// Validates a VAT number using the VIES REST API.
    /// </summary>
    /// <param name="countryCode">Two-letter ISO country code (e.g., "IT", "FR")</param>
    /// <param name="vatNumber">VAT number without country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIES validation response</returns>
    /// <response code="200">Returns VIES validation result</response>
    /// <response code="400">Unable to validate VAT number</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("validate/{countryCode}/{vatNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateVat(
        [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "Country code must be 2 uppercase letters")] string countryCode,
        [RegularExpression(@"^[A-Z0-9]{1,20}$", ErrorMessage = "VAT number must contain only alphanumeric characters")] string vatNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await viesService.ValidateVatAsync(countryCode, vatNumber, cancellationToken);

            if (result is null)
                return CreateValidationProblemDetails("Unable to validate VAT number");

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating VAT {CountryCode}{VatNumber}", countryCode, vatNumber);
            return CreateInternalServerErrorProblem("Internal server error", ex);
        }
    }
}
