using EventForge.Server.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for direct VIES VAT validation using the simplified REST API.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/vat")]
public class VatValidationController : BaseApiController
{
    private readonly IViesValidationService _viesService;
    private readonly ILogger<VatValidationController> _logger;

    public VatValidationController(
        IViesValidationService viesService,
        ILogger<VatValidationController> logger)
    {
        _viesService = viesService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a VAT number using the VIES REST API.
    /// </summary>
    /// <param name="countryCode">Two-letter ISO country code (e.g., "IT", "FR")</param>
    /// <param name="vatNumber">VAT number without country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIES validation response</returns>
    [HttpGet("validate/{countryCode}/{vatNumber}")]
    public async Task<IActionResult> ValidateVat(
        string countryCode, 
        string vatNumber, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _viesService.ValidateVatAsync(countryCode, vatNumber, cancellationToken);
            
            if (result == null)
            {
                return BadRequest(new { error = "Unable to validate VAT number" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VAT {CountryCode}{VatNumber}", countryCode, vatNumber);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
