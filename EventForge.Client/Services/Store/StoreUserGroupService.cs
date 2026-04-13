using Prym.DTOs.Common;
using Prym.DTOs.Store;
using System.Net.Http.Json;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store user groups.
/// </summary>
public class StoreUserGroupService(
    HttpClient httpClient,
    ILogger<StoreUserGroupService> logger) : IStoreUserGroupService
{
    private const string ApiBase = "api/v1/storeusers/groups";
    private const int MaxPageSize = 1000;

    public async Task<List<StoreUserGroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page=1&pageSize={MaxPageSize}", ct);
            response.EnsureSuccessStatusCode();

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserGroupDto>>();
            return pagedResult?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all store user groups");
            throw;
        }
    }

    public async Task<PagedResult<StoreUserGroupDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "gruppo", logger);
                logger.LogError("Error getting paged store user groups (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}",
                    page, pageSize, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<PagedResult<StoreUserGroupDto>>()
                ?? new PagedResult<StoreUserGroupDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged store user groups (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento dei gruppi.", ex);
        }
    }

    public async Task<StoreUserGroupDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StoreUserGroupDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store user group {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> CreateAsync(CreateStoreUserGroupDto createDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "gruppo", logger);
                logger.LogError("Error creating store user group: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StoreUserGroupDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating store user group");
            throw new InvalidOperationException("Errore nella creazione del gruppo. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StoreUserGroupDto?> UpdateAsync(Guid id, UpdateStoreUserGroupDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "gruppo", logger);
                logger.LogError("Error updating store user group {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<StoreUserGroupDto>();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating store user group {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del gruppo. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await StoreServiceHelper.GetErrorMessageAsync(response, "gruppo", logger);
                logger.LogError("Error deleting store user group {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            logger.LogError(ex, "Error deleting store user group {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione del gruppo.", ex);
        }
    }
}
