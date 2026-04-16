using Prym.DTOs.Common;
using Prym.DTOs.Store;
using System.Net;
using System.Net.Http.Json;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service implementation for managing fiscal drawers.
/// </summary>
public class FiscalDrawerService(
    HttpClient httpClient,
    ILogger<FiscalDrawerService> logger) : IFiscalDrawerService
{
    private const string ApiBase = "api/v1/fiscal-drawers";

    public async Task<PagedResult<FiscalDrawerDto>?> GetPagedAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            return await httpClient.GetFromJsonAsync<PagedResult<FiscalDrawerDto>>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting fiscal drawers paged");
            return null;
        }
    }

    public async Task<List<FiscalDrawerDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<PagedResult<FiscalDrawerDto>>($"{ApiBase}?page=1&pageSize=1000", ct);
            return result?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting all fiscal drawers");
            return [];
        }
    }

    public async Task<FiscalDrawerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<FiscalDrawerDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting fiscal drawer {Id}", id);
            return null;
        }
    }

    public async Task<FiscalDrawerDto?> GetByPosIdAsync(Guid posId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<FiscalDrawerDto>($"{ApiBase}/by-pos/{posId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting fiscal drawer for POS {PosId}", posId);
            return null;
        }
    }

    public async Task<FiscalDrawerDto?> GetByOperatorIdAsync(Guid operatorId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<FiscalDrawerDto>($"{ApiBase}/by-operator/{operatorId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting fiscal drawer for operator {OperatorId}", operatorId);
            return null;
        }
    }

    public async Task<FiscalDrawerDto?> CreateAsync(CreateFiscalDrawerDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<FiscalDrawerDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating fiscal drawer");
            return null;
        }
    }

    public async Task<FiscalDrawerDto?> UpdateAsync(Guid id, UpdateFiscalDrawerDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<FiscalDrawerDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating fiscal drawer {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting fiscal drawer {Id}", id);
            return false;
        }
    }

    public async Task<FiscalDrawerSessionDto?> GetCurrentSessionAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<FiscalDrawerSessionDto>($"{ApiBase}/{fiscalDrawerId}/current-session", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting current session for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<PagedResult<FiscalDrawerSessionDto>?> GetSessionsAsync(Guid fiscalDrawerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PagedResult<FiscalDrawerSessionDto>>($"{ApiBase}/{fiscalDrawerId}/sessions?page={page}&pageSize={pageSize}", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting sessions for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<FiscalDrawerSessionDto?> OpenSessionAsync(Guid fiscalDrawerId, OpenFiscalDrawerSessionDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{fiscalDrawerId}/open-session", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<FiscalDrawerSessionDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening session for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<FiscalDrawerSessionDto?> CloseSessionAsync(Guid fiscalDrawerId, CloseFiscalDrawerSessionDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{fiscalDrawerId}/close-session", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<FiscalDrawerSessionDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing session for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<PagedResult<FiscalDrawerTransactionDto>?> GetTransactionsAsync(Guid fiscalDrawerId, Guid? sessionId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/{fiscalDrawerId}/transactions?page={page}&pageSize={pageSize}";
            if (sessionId.HasValue) url += $"&sessionId={sessionId.Value}";
            return await httpClient.GetFromJsonAsync<PagedResult<FiscalDrawerTransactionDto>>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting transactions for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<FiscalDrawerTransactionDto?> CreateTransactionAsync(Guid fiscalDrawerId, CreateFiscalDrawerTransactionDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{fiscalDrawerId}/transactions", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<FiscalDrawerTransactionDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating transaction for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<List<CashDenominationDto>> GetDenominationsAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<CashDenominationDto>>($"{ApiBase}/{fiscalDrawerId}/denominations", ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting denominations for drawer {DrawerId}", fiscalDrawerId);
            return [];
        }
    }

    public async Task<List<CashDenominationDto>> InitializeDenominationsAsync(Guid fiscalDrawerId, string currencyCode = "EUR", CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"{ApiBase}/{fiscalDrawerId}/denominations/initialize?currencyCode={currencyCode}",
                new StringContent(string.Empty),
                ct);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<CashDenominationDto>>() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing denominations for drawer {DrawerId}", fiscalDrawerId);
            return [];
        }
    }

    public async Task<CashDenominationDto?> UpdateDenominationAsync(Guid denominationId, UpdateCashDenominationDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/denominations/{denominationId}", dto, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CashDenominationDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating denomination {Id}", denominationId);
            return null;
        }
    }

    public async Task<CalculateChangeResponseDto?> CalculateChangeAsync(Guid fiscalDrawerId, CalculateChangeRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{fiscalDrawerId}/calculate-change", request, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CalculateChangeResponseDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating change for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<FiscalDrawerSummaryDto?> GetSummaryAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<FiscalDrawerSummaryDto>($"{ApiBase}/{fiscalDrawerId}/summary", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting summary for drawer {DrawerId}", fiscalDrawerId);
            return null;
        }
    }

    public async Task<SalesDashboardDto?> GetSalesDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<SalesDashboardDto>($"{ApiBase}/sales-dashboard", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting sales dashboard");
            return null;
        }
    }
}
