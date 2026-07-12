using Prym.DTOs.Business.Fidelity;

namespace Prym.Web.Services;

public interface IFidelityService
{
    Task<IEnumerable<FidelityCardDto>> GetAllCardsAsync(CancellationToken ct = default);
    Task<IEnumerable<FidelityCardDto>> GetCardsByBusinessPartyAsync(Guid businessPartyId, CancellationToken ct = default);
    Task<FidelityCardDto?> GetCardByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default);
    Task<FidelityCardDto> CreateCardAsync(CreateFidelityCardDto dto, CancellationToken ct = default);
    Task<FidelityCardDto?> UpdateCardAsync(Guid id, UpdateFidelityCardDto dto, CancellationToken ct = default);
    Task RevokeCardAsync(Guid cardId, CancellationToken ct = default);
    Task SuspendCardAsync(Guid cardId, CancellationToken ct = default);
    Task ActivateCardAsync(Guid cardId, CancellationToken ct = default);
    Task<FidelityPointsTransactionDto?> AddPointsAsync(Guid cardId, ModifyFidelityPointsDto dto, CancellationToken ct = default);
    Task<FidelityPointsTransactionDto?> RedeemPointsAsync(Guid cardId, ModifyFidelityPointsDto dto, CancellationToken ct = default);
    Task<IEnumerable<FidelityPointsTransactionDto>> GetTransactionHistoryAsync(Guid cardId, CancellationToken ct = default);
    Task DeleteCardAsync(Guid cardId, CancellationToken ct = default);

    /// <summary>
    /// Computes a preview of the fidelity points that would be earned for a given order total and
    /// fidelity tier, using the tenant's currently effective rate (base rate * tier multiplier * any
    /// active campaign). Returns 0 on failure so the POS UI degrades gracefully.
    /// </summary>
    Task<int> CalculatePreviewAsync(decimal orderTotal, Guid tierId, CancellationToken ct = default);
}

public class FidelityService(
    IHttpClientService httpClientService,
    ILogger<FidelityService> logger) : IFidelityService
{
    private const string BaseUrl = "api/v1/fidelity-cards";

    public async Task<IEnumerable<FidelityCardDto>> GetAllCardsAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityCardDto>>(BaseUrl, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity cards");
            throw;
        }
    }

    public async Task<IEnumerable<FidelityCardDto>> GetCardsByBusinessPartyAsync(Guid businessPartyId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityCardDto>>(
                       $"{BaseUrl}?businessPartyId={businessPartyId}",
                       ct)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity cards for business party {BusinessPartyId}", businessPartyId);
            throw;
        }
    }

    public async Task<FidelityCardDto?> GetCardByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityCardDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity card {CardId}", id);
            throw;
        }
    }

    public async Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityCardDto>(
                $"{BaseUrl}/by-card-number/{Uri.EscapeDataString(cardNumber)}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nel lookup tessera per cardNumber {CardNumber}", cardNumber);
            return null;
        }
    }

    public async Task<FidelityCardDto> CreateCardAsync(CreateFidelityCardDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFidelityCardDto, FidelityCardDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create fidelity card");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fidelity card {CardNumber}", dto.CardNumber);
            throw;
        }
    }

    public async Task<FidelityCardDto?> UpdateCardAsync(Guid id, UpdateFidelityCardDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateFidelityCardDto, FidelityCardDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fidelity card {CardId}", id);
            throw;
        }
    }

    public async Task RevokeCardAsync(Guid cardId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync($"{BaseUrl}/{cardId}/revoke", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task SuspendCardAsync(Guid cardId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync($"{BaseUrl}/{cardId}/suspend", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error suspending fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task ActivateCardAsync(Guid cardId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync($"{BaseUrl}/{cardId}/activate", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task<FidelityPointsTransactionDto?> AddPointsAsync(Guid cardId, ModifyFidelityPointsDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<ModifyFidelityPointsDto, FidelityPointsTransactionDto>(
                $"{BaseUrl}/{cardId}/points/add",
                dto,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding points to fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task<FidelityPointsTransactionDto?> RedeemPointsAsync(Guid cardId, ModifyFidelityPointsDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<ModifyFidelityPointsDto, FidelityPointsTransactionDto>(
                $"{BaseUrl}/{cardId}/points/redeem",
                dto,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error redeeming points from fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task<IEnumerable<FidelityPointsTransactionDto>> GetTransactionHistoryAsync(Guid cardId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityPointsTransactionDto>>(
                       $"{BaseUrl}/{cardId}/transactions",
                       ct)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving transaction history for fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task DeleteCardAsync(Guid cardId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{cardId}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting fidelity card {CardId}", cardId);
            throw;
        }
    }

    public async Task<int> CalculatePreviewAsync(decimal orderTotal, Guid tierId, CancellationToken ct = default)
    {
        try
        {
            var query = $"api/v1/fidelity-points/base-rates/calculate-preview?orderTotal={Uri.EscapeDataString(orderTotal.ToString(System.Globalization.CultureInfo.InvariantCulture))}&tierId={tierId}";
            return await httpClientService.GetAsync<int>(query, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating fidelity points preview for order total {OrderTotal} and tier {TierId}", orderTotal, tierId);
            return 0;
        }
    }
}
