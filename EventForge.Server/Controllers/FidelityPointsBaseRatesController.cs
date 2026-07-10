using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Business.Fidelity;
using FidelityCardType = EventForge.Server.Data.Entities.Business.FidelityCardType;

namespace EventForge.Server.Controllers;

[Route("api/v1/fidelity-points/base-rates")]
[Authorize]
public class FidelityPointsBaseRatesController(
    IFidelityPointsBaseRateService baseRateService,
    IFidelityPointsRateService fidelityPointsRateService,
    ITenantContext tenantContext) : BaseApiController
{
    /// <summary>
    /// Computes a preview of the fidelity points that would be earned for a given order total and
    /// card type, using the tenant's currently effective rate (base rate * tier multiplier * any
    /// active campaign). Used by the POS to show the operator a realistic points value before payment.
    /// </summary>
    [HttpGet("calculate-preview")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CalculatePointsPreview(
        [FromQuery] decimal orderTotal,
        [FromQuery] FidelityCardType cardType,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var (rate, rounding) = await fidelityPointsRateService.GetEffectiveRateAsync(cardType, cancellationToken);
            var points = FidelityPointsRounding.Apply(orderTotal * rate, rounding);
            return Ok(points);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating the fidelity points preview.", ex);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FidelityPointsBaseRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityPointsBaseRateDto>>> GetBaseRates(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var baseRates = await baseRateService.GetAllAsync(cancellationToken);
            return Ok(baseRates.Select(MapBaseRate));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity points base rates.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FidelityPointsBaseRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsBaseRateDto>> GetBaseRate(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var baseRate = await baseRateService.GetByIdAsync(id, cancellationToken);
            return baseRate is null ? CreateNotFoundProblem($"Fidelity points base rate {id} not found.") : Ok(MapBaseRate(baseRate));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity points base rate.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FidelityPointsBaseRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsBaseRateDto>> CreateBaseRate(
        [FromBody] CreateFidelityPointsBaseRateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var created = await baseRateService.CreateAsync(new FidelityPointsBaseRate
            {
                Rate = dto.Rate,
                RoundingMode = (EventForge.Server.Data.Entities.Business.FidelityPointsRoundingMode)dto.RoundingMode,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = null
            }, GetCurrentUser(), cancellationToken);

            return CreatedAtAction(nameof(GetBaseRate), new { id = created.Id }, MapBaseRate(created));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the fidelity points base rate.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FidelityPointsBaseRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsBaseRateDto>> UpdateBaseRate(
        Guid id,
        [FromBody] UpdateFidelityPointsBaseRateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var existing = await baseRateService.GetByIdAsync(id, cancellationToken);
            if (existing is null) return CreateNotFoundProblem($"Fidelity points base rate {id} not found.");

            var updated = await baseRateService.UpdateAsync(id, new FidelityPointsBaseRate
            {
                Rate = dto.Rate,
                RoundingMode = (EventForge.Server.Data.Entities.Business.FidelityPointsRoundingMode)dto.RoundingMode,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = existing.EffectiveTo
            }, GetCurrentUser(), cancellationToken);

            return updated is null ? CreateNotFoundProblem($"Fidelity points base rate {id} not found.") : Ok(MapBaseRate(updated));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the fidelity points base rate.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBaseRate(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await baseRateService.DeleteAsync(id, GetCurrentUser(), cancellationToken);
            return deleted ? NoContent() : CreateNotFoundProblem($"Fidelity points base rate {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the fidelity points base rate.", ex);
        }
    }

    private static FidelityPointsBaseRateDto MapBaseRate(FidelityPointsBaseRate baseRate) =>
        new()
        {
            Id = baseRate.Id,
            Rate = baseRate.Rate,
            RoundingMode = (Prym.DTOs.Business.Fidelity.FidelityPointsRoundingMode)baseRate.RoundingMode,
            EffectiveFrom = baseRate.EffectiveFrom,
            EffectiveTo = baseRate.EffectiveTo,
            CreatedAt = baseRate.CreatedAt
        };
}
