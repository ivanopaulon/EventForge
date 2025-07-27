using EventForge.DTOs.Auth;
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
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthService> _logger;
        private string? _accessToken;
        private UserDto? _currentUser;
        private readonly string _tokenKey = "auth_token";
        private readonly string _userKey = "current_user";

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

                var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                    if (loginResponse != null)
                    {
                        _accessToken = loginResponse.AccessToken;
                        _currentUser = loginResponse.User;

                        // Store in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _tokenKey, _accessToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _userKey, System.Text.Json.JsonSerializer.Serialize(_currentUser));

                        // Note: Authentication headers are now handled by HttpClientService.GetConfiguredHttpClientAsync()
                        // No need to set DefaultRequestHeaders on individual HttpClient instances

                        OnAuthenticationStateChanged?.Invoke();
                    }
                    return loginResponse;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            _accessToken = null;
            _currentUser = null;
            // Note: No need to clear HttpClient headers since we use IHttpClientFactory
            // Authentication is handled in HttpClientService.GetConfiguredHttpClientAsync()

            // Clear localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _tokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _userKey);

            OnAuthenticationStateChanged?.Invoke();
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
    }
}