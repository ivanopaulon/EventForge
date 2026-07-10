using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Controllers;

[Route("api/v1/fidelity-points/campaigns")]
[Authorize]
public class FidelityPointsCampaignsController(
    IFidelityPointsCampaignService campaignService,
    ITenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FidelityPointsCampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityPointsCampaignDto>>> GetCampaigns(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var campaigns = await campaignService.GetAllAsync(cancellationToken);
            return Ok(campaigns.Select(MapCampaign));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity points campaigns.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FidelityPointsCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsCampaignDto>> GetCampaign(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var campaign = await campaignService.GetByIdAsync(id, cancellationToken);
            return campaign is null ? CreateNotFoundProblem($"Fidelity points campaign {id} not found.") : Ok(MapCampaign(campaign));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity points campaign.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FidelityPointsCampaignDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsCampaignDto>> CreateCampaign(
        [FromBody] CreateFidelityPointsCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var created = await campaignService.CreateAsync(new FidelityPointsCampaign
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Multiplier = dto.Multiplier,
                RoundingMode = (EventForge.Server.Data.Entities.Business.FidelityPointsRoundingMode)dto.RoundingMode,
                IgnoreTierMultiplier = dto.IgnoreTierMultiplier,
                IsActive = dto.IsActive,
                ProductIdsJSON = dto.ProductIdsJSON,
                CategoryIdsJSON = dto.CategoryIdsJSON
            }, GetCurrentUser(), cancellationToken);

            return CreatedAtAction(nameof(GetCampaign), new { id = created.Id }, MapCampaign(created));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the fidelity points campaign.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FidelityPointsCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsCampaignDto>> UpdateCampaign(
        Guid id,
        [FromBody] UpdateFidelityPointsCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var updated = await campaignService.UpdateAsync(id, new FidelityPointsCampaign
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Multiplier = dto.Multiplier,
                RoundingMode = (EventForge.Server.Data.Entities.Business.FidelityPointsRoundingMode)dto.RoundingMode,
                IgnoreTierMultiplier = dto.IgnoreTierMultiplier,
                IsActive = dto.IsActive,
                ProductIdsJSON = dto.ProductIdsJSON,
                CategoryIdsJSON = dto.CategoryIdsJSON
            }, GetCurrentUser(), cancellationToken);

            return updated is null ? CreateNotFoundProblem($"Fidelity points campaign {id} not found.") : Ok(MapCampaign(updated));
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the fidelity points campaign.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCampaign(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await campaignService.DeleteAsync(id, GetCurrentUser(), cancellationToken);
            return deleted ? NoContent() : CreateNotFoundProblem($"Fidelity points campaign {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the fidelity points campaign.", ex);
        }
    }

    private static FidelityPointsCampaignDto MapCampaign(FidelityPointsCampaign campaign) =>
        new()
        {
            Id = campaign.Id,
            Name = campaign.Name,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Multiplier = campaign.Multiplier,
            RoundingMode = (Prym.DTOs.Business.Fidelity.FidelityPointsRoundingMode)campaign.RoundingMode,
            IgnoreTierMultiplier = campaign.IgnoreTierMultiplier,
            IsActive = campaign.IsActive,
            ProductIdsJSON = campaign.ProductIdsJSON,
            CategoryIdsJSON = campaign.CategoryIdsJSON,
            CreatedAt = campaign.CreatedAt
        };
}
