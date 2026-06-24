using Prym.DTOs.Common;

namespace Prym.Web.Services;

public interface IClassificationNodeService
{
    Task<IEnumerable<ClassificationNodeDto>> GetAllAsync(ClassificationApplicableTo? applicableTo = null, CancellationToken ct = default);
}

public class ClassificationNodeService(
    IHttpClientService httpClientService,
    ILogger<ClassificationNodeService> logger) : IClassificationNodeService
{
    private const string BaseUrl = "api/v1/entities/classification-nodes";

    public async Task<IEnumerable<ClassificationNodeDto>> GetAllAsync(ClassificationApplicableTo? applicableTo = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}?page=1&pageSize=1000";
            if (applicableTo.HasValue)
                url += $"&applicableTo={(int)applicableTo.Value}";

            var result = await httpClientService.GetAsync<PagedResult<ClassificationNodeDto>>(url, ct);
            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving classification nodes");
            throw;
        }
    }
}
