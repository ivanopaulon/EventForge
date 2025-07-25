using EventForge.DTOs.Auth;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        bool IsAuthenticated { get; }
        string? AccessToken { get; }
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private string? _accessToken;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
        public string? AccessToken => _accessToken;

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", loginRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                    if (loginResponse != null)
                    {
                        _accessToken = loginResponse.AccessToken;
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
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

        public void Logout()
        {
            _accessToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}