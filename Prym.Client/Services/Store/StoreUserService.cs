using Prym.DTOs.Common;
using Prym.DTOs.Store;
using System.Net.Http.Json;

namespace Prym.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store users.
/// </summary>
public class StoreUserService(
    HttpClient httpClient,
    ILogger<StoreUserService> logger) : IStoreUserService
{
    private const string ApiBase = "api/v1/storeusers";

    public async Task<List<StoreUserDto>> GetAllAsync()
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page=1&pageSize=1000");
            response.EnsureSuccessStatusCode();

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserDto>>();
            return pagedResult?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StoreUserService] Error getting all store users: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting all store users");
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StoreUserService] Error getting store user {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting store user {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/by-username/{username}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StoreUserService] Error getting store user by username {username}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting store user by username {Username}", username);
            throw;
        }
    }

    public async Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, createDto);

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
            Console.WriteLine($"[StoreUserService] Error creating store user: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error creating store user");
            throw new InvalidOperationException("Errore nella creazione dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);

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
            Console.WriteLine($"[StoreUserService] Error updating store user {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error updating store user {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}");

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
            Console.WriteLine($"[StoreUserService] Error deleting store user {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error deleting store user {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione dell'operatore.", ex);
        }
    }

    public async Task<PagedResult<StoreUserDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");

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
            Console.WriteLine($"[StoreUserService] Error getting paged store users (page: {page}, pageSize: {pageSize}): {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[StoreUserService] StackTrace: {ex.StackTrace}");
            logger.LogError(ex, "Error getting paged store users (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento degli operatori.", ex);
        }
    }

    public async Task<IEnumerable<StoreUserDto>> GetWithBirthdayAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IEnumerable<StoreUserDto>>($"{ApiBase}/with-birthdays")
                ?? Enumerable.Empty<StoreUserDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting store users with birthday");
            return Enumerable.Empty<StoreUserDto>();
        }
    }
}
