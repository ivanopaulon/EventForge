using Prym.DTOs.Business.Fidelity;

namespace Prym.Web.Services;

public interface IFidelityPointsBaseRateService
{
    Task<IEnumerable<FidelityPointsBaseRateDto>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityPointsBaseRateDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityPointsBaseRateDto> CreateAsync(CreateFidelityPointsBaseRateDto dto, CancellationToken ct = default);
    Task<FidelityPointsBaseRateDto?> UpdateAsync(Guid id, UpdateFidelityPointsBaseRateDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FidelityPointsBaseRateService(
    IHttpClientService httpClientService,
    ILogger<FidelityPointsBaseRateService> logger) : IFidelityPointsBaseRateService
{
    private const string BaseUrl = "api/v1/fidelity-points/base-rates";

    public async Task<IEnumerable<FidelityPointsBaseRateDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityPointsBaseRateDto>>(BaseUrl, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity points base rates");
            throw;
        }
    }

    public async Task<FidelityPointsBaseRateDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityPointsBaseRateDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity points base rate {BaseRateId}", id);
            throw;
        }
    }

    public async Task<FidelityPointsBaseRateDto> CreateAsync(CreateFidelityPointsBaseRateDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFidelityPointsBaseRateDto, FidelityPointsBaseRateDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create fidelity points base rate");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fidelity points base rate effective from {EffectiveFrom}", dto.EffectiveFrom);
            throw;
        }
    }

    public async Task<FidelityPointsBaseRateDto?> UpdateAsync(Guid id, UpdateFidelityPointsBaseRateDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateFidelityPointsBaseRateDto, FidelityPointsBaseRateDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fidelity points base rate {BaseRateId}", id);
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
            logger.LogError(ex, "Error deleting fidelity points base rate {BaseRateId}", id);
            throw;
        }
    }
}
