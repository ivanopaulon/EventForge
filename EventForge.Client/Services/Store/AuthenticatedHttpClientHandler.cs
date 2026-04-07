using System.Net.Http.Headers;

namespace EventForge.Client.Services.Store;

/// <summary>
/// DelegatingHandler that adds authentication token to all requests for Store services.
/// Also logs HTTP errors to browser console for debugging.
/// </summary>
public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthenticatedHttpClientHandler> _logger;

    public AuthenticatedHttpClientHandler(
        IAuthService authService,
        ILogger<AuthenticatedHttpClientHandler> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Add Bearer token to request
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Log errors to browser console for debugging
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[Store API Error] {request.Method} {request.RequestUri}: {response.StatusCode}");
            Console.WriteLine($"[Store API Error] Response: {content}");
            _logger.LogError("Store API request failed: {Method} {Uri} returned {StatusCode}",
                request.Method, request.RequestUri, response.StatusCode);
        }

        return response;
    }
}
