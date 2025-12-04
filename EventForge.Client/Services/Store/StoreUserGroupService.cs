using EventForge.DTOs.Store;
using EventForge.DTOs.Common;
using System.Net.Http.Json;
using System.Net;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store user groups.
/// </summary>
public class StoreUserGroupService : IStoreUserGroupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StoreUserGroupService> _logger;
    private const string ApiBase = "api/v1/storeusers/groups";
    private const int MaxPageSize = 1000; // Maximum page size for GetAll operations

    public StoreUserGroupService(HttpClient httpClient, ILogger<StoreUserGroupService> logger)
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
                HttpStatusCode.NotFound => "Gruppo non trovato.",
                HttpStatusCode.Conflict => "Il gruppo esiste già o c'è un conflitto con i dati esistenti.",
                _ => $"Errore: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting error message from response");
            return "Si è verificato un errore durante l'operazione.";
        }
    }

    public async Task<List<StoreUserGroupDto>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page=1&pageSize={MaxPageSize}");
            response.EnsureSuccessStatusCode();
            
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserGroupDto>>();
            return pagedResult?.Items?.ToList() ?? new List<StoreUserGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all store user groups");
            throw;
        }
    }

    public async Task<PagedResult<StoreUserGroupDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error getting paged store user groups (page: {Page}, pageSize: {PageSize}): {StatusCode} - {ErrorMessage}", 
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
            _logger.LogError(ex, "Error getting paged store user groups (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw new InvalidOperationException("Errore nel caricamento dei gruppi.", ex);
        }
    }

    public async Task<StoreUserGroupDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserGroupDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user group {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> CreateAsync(CreateStoreUserGroupDto createDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBase, createDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error creating store user group: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error creating store user group");
            throw new InvalidOperationException("Errore nella creazione del gruppo. Verifica i dati e riprova.", ex);
        }
    }

    public async Task<StoreUserGroupDto?> UpdateAsync(Guid id, UpdateStoreUserGroupDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await GetErrorMessageAsync(response);
                _logger.LogError("Error updating store user group {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error updating store user group {Id}", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del gruppo. Verifica i dati e riprova.", ex);
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
                _logger.LogError("Error deleting store user group {Id}: {StatusCode} - {ErrorMessage}", id, response.StatusCode, errorMessage);
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
            _logger.LogError(ex, "Error deleting store user group {Id}", id);
            throw new InvalidOperationException("Errore nell'eliminazione del gruppo.", ex);
        }
    }
}
