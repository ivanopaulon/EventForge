# EventForge Client Services - Creation Guide

## Overview
This guide provides the standard patterns and best practices for creating client-side services in EventForge. Following these patterns ensures consistency, proper error handling, authentication management, and maintainability.

## Table of Contents
1. [Service Architecture](#service-architecture)
2. [Standard Service Pattern](#standard-service-pattern)
3. [HttpClient Configuration](#httpclient-configuration)
4. [Creating a New Service](#creating-a-new-service)
5. [Error Handling](#error-handling)
6. [Authentication](#authentication)
7. [Examples](#examples)
8. [Common Mistakes to Avoid](#common-mistakes-to-avoid)

---

## Service Architecture

EventForge uses a layered architecture for HTTP communication:

```
Blazor Components
       ↓
  IHttpClientService (Centralized)
       ↓
  IHttpClientFactory
       ↓
  Named HttpClient ("ApiClient")
       ↓
  EventForge.Server API
```

### Why IHttpClientService?

The `IHttpClientService` provides:
- ✅ **Centralized error handling** with user-friendly messages
- ✅ **Automatic authentication** token injection
- ✅ **Consistent logging** across all services
- ✅ **BaseAddress management** preventing null reference errors
- ✅ **Snackbar integration** for user feedback
- ✅ **ProblemDetails parsing** for API error responses

---

## Standard Service Pattern

### Service Interface

```csharp
namespace EventForge.Client.Services
{
    public interface IMyEntityService
    {
        // List/Get operations
        Task<PagedResult<MyEntityDto>> GetEntitiesAsync(int page = 1, int pageSize = 20);
        Task<MyEntityDto?> GetEntityAsync(Guid id);
        
        // Create operation
        Task<MyEntityDto> CreateEntityAsync(CreateMyEntityDto createDto);
        
        // Update operation
        Task<MyEntityDto> UpdateEntityAsync(Guid id, UpdateMyEntityDto updateDto);
        
        // Delete operation
        Task DeleteEntityAsync(Guid id);
    }
}
```

### Service Implementation

```csharp
namespace EventForge.Client.Services
{
    public class MyEntityService : IMyEntityService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<MyEntityService> _logger;
        private readonly ILoadingDialogService? _loadingDialogService; // Optional
        
        public MyEntityService(
            IHttpClientService httpClientService,
            ILogger<MyEntityService> logger,
            ILoadingDialogService? loadingDialogService = null)
        {
            _httpClientService = httpClientService;
            _logger = logger;
            _loadingDialogService = loadingDialogService;
        }
        
        public async Task<PagedResult<MyEntityDto>> GetEntitiesAsync(int page = 1, int pageSize = 20)
        {
            var result = await _httpClientService.GetAsync<PagedResult<MyEntityDto>>(
                $"api/v1/my-entities?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<MyEntityDto> 
            { 
                Items = new List<MyEntityDto>(), 
                TotalCount = 0, 
                Page = page, 
                PageSize = pageSize 
            };
        }
        
        public async Task<MyEntityDto?> GetEntityAsync(Guid id)
        {
            return await _httpClientService.GetAsync<MyEntityDto>($"api/v1/my-entities/{id}");
        }
        
        public async Task<MyEntityDto> CreateEntityAsync(CreateMyEntityDto createDto)
        {
            try
            {
                if (_loadingDialogService != null)
                {
                    await _loadingDialogService.ShowAsync("Creazione Entità", "Creazione in corso...", true);
                    await _loadingDialogService.UpdateProgressAsync(30);
                }
                
                var result = await _httpClientService.PostAsync<CreateMyEntityDto, MyEntityDto>(
                    "api/v1/my-entities", createDto);
                
                if (_loadingDialogService != null)
                {
                    await _loadingDialogService.UpdateProgressAsync(100);
                    await Task.Delay(500);
                    await _loadingDialogService.HideAsync();
                }
                
                return result ?? throw new InvalidOperationException("Failed to create entity");
            }
            catch (Exception)
            {
                if (_loadingDialogService != null)
                {
                    await _loadingDialogService.HideAsync();
                }
                throw;
            }
        }
        
        public async Task<MyEntityDto> UpdateEntityAsync(Guid id, UpdateMyEntityDto updateDto)
        {
            var result = await _httpClientService.PutAsync<UpdateMyEntityDto, MyEntityDto>(
                $"api/v1/my-entities/{id}", updateDto);
            return result ?? throw new InvalidOperationException("Failed to update entity");
        }
        
        public async Task DeleteEntityAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/my-entities/{id}");
        }
    }
}
```

---

## HttpClient Configuration

### ❌ WRONG - Do NOT do this

```csharp
// WRONG: Direct HttpClient injection
public class MyService
{
    private readonly HttpClient _httpClient;
    
    public MyService(HttpClient httpClient)
    {
        _httpClient = httpClient; // BaseAddress might be null!
    }
}
```

```csharp
// WRONG: Using IHttpClientFactory directly (inconsistent pattern)
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<Data> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        // Manual token management, error handling, etc.
        var response = await client.GetAsync("api/data");
        response.EnsureSuccessStatusCode(); // Basic error handling only
        return await response.Content.ReadFromJsonAsync<Data>();
    }
}
```

### ✅ CORRECT - Use IHttpClientService

```csharp
// CORRECT: Use IHttpClientService for all API calls
public class MyService
{
    private readonly IHttpClientService _httpClientService;
    
    public MyService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }
    
    public async Task<Data?> GetDataAsync()
    {
        // Automatic token injection, error handling, logging!
        return await _httpClientService.GetAsync<Data>("api/v1/data");
    }
}
```

---

## Creating a New Service

### Step 1: Define the Interface

Create `IMyEntityService.cs` in `EventForge.Client/Services/`:

```csharp
using EventForge.DTOs.Common;
using EventForge.DTOs.MyEntity;

namespace EventForge.Client.Services
{
    public interface IMyEntityService
    {
        Task<PagedResult<MyEntityDto>> GetEntitiesAsync(int page = 1, int pageSize = 20);
        Task<MyEntityDto?> GetEntityAsync(Guid id);
        Task<MyEntityDto> CreateEntityAsync(CreateMyEntityDto createDto);
        Task<MyEntityDto> UpdateEntityAsync(Guid id, UpdateMyEntityDto updateDto);
        Task DeleteEntityAsync(Guid id);
    }
}
```

### Step 2: Implement the Service

Create `MyEntityService.cs` in `EventForge.Client/Services/`:

```csharp
using EventForge.DTOs.Common;
using EventForge.DTOs.MyEntity;

namespace EventForge.Client.Services
{
    public class MyEntityService : IMyEntityService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<MyEntityService> _logger;
        
        public MyEntityService(
            IHttpClientService httpClientService,
            ILogger<MyEntityService> logger)
        {
            _httpClientService = httpClientService;
            _logger = logger;
        }
        
        // Implementation methods...
    }
}
```

### Step 3: Register the Service

Add to `EventForge.Client/Program.cs`:

```csharp
// Add custom services
builder.Services.AddScoped<IMyEntityService, MyEntityService>();
```

### Step 4: Verify Server Endpoint

Ensure the server controller exists with matching routes:

```csharp
[Route("api/v1/my-entities")]
[ApiController]
public class MyEntitiesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MyEntityDto>>> GetEntities(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        // Implementation
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MyEntityDto>> GetEntity(Guid id)
    {
        // Implementation
    }
    
    [HttpPost]
    public async Task<ActionResult<MyEntityDto>> CreateEntity([FromBody] CreateMyEntityDto dto)
    {
        // Implementation
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MyEntityDto>> UpdateEntity(Guid id, [FromBody] UpdateMyEntityDto dto)
    {
        // Implementation
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEntity(Guid id)
    {
        // Implementation
    }
}
```

---

## Error Handling

### Automatic Error Handling

`IHttpClientService` automatically handles:
- HTTP status codes (400, 401, 403, 404, 429, 500, 503)
- ProblemDetails parsing
- User-friendly error messages
- Snackbar notifications for critical errors
- Client-side error logging

### Custom Error Handling

```csharp
public async Task<MyEntityDto?> GetEntityAsync(Guid id)
{
    try
    {
        return await _httpClientService.GetAsync<MyEntityDto>($"api/v1/my-entities/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Handle specific case
        _logger.LogWarning("Entity {Id} not found", id);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving entity {Id}", id);
        throw;
    }
}
```

---

## Authentication

### Automatic Token Management

`IHttpClientService` automatically:
1. Retrieves the access token via `IAuthService`
2. Adds the `Authorization: Bearer {token}` header
3. Refreshes the token if needed

You don't need to manually manage authentication headers!

### Example (Automatic)

```csharp
// Token is automatically added - no manual work needed!
public async Task<Data> GetProtectedDataAsync()
{
    return await _httpClientService.GetAsync<Data>("api/v1/protected-data") 
           ?? throw new InvalidOperationException("Failed to retrieve data");
}
```

---

## Examples

### Example 1: Simple CRUD Service

```csharp
public class ProductService : IProductService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IHttpClientService httpClientService, ILogger<ProductService> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }
    
    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20)
    {
        var result = await _httpClientService.GetAsync<PagedResult<ProductDto>>(
            $"api/v1/products?page={page}&pageSize={pageSize}");
        return result ?? new PagedResult<ProductDto>();
    }
    
    public async Task<ProductDto?> GetProductAsync(Guid id)
    {
        return await _httpClientService.GetAsync<ProductDto>($"api/v1/products/{id}");
    }
    
    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var result = await _httpClientService.PostAsync<CreateProductDto, ProductDto>(
            "api/v1/products", dto);
        return result ?? throw new InvalidOperationException("Failed to create product");
    }
    
    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var result = await _httpClientService.PutAsync<UpdateProductDto, ProductDto>(
            $"api/v1/products/{id}", dto);
        return result ?? throw new InvalidOperationException("Failed to update product");
    }
    
    public async Task DeleteProductAsync(Guid id)
    {
        await _httpClientService.DeleteAsync($"api/v1/products/{id}");
    }
}
```

### Example 2: Service with Loading Dialog

```csharp
public class ComplexService : IComplexService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILoadingDialogService _loadingDialogService;
    private readonly ILogger<ComplexService> _logger;
    
    public ComplexService(
        IHttpClientService httpClientService,
        ILoadingDialogService loadingDialogService,
        ILogger<ComplexService> logger)
    {
        _httpClientService = httpClientService;
        _loadingDialogService = loadingDialogService;
        _logger = logger;
    }
    
    public async Task<ResultDto> PerformComplexOperationAsync(ComplexDto dto)
    {
        try
        {
            await _loadingDialogService.ShowAsync(
                "Operazione Complessa", 
                "Avvio operazione...", 
                true);
            
            await _loadingDialogService.UpdateProgressAsync(20);
            await _loadingDialogService.UpdateOperationAsync("Validazione dati...");
            
            await _loadingDialogService.UpdateProgressAsync(50);
            await _loadingDialogService.UpdateOperationAsync("Elaborazione...");
            
            var result = await _httpClientService.PostAsync<ComplexDto, ResultDto>(
                "api/v1/complex-operation", dto);
            
            await _loadingDialogService.UpdateProgressAsync(100);
            await _loadingDialogService.UpdateOperationAsync("Completato!");
            
            await Task.Delay(1000);
            await _loadingDialogService.HideAsync();
            
            return result ?? throw new InvalidOperationException("Operation failed");
        }
        catch (Exception)
        {
            await _loadingDialogService.HideAsync();
            throw;
        }
    }
}
```

### Example 3: Service with Custom Filtering

```csharp
public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly IHttpClientService _httpClientService;
    
    public AdvancedSearchService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }
    
    public async Task<PagedResult<ItemDto>> SearchAsync(SearchCriteria criteria)
    {
        // Build query string from criteria
        var queryParams = new List<string>
        {
            $"page={criteria.Page}",
            $"pageSize={criteria.PageSize}"
        };
        
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            queryParams.Add($"search={Uri.EscapeDataString(criteria.SearchTerm)}");
        
        if (criteria.CategoryId.HasValue)
            queryParams.Add($"categoryId={criteria.CategoryId.Value}");
        
        if (criteria.MinPrice.HasValue)
            queryParams.Add($"minPrice={criteria.MinPrice.Value}");
        
        if (criteria.MaxPrice.HasValue)
            queryParams.Add($"maxPrice={criteria.MaxPrice.Value}");
        
        var queryString = string.Join("&", queryParams);
        var result = await _httpClientService.GetAsync<PagedResult<ItemDto>>(
            $"api/v1/items/search?{queryString}");
        
        return result ?? new PagedResult<ItemDto>();
    }
}
```

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Direct HttpClient Injection
```csharp
// WRONG
public MyService(HttpClient httpClient) { }
```
**Problem**: BaseAddress might be null, no automatic authentication, no centralized error handling.

**Solution**: Use `IHttpClientService`

---

### ❌ Mistake 2: Manual Token Management
```csharp
// WRONG
var token = await _authService.GetAccessTokenAsync();
var client = _httpClientFactory.CreateClient("ApiClient");
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```
**Problem**: Code duplication, inconsistent token management.

**Solution**: `IHttpClientService` handles this automatically

---

### ❌ Mistake 3: Manual JSON Serialization
```csharp
// WRONG
var json = JsonSerializer.Serialize(dto);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await _httpClient.PostAsync(url, content);
```
**Problem**: Verbose, error-prone, inconsistent serialization options.

**Solution**: Use `IHttpClientService.PostAsync<TRequest, TResponse>()`

---

### ❌ Mistake 4: Basic Error Handling
```csharp
// WRONG
response.EnsureSuccessStatusCode();
```
**Problem**: No user feedback, no logging, generic error messages.

**Solution**: `IHttpClientService` provides comprehensive error handling

---

### ❌ Mistake 5: Inconsistent Return Types
```csharp
// INCONSISTENT
public async Task<List<ItemDto>> GetItems() { }
public async Task<IEnumerable<ItemDto>> GetOtherItems() { }
```
**Problem**: Inconsistent patterns, difficult to maintain.

**Solution**: Use `PagedResult<T>` for lists, nullable types for single items:
```csharp
public async Task<PagedResult<ItemDto>> GetItems(int page, int pageSize) { }
public async Task<ItemDto?> GetItem(Guid id) { }
```

---

## Summary

### ✅ Always Use
- `IHttpClientService` for all API calls
- Consistent return types (`PagedResult<T>`, nullable types)
- Proper exception handling with logging
- Loading dialogs for long operations

### ❌ Never Use
- Direct `HttpClient` injection
- Manual token management
- Manual JSON serialization
- Basic error handling without user feedback

### 📝 Remember
1. All services follow the same pattern
2. `IHttpClientService` handles authentication, errors, and logging
3. Keep service methods simple and focused
4. Let the framework handle the complexity

---

## Reference Services

Good examples to follow:
- ✅ `BusinessPartyService.cs` - Complete CRUD with loading dialogs
- ✅ `FinancialService.cs` - Multiple entity types in one service
- ✅ `SuperAdminService.cs` - Complex operations with progress tracking
- ✅ `EntityManagementService.cs` - Clean, simple CRUD operations

---

For more information, see:
- [HttpClient Best Practices](./HTTPCLIENT_BEST_PRACTICES.md)
- [Management Pages Guide](./MANAGEMENT_PAGES_GUIDE.md)
