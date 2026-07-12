using Prym.DTOs.Business.Fidelity;

namespace Prym.Web.Services;

public interface IFidelityTierService
{
    Task<IEnumerable<FidelityTierDto>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityTierDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityTierDto> CreateAsync(CreateFidelityTierDto dto, CancellationToken ct = default);
    Task<FidelityTierDto?> UpdateAsync(Guid id, UpdateFidelityTierDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FidelityTierService(
    IHttpClientService httpClientService,
    ILogger<FidelityTierService> logger) : IFidelityTierService
{
    private const string BaseUrl = "api/v1/business/fidelity-tiers";

    public async Task<IEnumerable<FidelityTierDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityTierDto>>(BaseUrl, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity tiers");
            throw;
        }
    }

    public async Task<FidelityTierDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityTierDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity tier {TierId}", id);
            throw;
        }
    }

    public async Task<FidelityTierDto> CreateAsync(CreateFidelityTierDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFidelityTierDto, FidelityTierDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create fidelity tier");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fidelity tier {Name}", dto.Name);
            throw;
        }
    }

    public async Task<FidelityTierDto?> UpdateAsync(Guid id, UpdateFidelityTierDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateFidelityTierDto, FidelityTierDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fidelity tier {TierId}", id);
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
            logger.LogError(ex, "Error deleting fidelity tier {TierId}", id);
            throw;
        }
    }
}
