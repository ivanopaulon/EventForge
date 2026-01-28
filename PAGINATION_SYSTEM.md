# Pagination System Foundation

## Overview

The EventForge Pagination System provides a centralized, configurable, and role-based approach to pagination across all API endpoints. This foundation replaces hardcoded pagination limits with a flexible system that supports endpoint-specific overrides, role-based limits, and export operations.

## Features

- **Centralized Configuration**: All pagination settings managed in `appsettings.json`
- **Role-Based Limits**: Different page size limits for User, Admin, and SuperAdmin roles
- **Endpoint Overrides**: Specific endpoints can have custom page size limits
- **Wildcard Support**: Pattern matching for endpoint groups (e.g., `/api/v1/export/*`)
- **Export Operations**: Special handling for export operations via `X-Export-Operation` header
- **Automatic Capping**: Page sizes are automatically capped to maximum allowed values
- **Logging**: Warning logs when capping occurs, information logs when exceeding recommended sizes
- **Backward Compatibility**: Existing endpoints with `int page, int pageSize` continue to work

## Configuration

### appsettings.json

```json
{
  "Pagination": {
    "DefaultPageSize": 20,
    "MaxPageSize": 1000,
    "MaxExportPageSize": 10000,
    "RecommendedPageSize": 100,
    "EndpointOverrides": {
      "/api/v1/stock/overview": 5000,
      "/api/v1/export/*": 10000
    },
    "RoleBasedLimits": {
      "User": 1000,
      "Admin": 5000,
      "SuperAdmin": 10000
    }
  }
}
```

### Configuration Properties

| Property | Description | Default |
|----------|-------------|---------|
| `DefaultPageSize` | Default page size when not specified | 20 |
| `MaxPageSize` | Maximum page size for unauthenticated users | 1000 |
| `MaxExportPageSize` | Maximum page size for export operations | 10000 |
| `RecommendedPageSize` | Recommended page size threshold for logging | 100 |
| `EndpointOverrides` | Dictionary of endpoint-specific limits | {} |
| `RoleBasedLimits` | Dictionary of role-specific limits | {} |

## Usage

### Using PaginationParameters in Controllers

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
    [FromQuery] PaginationParameters pagination)
{
    var skip = pagination.CalculateSkip();
    var take = pagination.PageSize;
    
    var products = await _context.Products
        .Skip(skip)
        .Take(take)
        .ToListAsync();
    
    return Ok(new PagedResult<ProductDto>
    {
        Items = products,
        TotalCount = await _context.Products.CountAsync(),
        Page = pagination.Page,
        PageSize = pagination.PageSize
    });
}
```

### Client Usage

#### Default Pagination
```http
GET /api/v1/products
# Uses default page size (20)
```

#### Custom Page Size
```http
GET /api/v1/products?page=2&pageSize=50
```

#### Export Operation
```http
GET /api/v1/products?page=1&pageSize=5000
X-Export-Operation: true
```

## Priority System

The pagination system applies limits in the following priority order (highest to lowest):

1. **Endpoint Override (Exact Match)**: `/api/v1/stock/overview` → 5000
2. **Endpoint Override (Wildcard)**: `/api/v1/export/*` → 10000
3. **Role-Based Limit**: User (1000), Admin (5000), SuperAdmin (10000)
4. **Export Header**: `X-Export-Operation: true` → MaxExportPageSize (10000)
5. **Default**: MaxPageSize (1000)

### Priority Examples

| Scenario | User Role | Endpoint | Header | Result |
|----------|-----------|----------|--------|--------|
| Stock overview | User | `/api/v1/stock/overview` | - | 5000 |
| Export products | User | `/api/v1/export/products` | - | 10000 |
| Regular query | Admin | `/api/v1/products` | - | 5000 |
| Export flag | User | `/api/v1/products` | `X-Export-Operation: true` | 10000 |
| Unauthenticated | - | `/api/v1/products` | - | 1000 |

## Model Binding

The `PaginationModelBinder` automatically:
- Parses `page` and `pageSize` from query parameters
- Applies appropriate limits based on context
- Sets `WasCapped` flag when page size is reduced
- Logs warnings when capping occurs
- Logs information when recommended size is exceeded

### Capping Notification

When a requested page size exceeds the allowed limit, the system:
1. Caps the page size to the maximum allowed
2. Sets `WasCapped = true` on the `PaginationParameters` object
3. Sets `AppliedMaxPageSize` to the limit that was applied
4. Logs a warning with details

## Testing

### Unit Tests
- 15+ unit tests in `EventForge.Tests/ModelBinders/PaginationModelBinderTests.cs`
- Tests cover all scenarios: defaults, capping, role-based limits, endpoint overrides, wildcards

### Integration Tests
- 8+ integration tests in `EventForge.Tests/Integration/PaginationIntegrationTests.cs`
- Tests verify configuration loading, JSON serialization, and end-to-end behavior

## Logging

### Warning Logs (Capping)
```
PageSize 2000 exceeds limit 1000 for user 'user@test.com' on path '/api/v1/products'. Capping to maximum.
```

### Information Logs (Above Recommended)
```
PageSize 150 exceeds recommended size 100 for user 'user@test.com' on path '/api/v1/products'.
```

## Best Practices

1. **Use PaginationParameters**: Always use `PaginationParameters` DTO instead of individual `int page, int pageSize` parameters
2. **Check WasCapped**: Check the `WasCapped` flag to notify users when their request was modified
3. **Configure Wisely**: Set `RecommendedPageSize` based on typical query performance
4. **Monitor Logs**: Review pagination logs to identify users requesting excessive page sizes
5. **Test Limits**: Verify role-based limits work correctly for your use cases

## Migration Guide

### Before (Old Code)
```csharp
[HttpGet]
public async Task<ActionResult> GetProducts(int page = 1, int pageSize = 20)
{
    // Hardcoded limits
    if (pageSize > 100) pageSize = 100;
    
    var skip = (page - 1) * pageSize;
    // ...
}
```

### After (New Code)
```csharp
[HttpGet]
public async Task<ActionResult> GetProducts([FromQuery] PaginationParameters pagination)
{
    var skip = pagination.CalculateSkip();
    var take = pagination.PageSize;
    // No need for manual capping - handled by model binder
    // ...
}
```

## Future Enhancements (Out of Scope for Phase 1)

- Controller refactoring to use `PaginationParameters` (PR #993)
- Service interface updates (PR #993)
- Export endpoints implementation (PR #994)
- FluentValidation integration (PR #992)
- BaseApiController enhancements (PR #992)

## Security Considerations

- **No Sensitive Data**: Pagination parameters don't expose sensitive information
- **Input Validation**: All parameters are validated and capped
- **Role-Based Access**: Limits are enforced based on user roles
- **Logging**: All capping events are logged for audit purposes

## Performance

- **Minimal Overhead**: Model binding adds negligible overhead
- **Efficient Capping**: Limits applied before database queries
- **Memory Safe**: Maximum page sizes prevent excessive memory usage
- **Query Optimization**: Recommended sizes encourage efficient queries

## Troubleshooting

### Issue: Page size not being capped
**Solution**: Verify `PaginationModelBinderProvider` is registered in Program.cs

### Issue: Role-based limits not working
**Solution**: Check user authentication and role claims are properly set

### Issue: Endpoint overrides not matching
**Solution**: Verify exact path match or wildcard pattern in configuration

### Issue: Logs not appearing
**Solution**: Check logging configuration and log level settings

## Related Issues & PRs

- **Issue #925**: Pagination System Implementation
- **PR #991**: This implementation (Phase 1 - Foundation)
- **PR #992**: BaseApiController & FluentValidation (Phase 2)
- **PR #993**: Controller Refactoring (Phase 3)
- **PR #994**: Export Endpoints (Phase 4)

## API Reference

### PaginationParameters

```csharp
public class PaginationParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 10000)]
    public int PageSize { get; set; } = 20;
    
    [JsonIgnore]
    public bool WasCapped { get; set; }
    
    [JsonIgnore]
    public int AppliedMaxPageSize { get; set; }
    
    public int CalculateSkip() => (Page - 1) * PageSize;
}
```

### PaginationSettings

```csharp
public class PaginationSettings
{
    public const string SectionName = "Pagination";
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 1000;
    public int MaxExportPageSize { get; set; } = 10000;
    public int RecommendedPageSize { get; set; } = 100;
    public Dictionary<string, int> EndpointOverrides { get; set; }
    public Dictionary<string, int> RoleBasedLimits { get; set; }
}
```

## Support

For questions or issues related to the pagination system, please:
1. Check this documentation
2. Review the test cases for examples
3. Check the issue #925 for design decisions
4. Open a new issue with the `pagination` label
