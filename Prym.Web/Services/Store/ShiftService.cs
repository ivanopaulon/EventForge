using Prym.DTOs.Store;
using System.Net;
using System.Net.Http.Json;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service implementation for cashier shift management.
/// </summary>
public class ShiftService(
    HttpClient httpClient,
    ILogger<ShiftService> logger) : IShiftService
{
    private const string ApiBase = "api/v1/shifts";

    public async Task<List<CashierShiftDto>> GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
            return await httpClient.GetFromJsonAsync<List<CashierShiftDto>>(url, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting shifts from {From} to {To}", from, to);
            throw;
        }
    }

    public async Task<List<CashierShiftDto>> GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/operator/{storeUserId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
            return await httpClient.GetFromJsonAsync<List<CashierShiftDto>>(url, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting shifts for operator {OperatorId}", storeUserId);
            throw;
        }
    }

    public async Task<CashierShiftDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<CashierShiftDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting shift {Id}", id);
            throw;
        }
    }

    public async Task<CashierShiftDto?> CreateAsync(CreateCashierShiftDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, dto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await StoreServiceHelper.GetErrorMessageAsync(response, "turno", logger);
                throw new InvalidOperationException(error);
            }

            return await response.Content.ReadFromJsonAsync<CashierShiftDto>(cancellationToken: ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating shift");
            throw;
        }
    }

    public async Task<CashierShiftDto?> UpdateAsync(Guid id, UpdateCashierShiftDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", dto, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var error = await StoreServiceHelper.GetErrorMessageAsync(response, "turno", logger);
                throw new InvalidOperationException(error);
            }

            return await response.Content.ReadFromJsonAsync<CashierShiftDto>(cancellationToken: ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating shift {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting shift {Id}", id);
            throw;
        }
    }
}
