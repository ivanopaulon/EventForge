using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace Prym.Web.Services;

/// <summary>
/// Client service implementation for consuming the Bold Reports report definitions API.
/// </summary>
public class ReportDefinitionService(
    IHttpClientService httpClientService,
    ILogger<ReportDefinitionService> logger) : IReportDefinitionService
{
    private const string BaseUrl = "api/v1/reports";

    /// <inheritdoc/>
    public async Task<PagedResult<ReportListItemDto>?> GetReportsAsync(
        string? category   = null,
        string? searchTerm = null,
        int     page       = 1,
        int     pageSize   = 25,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
            };
            if (!string.IsNullOrWhiteSpace(category))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");

            var url = $"{BaseUrl}?{string.Join("&", queryParams)}";
            return await httpClientService.GetAsync<PagedResult<ReportListItemDto>>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reports list");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<ReportDefinitionDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving report {ReportId}", id);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>?> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IReadOnlyList<string>>($"{BaseUrl}/categories", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving report categories");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto?> CreateReportAsync(CreateReportDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateReportDto, ReportDefinitionDto>(BaseUrl, dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating report");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto?> UpdateReportAsync(Guid id, UpdateReportDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateReportDto, ReportDefinitionDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating report {ReportId}", id);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteReportAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting report {ReportId}", id);
        }
    }
}
