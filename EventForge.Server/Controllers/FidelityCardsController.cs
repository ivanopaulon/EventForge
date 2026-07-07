using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Controllers;

[Route("api/v1/fidelity-cards")]
[Authorize]
public class FidelityCardsController(
    IFidelityCardService fidelityCardService,
    ITenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FidelityCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityCardDto>>> GetCards(
        [FromQuery] Guid? businessPartyId = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var cards = businessPartyId.HasValue
                ? await fidelityCardService.GetCardsByBusinessPartyAsync(businessPartyId.Value, cancellationToken)
                : await fidelityCardService.GetAllCardsAsync(cancellationToken);

            return Ok(cards);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity cards.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FidelityCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityCardDto>> GetCard(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var card = await fidelityCardService.GetCardByIdAsync(id, cancellationToken);
            return card is null ? CreateNotFoundProblem($"Fidelity card {id} not found.") : Ok(card);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity card.", ex);
        }
    }

    [HttpGet("by-card-number/{cardNumber}")]
    [ProducesResponseType(typeof(FidelityCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityCardDto>> GetCardByCardNumber(
        [FromRoute] string cardNumber,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (string.IsNullOrWhiteSpace(cardNumber))
            return CreateValidationProblemDetails("cardNumber è obbligatorio.");

        try
        {
            var card = await fidelityCardService.GetCardByCardNumberAsync(cardNumber, cancellationToken);
            return card is null ? CreateNotFoundProblem($"Fidelity card '{cardNumber}' not found.") : Ok(card);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the fidelity card by card number.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FidelityCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityCardDto>> CreateCard(
        [FromBody] CreateFidelityCardDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var createdCard = await fidelityCardService.CreateCardAsync(dto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetCard), new { id = createdCard.Id }, createdCard);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the fidelity card.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FidelityCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityCardDto>> UpdateCard(
        Guid id,
        [FromBody] UpdateFidelityCardDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var updatedCard = await fidelityCardService.UpdateCardAsync(id, dto, GetCurrentUser(), cancellationToken);
            return updatedCard is null ? CreateNotFoundProblem($"Fidelity card {id} not found.") : Ok(updatedCard);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the fidelity card.", ex);
        }
    }

    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeCard(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var revoked = await fidelityCardService.RevokeCardAsync(id, GetCurrentUser(), cancellationToken);
            return revoked ? NoContent() : CreateNotFoundProblem($"Fidelity card {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while revoking the fidelity card.", ex);
        }
    }

    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SuspendCard(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var suspended = await fidelityCardService.SuspendCardAsync(id, GetCurrentUser(), cancellationToken);
            return suspended ? NoContent() : CreateNotFoundProblem($"Fidelity card {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while suspending the fidelity card.", ex);
        }
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateCard(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var activated = await fidelityCardService.ActivateCardAsync(id, GetCurrentUser(), cancellationToken);
            return activated ? NoContent() : CreateNotFoundProblem($"Fidelity card {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while activating the fidelity card.", ex);
        }
    }

    [HttpPost("{id:guid}/points/add")]
    [ProducesResponseType(typeof(FidelityPointsTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsTransactionDto>> AddPoints(
        Guid id,
        [FromBody] ModifyFidelityPointsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var transaction = await fidelityCardService.AddPointsAsync(id, dto, GetCurrentUser(), cancellationToken);
            return transaction is null ? CreateNotFoundProblem($"Fidelity card {id} not found.") : Ok(transaction);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding fidelity points.", ex);
        }
    }

    [HttpPost("{id:guid}/points/redeem")]
    [ProducesResponseType(typeof(FidelityPointsTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FidelityPointsTransactionDto>> RedeemPoints(
        Guid id,
        [FromBody] ModifyFidelityPointsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var transaction = await fidelityCardService.RedeemPointsAsync(id, dto, GetCurrentUser(), cancellationToken);
            return transaction is null ? CreateNotFoundProblem($"Fidelity card {id} not found.") : Ok(transaction);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while redeeming fidelity points.", ex);
        }
    }

    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<FidelityPointsTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FidelityPointsTransactionDto>>> GetTransactionHistory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var transactions = await fidelityCardService.GetTransactionHistoryAsync(id, cancellationToken);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving fidelity transactions.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCard(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await fidelityCardService.DeleteCardAsync(id, GetCurrentUser(), cancellationToken);
            return deleted ? NoContent() : CreateNotFoundProblem($"Fidelity card {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the fidelity card.", ex);
        }
    }
}
