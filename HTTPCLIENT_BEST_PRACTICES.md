# HttpClient Best Practices Implementation

This document describes the comprehensive HttpClient best practices implementation across the entire EventForge.Client project.

## Problem Statement

The original EventForge.Client implementation had inconsistent HttpClient usage patterns across services:

1. **Mixed Injection Patterns**: Some services used direct `HttpClient` injection while others used `IHttpClientFactory`
2. **Potential BaseAddress Issues**: Direct HttpClient injection could lead to null BaseAddress problems
3. **Inconsistent Authentication**: Different services handled authentication headers differently
4. **Maintainability Concerns**: No standardized approach for HTTP client configuration

## Comprehensive Solution

### 1. Program.cs Configuration

All HttpClient instances are now properly configured with named clients at startup:

```csharp
// Configure HttpClient instances using best practices
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.Timeout = TimeSpan.FromSeconds(30);
    // Add default headers for API requests
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
});

// Configure StaticClient for translation files and static assets
builder.Services.AddHttpClient("StaticClient", client =>
{
    // In Blazor WASM, static files are served from the same origin as the app
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Key Benefits:**
- BaseAddress is set once at startup before any requests
- Named client pattern ensures consistency
- Follows Microsoft's recommended DI patterns

### 2. Service Layer Refactoring

#### Standardized Pattern

All services now follow the same pattern using `IHttpClientFactory`:

```csharp
public class ExampleService : IExampleService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(IHttpClientFactory httpClientFactory, ILogger<ExampleService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SomeDto> GetDataAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        _logger.LogDebug("Service: Using HttpClient with BaseAddress: {BaseAddress}", httpClient.BaseAddress);
        
        return await httpClient.GetFromJsonAsync<SomeDto>("api/endpoint");
    }
}
```

#### Refactored Services

✅ **HealthService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **BackupService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **AuthService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **SuperAdminService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **SignalRService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **ConfigurationService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **LogsService**: Changed from direct `HttpClient` to `IHttpClientFactory`  
✅ **TranslationService**: Already using `IHttpClientFactory` (best practice example)  
✅ **HttpClientService**: Already using `IHttpClientFactory` (centralized service)  

### 3. Authentication Handling

#### Centralized Pattern

The `HttpClientService` provides centralized authentication handling:

```csharp
private async Task<HttpClient> GetConfiguredHttpClientAsync()
{
    var httpClient = _httpClientFactory.CreateClient("ApiClient");
    
    // Ensure authentication header is set
    var token = await _authService.GetAccessTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
    
    return httpClient;
}
```

#### Service-Specific Authentication

Services that need authentication (like `SuperAdminService` and `LogsService`) implement their own authentication patterns:

```csharp
private async Task<HttpClient> CreateAuthenticatedHttpClientAsync()
{
    var httpClient = _httpClientFactory.CreateClient("ApiClient");
    
    var token = await _authService.GetAccessTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
    
    return httpClient;
}
```

## Best Practices Followed

### ✅ HttpClient Configuration
- **BaseAddress set at startup**: Never modified after first request
- **Named HttpClient pattern**: Uses IHttpClientFactory with pre-configured instances
- **Proper DI registration**: All configuration done in Program.cs
- **No direct HttpClient injection**: All services use IHttpClientFactory

### ✅ Consistency and Maintainability
- **Standardized patterns**: All services follow the same HttpClient creation pattern
- **Temporary logging**: BaseAddress logging added for verification (to be removed)
- **Authentication handling**: Consistent token management across services
- **Error handling**: Proper exception handling and logging

### ✅ Performance and Reliability
- **Socket exhaustion prevention**: IHttpClientFactory manages connection pooling
- **BaseAddress null prevention**: Named clients ensure BaseAddress is always set
- **Authentication efficiency**: Tokens are set per request, not per client instance
- **Memory efficiency**: HttpClient instances are properly managed

## Named Clients

### ApiClient
- **Purpose**: API calls to the EventForge.Server
- **BaseAddress**: `https://localhost:7241/`
- **Used by**: All services making API calls
- **Authentication**: Set per request when needed

### StaticClient  
- **Purpose**: Static files and translation resources
- **BaseAddress**: Blazor HostEnvironment.BaseAddress
- **Used by**: TranslationService for i18n files
- **Authentication**: Not required

## Verification and Testing

### Build Verification
- ✅ All services compile successfully
- ✅ No direct HttpClient injections remain
- ✅ Named clients properly configured
- ✅ Authentication patterns consistent

### Runtime Verification
- BaseAddress logging temporarily added to all services
- Services create HttpClient instances correctly
- Authentication headers properly set when needed

## Troubleshooting

### Common Issues

1. **BaseAddress is null**: 
   - Ensure service uses `_httpClientFactory.CreateClient("ApiClient")`
   - Verify Program.cs has proper AddHttpClient configuration

2. **Authentication not working**:
   - Check that service calls GetAccessTokenAsync before making requests
   - Verify Authorization header is set on HttpClient instance

3. **Wrong base URL**:
   - Ensure correct named client is used ("ApiClient" vs "StaticClient")
   - Check Program.cs configuration for the named client

## Future Maintenance

### Adding New Services

When adding new services that need HTTP calls:

1. Inject `IHttpClientFactory` (not `HttpClient` directly)
2. Use `CreateClient("ApiClient")` for API calls
3. Use `CreateClient("StaticClient")` for static resources
4. Add authentication if needed using established patterns
5. Include BaseAddress logging during development

### Updating Configuration

- All HttpClient configuration should be done in Program.cs
- Use AddHttpClient with named clients
- Never modify BaseAddress after client creation
- Consider adding new named clients for different scenarios

## References

- [Microsoft Docs: HttpClient factory in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests)
- [Microsoft Docs: HttpClient usage in Blazor](https://learn.microsoft.com/en-us/answers/questions/437107/httpclient-not-fetching-baseaddress)
- [StackOverflow: Configure HttpClient in Blazor Server](https://stackoverflow.com/questions/63828177/how-to-configure-httpclient-base-address-in-blazor-server-using-ihttpclientfacto)

## Maintenance Notes

For future maintainers:
1. **HttpClient configuration**: Always done in Program.cs using AddHttpClient
2. **Service injection**: Always inject IHttpClientFactory, never HttpClient directly
3. **Named clients**: Use "ApiClient" for API calls, "StaticClient" for static resources
4. **BaseAddress**: Never modify after startup - configured once in Program.cs
5. **Authentication**: Handle per request using established patterns
6. **Logging**: Remove temporary BaseAddress logging after verification