using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Controllers;

[Route("api/v1/fidelity-points/tier-multipliers")]
[Authorize]
public class FidelityTierMultipliersController(
    IFidelityTierMultiplierService tierMultiplierService,
    ITenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FidelityTierMultiplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityTierMultiplierDto>>> GetTierMultipliers(
        [FromQuery] Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var multipliers = await tierMultiplierService.GetByCampaignAsync(campaignId, cancellationToken);
            return Ok(multipliers.Select(MapTierMultiplier));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity tier multipliers.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FidelityTierMultiplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierMultiplierDto>> GetTierMultiplier(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var multiplier = await tierMultiplierService.GetByIdAsync(id, cancellationToken);
            return multiplier is null ? CreateNotFoundProblem($"Fidelity tier multiplier {id} not found.") : Ok(MapTierMultiplier(multiplier));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity tier multiplier.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FidelityTierMultiplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierMultiplierDto>> CreateTierMultiplier(
        [FromBody] CreateFidelityTierMultiplierDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var created = await tierMultiplierService.CreateAsync(new FidelityTierMultiplier
            {
                CampaignId = dto.CampaignId,
                TierId = dto.TierId,
                Multiplier = dto.Multiplier
            }, GetCurrentUser(), cancellationToken);

            return CreatedAtAction(nameof(GetTierMultiplier), new { id = created.Id }, MapTierMultiplier(created));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the fidelity tier multiplier.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FidelityTierMultiplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierMultiplierDto>> UpdateTierMultiplier(
        Guid id,
        [FromBody] UpdateFidelityTierMultiplierDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var updated = await tierMultiplierService.UpdateAsync(id, new FidelityTierMultiplier
            {
                TierId = dto.TierId,
                Multiplier = dto.Multiplier
            }, GetCurrentUser(), cancellationToken);

            return updated is null ? CreateNotFoundProblem($"Fidelity tier multiplier {id} not found.") : Ok(MapTierMultiplier(updated));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the fidelity tier multiplier.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTierMultiplier(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await tierMultiplierService.DeleteAsync(id, GetCurrentUser(), cancellationToken);
            return deleted ? NoContent() : CreateNotFoundProblem($"Fidelity tier multiplier {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the fidelity tier multiplier.", ex);
        }
    }

    private static FidelityTierMultiplierDto MapTierMultiplier(FidelityTierMultiplier multiplier) =>
        new()
        {
            Id = multiplier.Id,
            CampaignId = multiplier.CampaignId,
            TierId = multiplier.TierId,
            TierName = multiplier.Tier?.Name,
            Multiplier = multiplier.Multiplier,
            CreatedAt = multiplier.CreatedAt
        };
}
