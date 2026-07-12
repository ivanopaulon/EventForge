using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Controllers;

[Route("api/v1/business/fidelity-tiers")]
[Authorize]
public class FidelityTiersController(
    IFidelityTierService tierService,
    ITenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FidelityTierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityTierDto>>> GetTiers(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var tiers = await tierService.GetAllAsync(cancellationToken);
            return Ok(tiers.Select(MapTier));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity tiers.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FidelityTierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierDto>> GetTier(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var tier = await tierService.GetByIdAsync(id, cancellationToken);
            return tier is null ? CreateNotFoundProblem($"Fidelity tier {id} not found.") : Ok(MapTier(tier));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity tier.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FidelityTierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierDto>> CreateTier(
        [FromBody] CreateFidelityTierDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var created = await tierService.CreateAsync(new FidelityTier
            {
                Name = dto.Name,
                SortOrder = dto.SortOrder,
                Color = dto.Color,
                Icon = dto.Icon,
                IsActive = dto.IsActive
            }, dto.MinimumSpendThreshold, dto.EvaluationPeriodMonths, GetCurrentUser(), cancellationToken);

            return CreatedAtAction(nameof(GetTier), new { id = created.Id }, MapTier(created));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the fidelity tier.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FidelityTierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityTierDto>> UpdateTier(
        Guid id,
        [FromBody] UpdateFidelityTierDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var updated = await tierService.UpdateAsync(id, new FidelityTier
            {
                Name = dto.Name,
                SortOrder = dto.SortOrder,
                Color = dto.Color,
                Icon = dto.Icon,
                IsActive = dto.IsActive
            }, dto.MinimumSpendThreshold, dto.EvaluationPeriodMonths, GetCurrentUser(), cancellationToken);

            return updated is null ? CreateNotFoundProblem($"Fidelity tier {id} not found.") : Ok(MapTier(updated));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the fidelity tier.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTier(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await tierService.DeleteAsync(id, GetCurrentUser(), cancellationToken);
            return deleted ? NoContent() : CreateNotFoundProblem($"Fidelity tier {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the fidelity tier.", ex);
        }
    }

    private static FidelityTierDto MapTier(FidelityTier tier) =>
        new()
        {
            Id = tier.Id,
            Name = tier.Name,
            SortOrder = tier.SortOrder,
            Color = tier.Color,
            Icon = tier.Icon,
            IsActive = tier.IsActive,
            MinimumSpendThreshold = tier.Rule?.MinimumSpendThreshold,
            EvaluationPeriodMonths = tier.Rule?.EvaluationPeriodMonths ?? 12,
            CreatedAt = tier.CreatedAt
        };
}
