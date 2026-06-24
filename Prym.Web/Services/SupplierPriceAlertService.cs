using Prym.DTOs.Alerts;
using System.Globalization;
using System.Text;

namespace Prym.Web.Services;

public interface ISupplierPriceAlertService
{
    Task<PaginatedResult<SupplierPriceAlertDto>?> GetAlertsAsync(AlertFilterRequest filter, CancellationToken ct = default);
    Task<SupplierPriceAlertDto?> GetAlertAsync(Guid id, CancellationToken ct = default);
    Task<AlertStatistics?> GetStatisticsAsync(CancellationToken ct = default);
    Task<bool> AcknowledgeAlertAsync(Guid id, CancellationToken ct = default);
    Task<bool> ResolveAlertAsync(Guid id, string? notes, CancellationToken ct = default);
    Task<bool> DismissAlertAsync(Guid id, CancellationToken ct = default);
}

public class SupplierPriceAlertService(
    IHttpClientService httpClientService,
    ILogger<SupplierPriceAlertService> logger) : ISupplierPriceAlertService
{
    private const string BaseUrl = "api/v1/alerts";

    public async Task<PaginatedResult<SupplierPriceAlertDto>?> GetAlertsAsync(AlertFilterRequest filter, CancellationToken ct = default)
    {
        try
        {
            var endpoint = BuildAlertsUrl(filter);
            return await httpClientService.GetAsync<PaginatedResult<SupplierPriceAlertDto>>(endpoint, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving supplier price alerts");
            throw;
        }
    }

    public async Task<SupplierPriceAlertDto?> GetAlertAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<SupplierPriceAlertDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving supplier price alert {AlertId}", id);
            throw;
        }
    }

    public async Task<AlertStatistics?> GetStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<AlertStatistics>($"{BaseUrl}/statistics", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving supplier price alert statistics");
            throw;
        }
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<object, Dictionary<string, object>>(
                $"{BaseUrl}/{id}/acknowledge",
                new { },
                ct);

            return result is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error acknowledging supplier price alert {AlertId}", id);
            throw;
        }
    }

    public async Task<bool> ResolveAlertAsync(Guid id, string? notes, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<object, Dictionary<string, object>>(
                $"{BaseUrl}/{id}/resolve",
                new { Notes = notes },
                ct);

            return result is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving supplier price alert {AlertId}", id);
            throw;
        }
    }

    public async Task<bool> DismissAlertAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<object, Dictionary<string, object>>(
                $"{BaseUrl}/{id}/dismiss",
                new { },
                ct);

            return result is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dismissing supplier price alert {AlertId}", id);
            throw;
        }
    }

    private static string BuildAlertsUrl(AlertFilterRequest filter)
    {
        var query = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query.Add($"status={Uri.EscapeDataString(filter.Status)}");

        if (!string.IsNullOrWhiteSpace(filter.Severity))
            query.Add($"severity={Uri.EscapeDataString(filter.Severity)}");

        if (!string.IsNullOrWhiteSpace(filter.AlertType))
            query.Add($"alertType={Uri.EscapeDataString(filter.AlertType)}");

        if (filter.ProductId.HasValue)
            query.Add($"productId={filter.ProductId.Value}");

        if (filter.SupplierId.HasValue)
            query.Add($"supplierId={filter.SupplierId.Value}");

        if (filter.FromDate.HasValue)
            query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O", CultureInfo.InvariantCulture))}");

        if (filter.ToDate.HasValue)
            query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O", CultureInfo.InvariantCulture))}");

        if (!string.IsNullOrWhiteSpace(filter.SortBy))
            query.Add($"sortBy={Uri.EscapeDataString(filter.SortBy)}");

        if (!string.IsNullOrWhiteSpace(filter.SortOrder))
            query.Add($"sortOrder={Uri.EscapeDataString(filter.SortOrder)}");

        var urlBuilder = new StringBuilder(BaseUrl);
        if (query.Count > 0)
        {
            urlBuilder.Append('?');
            urlBuilder.Append(string.Join("&", query));
        }

        return urlBuilder.ToString();
    }
}
