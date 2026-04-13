using Prym.DTOs.External;
using EventForge.Server.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for VAT number lookup using VIES service.
/// </summary>
[ApiController]
[Route("api/v1/vat-lookup")]
[Authorize]
public class VatLookupController(
    IVatLookupService vatLookupService,
    ILogger<VatLookupController> logger) : BaseApiController
{

    /// <summary>
    /// Looks up a VAT number and returns validation result with company information.
    /// </summary>
    /// <param name="vatNumber">VAT number to lookup (with or without country code)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT lookup result</returns>
    [HttpGet("{vatNumber}")]
    [ProducesResponseType(typeof(VatLookupResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VatLookupResultDto>> Lookup(
        string vatNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
        {
            return CreateValidationProblemDetails("VAT number is required.");
        }

        logger.LogInformation("VAT lookup requested for: {VatNumber}", vatNumber);

        try
        {
            var result = await vatLookupService.LookupAsync(vatNumber, cancellationToken);

            if (result is null)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                    Title = "Service Unavailable",
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Detail = "VAT lookup service is temporarily unavailable.",
                    Instance = HttpContext.Request.Path
                };

                if (HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                {
                    problemDetails.Extensions["correlationId"] = correlationId;
                }

                problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                return StatusCode(StatusCodes.Status503ServiceUnavailable, problemDetails);
            }

            var dto = new VatLookupResultDto
            {
                IsValid = result.IsValid,
                CountryCode = result.CountryCode,
                VatNumber = result.VatNumber,
                Name = result.Name,
                Address = result.Address,
                Street = result.ParsedAddress?.Street,
                City = result.ParsedAddress?.City,
                PostalCode = result.ParsedAddress?.PostalCode,
                Province = result.ParsedAddress?.Province,
                ErrorMessage = result.ErrorMessage
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Lookup for VAT number {VatNumber}", vatNumber);
            return CreateInternalServerErrorProblem("Errore interno del server.", ex);
        }
    }
}
