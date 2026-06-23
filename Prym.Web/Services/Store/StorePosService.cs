using Prym.DTOs.Common;
using Prym.DTOs.Store;
using Prym.DTOs.Teams;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service implementation for managing store POS terminals.
/// </summary>
public class StorePosService(
    HttpClient httpClient,
    ILogger<StorePosService> logger) : IStorePosService
{
    private const string ApiBase = "api/v1/storeusers/pos";

    public async Task<List<StorePosDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page=1&pageSize=100", ct);

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

    public async Task<List<StorePosDto>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            var allPos = await GetAllAsync(ct);
            return allPos.Where(p => p.Status == CashRegisterStatus.Active).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting active store POS terminals, returning empty list");
            return [];
        }
    }

    public async Task<StorePosDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StorePosDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store POS {Id}", id);
            throw;
        }
    }

    public async Task<StorePosDto?> CreateAsync(CreateStorePosDto createDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto, ct);

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
            logger.LogError(ex, "Error creating store POS");
            throw new InvalidOperationException("Errore nella creazione del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto, ct);

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
            logger.LogError(ex, "Error updating store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del punto cassa. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);

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
            logger.LogError(ex, "Error deleting store POS {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione del punto cassa.", ex);
        }
    }

    public async Task<PagedResult<StorePosDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}", ct);

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
            logger.LogError(ex, "Error getting paged store POS terminals (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento dei punti cassa.", ex);
        }
    }

    public async Task<StorePosDto?> UploadImageAsync(Guid id, IBrowserFile file, CancellationToken ct = default)
    {
        try
        {
            const long maxFileSize = 5 * 1024 * 1024;
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await httpClient.PostAsync($"{ApiBase}/{id}/image", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "immagine punto cassa", logger);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StorePosDto>(cancellationToken: ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image for store POS {Id}", id);
            throw new InvalidOperationException("Errore nel caricamento dell'immagine del punto cassa.", ex);
        }
    }

    public async Task<DocumentReferenceDto?> GetImageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DocumentReferenceDto>($"{ApiBase}/{id}/image", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}/image", ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "immagine punto cassa", logger);
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
            logger.LogError(ex, "Error deleting image for store POS {Id}", id);
            throw new InvalidOperationException("Errore nella rimozione dell'immagine del punto cassa.", ex);
        }
    }
}
