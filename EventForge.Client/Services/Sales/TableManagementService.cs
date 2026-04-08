using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for table management.
/// </summary>
public class TableManagementService(
    IHttpClientService httpClientService,
    ILogger<TableManagementService> logger) : ITableManagementService
{
    private const string BaseUrl = "api/v1/tables";

    public async Task<List<TableSessionDto>?> GetAllTablesAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<TableSessionDto>>(BaseUrl, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> GetTableAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TableSessionDto>($"{BaseUrl}/{id}", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving table {Id}", id);
            return null;
        }
    }

    public async Task<List<TableSessionDto>?> GetAvailableTablesAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<TableSessionDto>>($"{BaseUrl}/available", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available tables");
            return null;
        }
    }

    public async Task<TableSessionDto?> CreateTableAsync(CreateTableSessionDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateTableSessionDto, TableSessionDto>(BaseUrl, createDto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating table");
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableAsync(Guid id, UpdateTableSessionDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateTableSessionDto, TableSessionDto>($"{BaseUrl}/{id}", updateDto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating table {Id}", id);
            return null;
        }
    }

    public async Task<TableSessionDto?> UpdateTableStatusAsync(Guid id, UpdateTableStatusDto statusDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateTableStatusDto, TableSessionDto>($"{BaseUrl}/{id}/status", statusDto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating table status {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteTableAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting table {Id}", id);
            return false;
        }
    }

    public async Task<List<TableReservationDto>?> GetReservationsByDateAsync(DateTime date, CancellationToken ct = default)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            return await httpClientService.GetAsync<List<TableReservationDto>>($"{BaseUrl}/reservations?date={dateStr}", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reservations for date {Date}", date);
            return null;
        }
    }

    public async Task<TableReservationDto?> GetReservationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TableReservationDto>($"{BaseUrl}/reservations/{id}", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> CreateReservationAsync(CreateTableReservationDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateTableReservationDto, TableReservationDto>($"{BaseUrl}/reservations", createDto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating reservation");
            return null;
        }
    }

    public async Task<TableReservationDto?> UpdateReservationAsync(Guid id, UpdateTableReservationDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateTableReservationDto, TableReservationDto>($"{BaseUrl}/reservations/{id}", updateDto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> ConfirmReservationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/confirm", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming reservation {Id}", id);
            return null;
        }
    }

    public async Task<TableReservationDto?> MarkArrivedAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/arrived", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking reservation arrived {Id}", id);
            return null;
        }
    }

    public async Task<bool> CancelReservationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/reservations/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling reservation {Id}", id);
            return false;
        }
    }

    public async Task<TableReservationDto?> MarkNoShowAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<object, TableReservationDto>($"{BaseUrl}/reservations/{id}/no-show", new { });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking reservation no-show {Id}", id);
            return null;
        }
    }
}
