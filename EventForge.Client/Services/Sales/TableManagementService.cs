using EventForge.DTOs.Sales;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for table management.
/// </summary>
public class TableManagementService : ITableManagementService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<TableManagementService> _logger;
    private const string BaseUrl = "api/v1/tables";

    public TableManagementService(IHttpClientService httpClientService, ILogger<TableManagementService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TableSessionDto>?> GetAllTablesAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<TableSessionDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> GetTableAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<TableSessionDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table {Id}", id);
            return null;
        }
    }

    public async Task<List<TableSessionDto>?> GetAvailableTablesAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<TableSessionDto>>($"{BaseUrl}/available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> CreateTableAsync(CreateTableSessionDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateTableSessionDto, TableSessionDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating table");
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableAsync(Guid id, UpdateTableSessionDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateTableSessionDto, TableSessionDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table {Id}", id);
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableStatusAsync(Guid id, UpdateTableStatusDto statusDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateTableStatusDto, TableSessionDto>($"{BaseUrl}/{id}/status", statusDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table status {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteTableAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting table {Id}", id);
            return false;
        }
    }

    public async Task<List<TableReservationDto>?> GetReservationsByDateAsync(DateTime date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            return await _httpClientService.GetAsync<List<TableReservationDto>>($"{BaseUrl}/reservations?date={dateStr}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for date {Date}", date);
            return null;
        }
    }

    public async Task<TableReservationDto?> GetReservationAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<TableReservationDto>($"{BaseUrl}/reservations/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> CreateReservationAsync(CreateTableReservationDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateTableReservationDto, TableReservationDto>($"{BaseUrl}/reservations", createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation");
            return null;
        }
    }

    public async Task<TableReservationDto?> UpdateReservationAsync(Guid id, UpdateTableReservationDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateTableReservationDto, TableReservationDto>($"{BaseUrl}/reservations/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> ConfirmReservationAsync(Guid id)
    {
        try
        {
            return await _httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/confirm", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> MarkArrivedAsync(Guid id)
    {
        try
        {
            return await _httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/arrived", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation arrived {Id}", id);
            return null;
        }
    }

    public async Task<bool> CancelReservationAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/reservations/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {Id}", id);
            return false;
        }
    }

    public async Task<TableReservationDto?> MarkNoShowAsync(Guid id)
    {
        try
        {
            return await _httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/no-show", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation no-show {Id}", id);
            return null;
        }
    }
}
