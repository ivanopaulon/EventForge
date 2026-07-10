using Prym.DTOs.Business.Fidelity;

namespace Prym.Web.Services;

public interface IFidelityTierMultiplierService
{
    Task<IEnumerable<FidelityTierMultiplierDto>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityTierMultiplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityTierMultiplierDto> CreateAsync(CreateFidelityTierMultiplierDto dto, CancellationToken ct = default);
    Task<FidelityTierMultiplierDto?> UpdateAsync(Guid id, UpdateFidelityTierMultiplierDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FidelityTierMultiplierService(
    IHttpClientService httpClientService,
    ILogger<FidelityTierMultiplierService> logger) : IFidelityTierMultiplierService
{
    private const string BaseUrl = "api/v1/fidelity-points/tier-multipliers";

    public async Task<IEnumerable<FidelityTierMultiplierDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityTierMultiplierDto>>(BaseUrl, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity tier multipliers");
            throw;
        }
    }

    public async Task<FidelityTierMultiplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityTierMultiplierDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity tier multiplier {TierMultiplierId}", id);
            throw;
        }
    }

    public async Task<FidelityTierMultiplierDto> CreateAsync(CreateFidelityTierMultiplierDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFidelityTierMultiplierDto, FidelityTierMultiplierDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create fidelity tier multiplier");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fidelity tier multiplier for card type {CardType}", dto.CardType);
            throw;
        }
    }

    public async Task<FidelityTierMultiplierDto?> UpdateAsync(Guid id, UpdateFidelityTierMultiplierDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateFidelityTierMultiplierDto, FidelityTierMultiplierDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fidelity tier multiplier {TierMultiplierId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting fidelity tier multiplier {TierMultiplierId}", id);
            throw;
        }
    }
}
