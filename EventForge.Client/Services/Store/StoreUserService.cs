using EventForge.DTOs.Store;
using EventForge.DTOs.Common;
using System.Net.Http.Json;
using System.Net;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store users.
/// </summary>
public class StoreUserService : IStoreUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StoreUserService> _logger;
    private const string ApiBase = "api/v1/storeusers";

    public StoreUserService(HttpClient httpClient, ILogger<StoreUserService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts a user-friendly error message from the HTTP response, with special handling for tenant-related errors.
    /// </summary>
    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Check for tenant-related errors
            if (content.Contains("Tenant context is required", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("TenantId", StringComparison.OrdinalIgnoreCase))
            {
                return "Impossibile completare l'operazione: contesto tenant mancante. Effettua nuovamente l'accesso.";
            }

            // Try to parse ProblemDetails
            try
            {
                var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetailsDto>(content);
                if (!string.IsNullOrEmpty(problemDetails?.Detail))
                {
                    return problemDetails.Detail;
                }
                if (!string.IsNullOrEmpty(problemDetails?.Title))
                {
                    return problemDetails.Title;
                }
            }
            catch
            {
                // Not a ProblemDetails response
            }

            // Return generic message based on status code
            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Dati non validi. Verifica i campi inseriti.",
                HttpStatusCode.Unauthorized => "Non autorizzato. Effettua nuovamente l'accesso.",
                HttpStatusCode.Forbidden => "Non hai i permessi necessari per questa operazione.",
                HttpStatusCode.NotFound => "Operatore non trovato.",
                HttpStatusCode.Conflict => "L'operatore esiste già o c'è un conflitto con i dati esistenti.",
                _ => $"Errore: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting error message from response");
            return "Si è verificato un errore durante l'operazione.";
        }
    }

    public async Task<List<StoreUserDto>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page=1&pageSize=1000");
            response.EnsureSuccessStatusCode();
            
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserDto>>();
            return pagedResult?.Items?.ToList() ?? new List<StoreUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all store users");
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/by-username/{username}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user by username {Username}", username);
            throw;
        }
    }

    public async Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBase, createDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error creating store user: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error creating store user");
            throw new InvalidOperationException("Errore nella creazione dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error updating store user {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error updating store user {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento dell'operatore. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBase}/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error deleting store user {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error deleting store user {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione dell'operatore.", ex);
        }
    }

    public async Task<PagedResult<StoreUserDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error getting paged store users (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}", 
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
            _logger.LogError(ex, "Error getting paged store users (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento degli operatori.", ex);
        }
    }
}
