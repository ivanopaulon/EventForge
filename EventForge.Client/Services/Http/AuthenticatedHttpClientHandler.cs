using EventForge.Client.Services;
using System.Net.Http.Headers;

namespace EventForge.Client.Services.Http;

/// <summary>
/// DelegatingHandler that automatically injects Bearer token authentication
/// into outgoing HTTP requests for authenticated API calls.
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
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve the access token from the auth service
            var token = await _authService.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                // Add Authorization header with Bearer token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                _logger.LogDebug("Authorization header added for request to {RequestUri}", request.RequestUri);
            }
            else
            {
                _logger.LogWarning("No access token available for request to {RequestUri}", request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token for request to {RequestUri}", request.RequestUri);
            // Continue with the request even if token retrieval fails
        }

        // Add correlation ID for request tracking
        var correlationId = Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);

        return await base.SendAsync(request, cancellationToken);
    }
}
