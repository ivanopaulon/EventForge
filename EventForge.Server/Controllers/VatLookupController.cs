using EventForge.DTOs.External;
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
public class VatLookupController : BaseApiController
{
    private readonly IVatLookupService _vatLookupService;
    private readonly ILogger<VatLookupController> _logger;

    public VatLookupController(
        IVatLookupService vatLookupService,
        ILogger<VatLookupController> logger)
    {
        _vatLookupService = vatLookupService;
        _logger = logger;
    }

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
            return BadRequest(new { error = "VAT number is required" });
        }

        _logger.LogInformation("VAT lookup requested for: {VatNumber}", vatNumber);

        var result = await _vatLookupService.LookupAsync(vatNumber, cancellationToken);

        if (result == null)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "VAT lookup service is temporarily unavailable" });
        }

        // Map to DTO
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
}
