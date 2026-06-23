using Prym.DTOs.Common;
using Prym.DTOs.Store;
using Prym.DTOs.Teams;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service implementation for managing store users.
/// </summary>
public class StoreUserService(
    HttpClient httpClient,
    ILogger<StoreUserService> logger) : IStoreUserService
{
    private const string ApiBase = "api/v1/storeusers";

    public async Task<List<StoreUserDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page=1&pageSize=1000", ct);
            response.EnsureSuccessStatusCode();

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserDto>>();
            return pagedResult?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all store users");
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store user {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/by-username/{username}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store user by username {Username}", username);
            throw;
        }
    }

    public async Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "operatore", logger);
                logger.LogError("Error creating store user: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StoreUserDto>();
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw with our custom message
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating store user");
            throw new InvalidOperationException("Errore nella creazione dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "operatore", logger);
                logger.LogError("Error updating store user {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StoreUserDto>();
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw with our custom message
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating store user {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "operatore", logger);
                logger.LogError("Error deleting store user {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw with our custom message
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting store user {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione dell'operatore.", ex);
        }
    }

    public async Task<PagedResult<StoreUserDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "operatore", logger);
                logger.LogError("Error getting paged store users (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}",
                    page, pageSize, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<PagedResult<StoreUserDto>>()
                ?? new PagedResult<StoreUserDto>();
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw with our custom message
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged store users (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento degli operatori.", ex);
        }
    }

    public async Task<IEnumerable<StoreUserDto>> GetWithBirthdayAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IEnumerable<StoreUserDto>>($"{ApiBase}/with-birthdays", ct)
                ?? Enumerable.Empty<StoreUserDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store users with birthday");
            return Enumerable.Empty<StoreUserDto>();
        }
    }

    public async Task<StoreUserDto?> UploadImageAsync(Guid id, IBrowserFile file, CancellationToken ct = default)
    {
        try
        {
            const long maxFileSize = 5 * 1024 * 1024;
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await httpClient.PostAsync($"{ApiBase}/{id}/photo", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "immagine operatore", logger);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StoreUserDto>(cancellationToken: ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image for store user {Id}", id);
            throw new InvalidOperationException("Errore nel caricamento dell'immagine operatore.", ex);
        }
    }

    public async Task<DocumentReferenceDto?> GetImageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DocumentReferenceDto>($"{ApiBase}/{id}/photo", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}/photo", ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "immagine operatore", logger);
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
            logger.LogError(ex, "Error deleting image for store user {Id}", id);
            throw new InvalidOperationException("Errore nella rimozione dell'immagine operatore.", ex);
        }
    }
}
