using EventForge.DTOs.Sales;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for table management.
/// </summary>
public class TableManagementService : ITableManagementService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TableManagementService> _logger;
    private const string BaseUrl = "api/v1/tables";

    public TableManagementService(IHttpClientFactory httpClientFactory, ILogger<TableManagementService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TableSessionDto>?> GetAllTablesAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<TableSessionDto>>(BaseUrl, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> GetTableAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve table {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table {Id}", id);
            return null;
        }
    }

    public async Task<List<TableSessionDto>?> GetAvailableTablesAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<TableSessionDto>>($"{BaseUrl}/available", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> CreateTableAsync(CreateTableSessionDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create table. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating table");
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableAsync(Guid id, UpdateTableSessionDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update table {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table {Id}", id);
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableStatusAsync(Guid id, UpdateTableStatusDto statusDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}/status", statusDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update table status {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table status {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteTableAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting table {Id}", id);
            return false;
        }
    }

    public async Task<List<TableReservationDto>?> GetReservationsByDateAsync(DateTime date)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            return await httpClient.GetFromJsonAsync<List<TableReservationDto>>($"{BaseUrl}/reservations?date={dateStr}", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for date {Date}", date);
            return null;
        }
    }

    public async Task<TableReservationDto?> GetReservationAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/reservations/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve reservation {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> CreateReservationAsync(CreateTableReservationDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/reservations", createDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create reservation. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation");
            return null;
        }
    }

    public async Task<TableReservationDto?> UpdateReservationAsync(Guid id, UpdateTableReservationDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/reservations/{id}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update reservation {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> ConfirmReservationAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsync($"{BaseUrl}/reservations/{id}/confirm", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to confirm reservation {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> MarkArrivedAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsync($"{BaseUrl}/reservations/{id}/arrived", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to mark reservation arrived {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation arrived {Id}", id);
            return null;
        }
    }

    public async Task<bool> CancelReservationAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/reservations/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {Id}", id);
            return false;
        }
    }

    public async Task<TableReservationDto?> MarkNoShowAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsync($"{BaseUrl}/reservations/{id}/no-show", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableReservationDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to mark reservation no-show {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation no-show {Id}", id);
            return null;
        }
    }
}
