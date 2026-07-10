using Prym.DTOs.Business.Fidelity;

namespace Prym.Web.Services;

public interface IFidelityPointsCampaignService
{
    Task<IEnumerable<FidelityPointsCampaignDto>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityPointsCampaignDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityPointsCampaignDto> CreateAsync(CreateFidelityPointsCampaignDto dto, CancellationToken ct = default);
    Task<FidelityPointsCampaignDto?> UpdateAsync(Guid id, UpdateFidelityPointsCampaignDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FidelityPointsCampaignService(
    IHttpClientService httpClientService,
    ILogger<FidelityPointsCampaignService> logger) : IFidelityPointsCampaignService
{
    private const string BaseUrl = "api/v1/fidelity-points/campaigns";

    public async Task<IEnumerable<FidelityPointsCampaignDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FidelityPointsCampaignDto>>(BaseUrl, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity points campaigns");
            throw;
        }
    }

    public async Task<FidelityPointsCampaignDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<FidelityPointsCampaignDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving fidelity points campaign {CampaignId}", id);
            throw;
        }
    }

    public async Task<FidelityPointsCampaignDto> CreateAsync(CreateFidelityPointsCampaignDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFidelityPointsCampaignDto, FidelityPointsCampaignDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create fidelity points campaign");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fidelity points campaign {CampaignName}", dto.Name);
            throw;
        }
    }

    public async Task<FidelityPointsCampaignDto?> UpdateAsync(Guid id, UpdateFidelityPointsCampaignDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateFidelityPointsCampaignDto, FidelityPointsCampaignDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fidelity points campaign {CampaignId}", id);
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
            logger.LogError(ex, "Error deleting fidelity points campaign {CampaignId}", id);
            throw;
        }
    }
}
