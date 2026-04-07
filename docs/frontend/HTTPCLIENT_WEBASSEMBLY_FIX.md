# HttpClient WebAssembly Compatibility Fix

## Problem Description

The Blazor WebAssembly client was experiencing a `PlatformNotSupportedException` during startup due to unsupported HttpClientHandler configuration properties in the WebAssembly environment.

### Error Details

```
System.PlatformNotSupportedException: Operation is not supported on this platform.
at System.Net.Http.BrowserHttpHandler.set_UseCookies(Boolean value)
at System.Net.Http.HttpClientHandler.set_UseCookies(Boolean value)
at Program.<>c.<<Main>$>b__0_1() in Program.cs:line 22
```

## Root Cause

In Blazor WebAssembly, HttpClient uses `BrowserHttpHandler` internally, which has limited configuration options compared to `HttpClientHandler` used in server-side applications. The following properties are **not supported** in WebAssembly:

- `UseCookies` - Cookie handling is managed by the browser
- `MaxConnectionsPerServer` - Connection pooling is managed by the browser
- `UseProxy` - Proxy settings are managed by the browser

## Solution

**File:** `EventForge.Client/Program.cs`

**Before:**
```csharp
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    // These properties are NOT supported in WebAssembly
    UseCookies = false,
    MaxConnectionsPerServer = 10,
    UseProxy = false
});
```

**After:**
```csharp
// Configure HttpClient instances using best practices for performance
// Note: WebAssembly uses BrowserHttpHandler which doesn't support HttpClientHandler configuration
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.Timeout = TimeSpan.FromSeconds(30);
    // Add default headers for API requests
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
    // Enable compression for better mobile performance
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
});
```

## Changes Made

1. **Removed** `ConfigurePrimaryHttpMessageHandler` configuration that was causing the exception
2. **Kept** all supported HttpClient configuration (base address, timeout, headers)
3. **Added** explanatory comment about WebAssembly limitations

## WebAssembly HttpClient Limitations

When developing Blazor WebAssembly applications, be aware of these limitations:

### Supported Configurations
- ✅ `BaseAddress`
- ✅ `Timeout`
- ✅ `DefaultRequestHeaders`
- ✅ Standard HTTP methods (GET, POST, PUT, DELETE, PATCH)

### Unsupported Configurations
- ❌ `HttpClientHandler.UseCookies`
- ❌ `HttpClientHandler.MaxConnectionsPerServer`
- ❌ `HttpClientHandler.UseProxy`
- ❌ `HttpClientHandler.ClientCertificates`
- ❌ Custom certificate validation
- ❌ Windows authentication

### Browser-Managed Features
In WebAssembly, these features are automatically handled by the browser:
- Cookie management (respects browser cookie settings)
- Connection pooling and limits
- Proxy configuration (uses browser proxy settings)
- Compression (automatically handled if server supports it)

## Testing

The fix has been tested and verified:

1. ✅ Application starts without `PlatformNotSupportedException`
2. ✅ HttpClient functionality preserved for API calls
3. ✅ All existing features continue to work
4. ✅ No breaking changes to application behavior

## Best Practices for Blazor WebAssembly HttpClient

1. **Always** avoid `ConfigurePrimaryHttpMessageHandler` in WebAssembly projects
2. **Use** conditional compilation if you need different configurations for server vs. WebAssembly
3. **Rely** on browser capabilities for connection management and security
4. **Test** in actual WebAssembly environment, not just compilation

## Example of Conditional Configuration (if needed)

```csharp
#if !WASM
// Server-side configuration
builder.Services.AddHttpClient("ApiClient", client => { /* config */ })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        UseCookies = false,
        MaxConnectionsPerServer = 10
    });
#else
// WebAssembly configuration
builder.Services.AddHttpClient("ApiClient", client => { /* config */ });
#endif
```

## References

- [Blazor WebAssembly HttpClient Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/call-web-api)
- [Browser limitations in Blazor WebAssembly](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios)