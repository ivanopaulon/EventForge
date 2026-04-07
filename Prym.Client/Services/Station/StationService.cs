using Prym.Client.Services.Store;
using Prym.DTOs.Common;
using Prym.DTOs.Station;
using System.Net;
using System.Net.Http.Json;

namespace Prym.Client.Services.Station;

/// <summary>
/// Client service implementation for managing stations.
/// </summary>
public class StationService(
    HttpClient httpClient,
    ILogger<StationService> logger) : IStationService
{
    private const string ApiBase = "api/v1/stations";

    public async Task<List<StationDto>> GetAllAsync()
    {
        try
        {
            var result = await GetPagedAsync(1, 200);
            return result.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting all stations, returning empty list");
            return [];
        }
    }

    public async Task<StationDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StationDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting station {Id}", id);
            throw;
        }
    }

    public async Task<StationDto?> CreateAsync(CreateStationDto createDto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "stazione", logger);
                logger.LogError("Error creating station: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StationDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating station");
            throw new InvalidOperationException("Errore nella creazione della stazione. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StationDto?> UpdateAsync(Guid id, UpdateStationDto updateDto)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "stazione", logger);
                logger.LogError("Error updating station {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StationDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating station {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento della stazione. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "stazione", logger);
                logger.LogError("Error deleting station {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting station {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione della stazione.", ex);
        }
    }

    public async Task<PagedResult<StationDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "stazione", logger);
                logger.LogError("Error getting paged stations (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}",
                    page, pageSize, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<PagedResult<StationDto>>()
                ?? new PagedResult<StationDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged stations (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento delle stazioni.", ex);
        }
    }
}
