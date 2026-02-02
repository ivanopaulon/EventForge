using EventForge.DTOs.Auth;
using EventForge.DTOs.Tenants;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetAccessTokenAsync();
        Task<UserDto?> GetCurrentUserAsync();
        Task<bool> IsInRoleAsync(string role);
        Task<bool> IsInAnyRoleAsync(params string[] roles);
        Task<bool> HasAllRolesAsync(params string[] roles);
        Task<string[]> GetUserRolesAsync();
        Task<bool> IsSuperAdminAsync();
        Task<bool> IsAdminOrSuperAdminAsync();
        event Action? OnAuthenticationStateChanged;

        // Nuovo: recupera i tenant disponibili per il login (leggero, cached lato client)
        Task<IEnumerable<TenantResponseDto>> GetAvailableTenantsAsync();

        // Refresh the JWT token to extend the session
        Task<bool> RefreshTokenAsync();

        // Get token expiry time
        Task<TimeSpan?> GetTokenTimeToExpiryAsync();
    }

    public class AuthService : IAuthService
    {
        private const string BaseUrl = "api/v1/auth";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthService> _logger;
        private string? _accessToken;
        private UserDto? _currentUser;
        private readonly string _tokenKey = "auth_token";
        private readonly string _userKey = "current_user";

        // Cache locale per evitare chiamate ripetute
        private List<TenantResponseDto>? _cachedTenants;

        public event Action? OnAuthenticationStateChanged;

        public AuthService(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime, ILogger<AuthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_accessToken == null)
            {
                await LoadTokenFromStorageAsync();
            }

            if (string.IsNullOrEmpty(_accessToken))
                return false;

            // Check if token is expired
            return !IsTokenExpired(_accessToken);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            if (_accessToken == null)
            {
                await LoadTokenFromStorageAsync();
            }
            return _accessToken;
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            if (_currentUser == null)
            {
                await LoadUserFromStorageAsync();
            }
            return _currentUser;
        }

        public async Task<bool> IsInRoleAsync(string role)
        {
            var user = await GetCurrentUserAsync();
            return user?.Roles?.Contains(role, StringComparer.OrdinalIgnoreCase) == true;
        }

        public async Task<bool> IsInAnyRoleAsync(params string[] roles)
        {
            var user = await GetCurrentUserAsync();
            if (user?.Roles == null || !user.Roles.Any())
                return false;

            return roles.Any(role => user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<bool> HasAllRolesAsync(params string[] roles)
        {
            var user = await GetCurrentUserAsync();
            if (user?.Roles == null || !user.Roles.Any())
                return false;

            return roles.All(role => user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<string[]> GetUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Roles?.ToArray() ?? Array.Empty<string>();
        }

        public async Task<bool> IsSuperAdminAsync()
        {
            return await IsInRoleAsync("SuperAdmin");
        }

        public async Task<bool> IsAdminOrSuperAdminAsync()
        {
            return await IsInAnyRoleAsync("Admin", "SuperAdmin");
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");

                _logger.LogDebug("Invio richiesta di autenticazione per {Username}", loginRequest.Username);
                var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Elaborazione risposta login...");
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                    if (loginResponse != null)
                    {
                        _accessToken = loginResponse.AccessToken;
                        _currentUser = loginResponse.User;

                        _logger.LogDebug("Salvataggio token e utente in localStorage");
                        // Store in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _tokenKey, _accessToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _userKey, System.Text.Json.JsonSerializer.Serialize(_currentUser));

                        // Invalidate tenants cache on successful login (if multi-tenant visibility changes)
                        _cachedTenants = null;

                        // Note: Authentication headers are now handled by HttpClientService.GetConfiguredHttpClientAsync()
                        // No need to set DefaultRequestHeaders on individual HttpClient instances

                        OnAuthenticationStateChanged?.Invoke();
                    }

                    return loginResponse;
                }

                _logger.LogWarning("Login request failed with status {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogDebug("Logout: pulizia stato locale");

                _accessToken = null;
                _currentUser = null;
                // Clear cached tenants on logout
                _cachedTenants = null;

                // Note: No need to clear HttpClient headers since we use IHttpClientFactory
                // Authentication is handled in HttpClientService.GetConfiguredHttpClientAsync()

                _logger.LogDebug("Rimozione dati locali da localStorage");
                // Clear localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _tokenKey);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _userKey);

                OnAuthenticationStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante logout");
                throw;
            }
        }

        /// <summary>
        /// Recupera i tenant disponibili per il login. Metodo leggero, con caching client-side per ridurre round-trip.
        /// Endpoint server previsto: GET /api/v1/auth/tenants
        /// </summary>
        // Modifica: implementazione pi� robusta di GetAvailableTenantsAsync con timeout, retry e logging
        public async Task<IEnumerable<TenantResponseDto>> GetAvailableTenantsAsync()
        {
            try
            {
                if (_cachedTenants != null && _cachedTenants.Any())
                    return _cachedTenants;

                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var endpoint = "api/v1/tenants/available";
                const int timeoutSeconds = 10;
                const int maxAttempts = 2;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    try
                    {
                        _logger.LogDebug("Attempt {Attempt} - GET {Endpoint} (BaseAddress: {BaseAddress})", attempt, endpoint, httpClient.BaseAddress);
                        var response = await httpClient.GetAsync(endpoint, cts.Token);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("GET {Endpoint} returned status {StatusCode} on attempt {Attempt}", endpoint, response.StatusCode, attempt);
                            // If server returned non-success, no point in retrying for 4xx (but we allow one retry for transient 5xx)
                            if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                            {
                                await Task.Delay(500);
                                continue;
                            }

                            return Enumerable.Empty<TenantResponseDto>();
                        }

                        var tenants = await response.Content.ReadFromJsonAsync<IEnumerable<TenantResponseDto>>(cancellationToken: cts.Token);
                        _cachedTenants = tenants?.ToList() ?? new List<TenantResponseDto>();

                        sw.Stop();
                        _logger.LogInformation("Loaded {Count} tenants from {Endpoint} in {ElapsedMs}ms (attempt {Attempt})", _cachedTenants.Count, endpoint, sw.ElapsedMilliseconds, attempt);

                        return _cachedTenants;
                    }
                    catch (TaskCanceledException tex)
                    {
                        sw.Stop();
                        if (cts.IsCancellationRequested)
                        {
                            _logger.LogWarning(tex, "Timeout ({Timeout}s) while loading tenants from {Endpoint} on attempt {Attempt}", timeoutSeconds, endpoint, attempt);
                        }
                        else
                        {
                            _logger.LogWarning(tex, "Request cancelled while loading tenants from {Endpoint} on attempt {Attempt}", endpoint, attempt);
                        }

                        if (attempt < maxAttempts)
                        {
                            // small backoff before retry
                            await Task.Delay(500);
                            continue;
                        }

                        return Enumerable.Empty<TenantResponseDto>();
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.LogWarning(ex, "Failed to load available tenants from {Endpoint} on attempt {Attempt}", endpoint, attempt);
                        if (attempt < maxAttempts)
                        {
                            await Task.Delay(500);
                            continue;
                        }

                        return Enumerable.Empty<TenantResponseDto>();
                    }
                }

                return Enumerable.Empty<TenantResponseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error in GetAvailableTenantsAsync");
                return Enumerable.Empty<TenantResponseDto>();
            }
        }

        private async Task LoadTokenFromStorageAsync()
        {
            try
            {
                _accessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", _tokenKey);
                if (!string.IsNullOrEmpty(_accessToken) && !IsTokenExpired(_accessToken))
                {
                    // Token is valid, keep it
                    // Note: Authentication headers are handled by HttpClientService.GetConfiguredHttpClientAsync()
                }
                else
                {
                    _accessToken = null;
                }
            }
            catch (Exception)
            {
                _accessToken = null;
            }
        }

        private async Task LoadUserFromStorageAsync()
        {
            try
            {
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", _userKey);
                if (!string.IsNullOrEmpty(userJson))
                {
                    _currentUser = System.Text.Json.JsonSerializer.Deserialize<UserDto>(userJson);
                }
            }
            catch (Exception)
            {
                _currentUser = null;
            }
        }

        private static bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                return jwtToken.ValidTo <= DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            const int maxRetries = 2;

            try
            {
                // Check if we're authenticated first
                if (!await IsAuthenticatedAsync())
                {
                    _logger.LogWarning("Cannot refresh token: User not authenticated");
                    return false;
                }

                // SLIDING EXPIRATION MODE: Always attempt refresh when called
                // No need to check time to expiry - this is intentional to keep session alive
                var currentToken = await GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(currentToken))
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(currentToken);
                    var timeToExpiry = jwtToken.ValidTo - DateTime.UtcNow;

                    _logger.LogDebug(
                        "Token refresh requested. Current validity: {Minutes:F1} minutes. Proceeding with sliding expiration refresh.", 
                        timeToExpiry.TotalMinutes);
                }

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient("ApiClient");

                        if (attempt > 1)
                        {
                            _logger.LogInformation("Attempting token refresh (attempt {Attempt}/{MaxRetries}) - Sliding Expiration Mode", 
                                attempt, maxRetries);
                        }
                        else
                        {
                            _logger.LogDebug("Attempting token refresh (attempt {Attempt}/{MaxRetries}) - Sliding Expiration Mode", 
                                attempt, maxRetries);
                        }

                        var response = await client.PostAsync($"{BaseUrl}/refresh-token", null);

                        if (response.IsSuccessStatusCode)
                        {
                            var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
                            if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                            {
                                // Update token in memory
                                _accessToken = refreshResponse.AccessToken;

                                // Save to localStorage
                                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _tokenKey, _accessToken);

                                _logger.LogInformation("Token refreshed successfully on attempt {Attempt}. New token expires in {Minutes} minutes", 
                                    attempt, refreshResponse.ExpiresIn / 60);

                                // Notify that authentication state may have changed
                                OnAuthenticationStateChanged?.Invoke();

                                return true;
                            }
                        }

                        _logger.LogWarning("Token refresh failed with status code: {StatusCode} (attempt {Attempt}/{MaxRetries})",
                            response.StatusCode, attempt, maxRetries);

                        // Retry only on 5xx errors or network issues
                        if ((int)response.StatusCode >= 500 && attempt < maxRetries)
                        {
                            await Task.Delay(1000 * attempt); // Linear backoff: 1s, 2s
                            continue;
                        }

                        // For 401/403 errors, don't retry
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            _logger.LogError("Token refresh denied by server (status {StatusCode}). User may need to re-login.", 
                                response.StatusCode);
                            return false;
                        }
                    }
                    catch (Exception attemptEx)
                    {
                        _logger.LogError(attemptEx, "Error during token refresh attempt {Attempt}/{MaxRetries}", 
                            attempt, maxRetries);

                        if (attempt < maxRetries)
                        {
                            await Task.Delay(1000 * attempt);
                            continue;
                        }
                    }
                }

                _logger.LogError("Token refresh failed after {MaxRetries} attempts", maxRetries);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error refreshing token");
                return false;
            }
        }

        /// <summary>
        /// Gets the time remaining until token expiration
        /// </summary>
        public async Task<TimeSpan?> GetTokenTimeToExpiryAsync()
        {
            try
            {
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo - DateTime.UtcNow;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid JWT token format when checking expiry");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking token expiry");
                return null;
            }
        }
    }
}