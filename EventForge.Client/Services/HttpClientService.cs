using EventForge.DTOs.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Centralized HTTP client service with standardized error handling, 
/// authentication, and request configuration.
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// Performs a GET request and returns the deserialized response.
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a GET request and returns the response stream.
    /// </summary>
    Task<Stream> GetStreamAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a POST request with JSON payload and returns the response stream.
    /// </summary>
    Task<Stream> PostStreamAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a POST request with JSON payload.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a POST request without expecting a response body.
    /// </summary>
    Task PostAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a PUT request with JSON payload.
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a PUT request without expecting a response body.
    /// </summary>
    Task PutAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a DELETE request.
    /// </summary>
    Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a PATCH request with JSON payload.
    /// </summary>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of centralized HTTP client service.
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<HttpClientService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        ILogger<HttpClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;

        // Configure JSON options for consistent serialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("GET request to {Endpoint}", endpoint);

            var response = await httpClient.GetAsync(endpoint, cancellationToken);
            return await HandleResponseAsync<T>(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<Stream> GetStreamAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("GET stream request to {Endpoint}", endpoint);

            var response = await httpClient.GetAsync(endpoint, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, endpoint);

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET stream request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<Stream> PostStreamAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("POST stream request to {Endpoint}", endpoint);

            var response = await httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, endpoint);

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST stream request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("POST request to {Endpoint}", endpoint);

            var response = await httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task PostAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("POST request (no response) to {Endpoint}", endpoint);

            var response = await httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("PUT request to {Endpoint}", endpoint);

            var response = await httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task PutAsync<TRequest>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("PUT request (no response) to {Endpoint}", endpoint);

            var response = await httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("DELETE request to {Endpoint}", endpoint);

            var response = await httpClient.DeleteAsync(endpoint, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var httpClient = await GetConfiguredHttpClientAsync();

        try
        {
            _logger.LogDebug("PATCH request to {Endpoint}", endpoint);

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PatchAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PATCH request failed for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task<HttpClient> GetConfiguredHttpClientAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");

        // Ensure authentication header is set
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            if (httpClient.DefaultRequestHeaders.Authorization?.Parameter != token)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Add correlation ID for request tracking
        var correlationId = Guid.NewGuid().ToString();
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        return httpClient;
    }

    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, string endpoint)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default(T);
        }

        await EnsureSuccessStatusCodeAsync(response, endpoint);

        if (response.Content.Headers.ContentLength == 0)
        {
            return default(T);
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from {Endpoint}", endpoint);
            throw new InvalidOperationException($"Failed to deserialize response from {endpoint}", ex);
        }
    }

    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, string endpoint)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();

        _logger.LogWarning(
            "HTTP request failed. Endpoint: {Endpoint}, Status: {StatusCode}, Content: {Content}",
            endpoint, response.StatusCode, content);

        // Try to parse ProblemDetails if available
        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsDto>(content, _jsonOptions);
            if (problemDetails != null)
            {
                var exception = new HttpRequestException($"Request failed: {problemDetails.Title}", null, response.StatusCode);
                exception.Data["ProblemDetails"] = problemDetails;
                throw exception;
            }
        }
        catch (JsonException)
        {
            // Not a ProblemDetails response, continue with generic error
        }

        // Throw generic HTTP exception
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Non autorizzato. Effettua l'accesso e riprova.",
            HttpStatusCode.Forbidden => "Non hai i permessi necessari per questa operazione.",
            HttpStatusCode.NotFound => "La risorsa richiesta non è stata trovata.",
            HttpStatusCode.BadRequest => "Richiesta non valida. Verifica i dati inseriti.",
            HttpStatusCode.InternalServerError => "Errore interno del server. Riprova più tardi.",
            HttpStatusCode.ServiceUnavailable => "Servizio temporaneamente non disponibile.",
            _ => $"Errore HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
        };

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}