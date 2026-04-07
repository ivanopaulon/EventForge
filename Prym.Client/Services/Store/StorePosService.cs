using Prym.DTOs.Common;
using Prym.DTOs.Store;
using System.Net;
using System.Net.Http.Json;

namespace Prym.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store POS terminals.
/// </summary>
public class StorePosService(
    HttpClient httpClient,
    ILogger<StorePosService> logger) : IStorePosService
{
    private const string ApiBase = "api/v1/storeusers/pos";

    public async Task<List<StorePosDto>> GetAllAsync()
    {
        try
        {
            // Try non-paginated endpoint first for better performance
            try
            {
                var activeEndpoint = ApiBase.Replace("/pos", "") + "/active";
                var activeResponse = await httpClient.GetAsync(activeEndpoint);
                if (activeResponse.IsSuccessStatusCode)
                {
                    var activePosResult = await activeResponse.Content.ReadFromJsonAsync<List<StorePosDto>>();
                    logger.LogDebug("Successfully fetched POS terminals from /active endpoint");
                    return activePosResult ?? [];
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Non-paginated /active endpoint not available, falling back to paginated endpoint");
            }

            // Fallback to paginated endpoint with reduced pageSize
            var response = await httpClient.GetAsync($"{ApiBase}?page=1&pageSize=100");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to get POS terminals: {StatusCode}", response.StatusCode);
                return [];
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StorePosDto>>();
            return pagedResult?.Items?.ToList() ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest || ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(ex, "POS terminals API returned {StatusCode}, returning empty list", ex.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting all store POS terminals, returning empty list");
            return [];
        }
    }

    public async Task<List<StorePosDto>> GetActiveAsync()
    {
        try
        {
            var allPos = await GetAllAsync();
            return allPos.Where(p => p.Status == CashRegisterStatus.Active).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting active store POS terminals, returning empty list");
            return [];
        }
    }

    public async Task<StorePosDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StorePosDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorePosService] Error getting store POS {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StorePosService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting store POS {Id}", id);
            throw;
        }
    }

    public async Task<StorePosDto?> CreateAsync(CreateStorePosDto createDto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", logger);
                logger.LogError("Error creating store POS: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorePosService] Error creating store POS: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StorePosService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error creating store POS");
            throw new InvalidOperationException("Errore nella creazione del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", logger);
                logger.LogError("Error updating store POS {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorePosService] Error updating store POS {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StorePosService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error updating store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", logger);
                logger.LogError("Error deleting store POS {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            Console.WriteLine($"[StorePosService] Error deleting store POS {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StorePosService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error deleting store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione del punto cassa.", ex);
        }
    }

    public async Task<PagedResult<StorePosDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "punto cassa", logger);
                logger.LogError("Error getting paged store POS terminals (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}",
                    page, pageSize, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<PagedResult<StorePosDto>>()
                ?? new PagedResult<StorePosDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StorePosService] Error getting paged store POS terminals (page: {page}, pageSize: {pageSize}): {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StorePosService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting paged store POS terminals (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento dei punti cassa.", ex);
        }
    }
}
