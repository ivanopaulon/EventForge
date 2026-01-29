# Service Layer Organization

## Overview
This directory contains the business logic layer of EventForge, organized into domain-specific service groups and facades.

## Directory Structure

```
Services/
├── Documents/          # Document management services
│   ├── IDocumentFacade.cs
│   ├── DocumentFacade.cs
│   └── ... (specialized document services)
├── Warehouse/          # Warehouse and inventory services
│   ├── IWarehouseFacade.cs
│   ├── WarehouseFacade.cs
│   └── ... (specialized warehouse services)
├── Caching/           # Cache management
├── Auth/              # Authentication and authorization
└── ... (other service groups)
```

## Facade Services

Facade services consolidate related domain services for simplified controller usage and reduced complexity.

### When to Use Facade Pattern

Consider using a facade when:
- ✅ Controller has 7+ constructor dependencies
- ✅ Services are related by domain (Documents, Warehouse, etc.)
- ✅ Multiple services work together frequently
- ✅ You want to simplify unit testing

See [DEPENDENCY_INJECTION_PATTERNS.md](../../docs/architecture/DEPENDENCY_INJECTION_PATTERNS.md) for detailed guidance.

## DocumentFacade

**Location**: `EventForge.Server/Services/Documents/DocumentFacade.cs`

**Purpose**: Consolidates document-related services into a single, cohesive interface.

**Consolidated Services**:
- Document Headers
- Document Types & Status
- Document Attachments
- Document Comments
- Document Templates
- Document Workflows
- Document Analytics

**Impact**: Reduced DocumentsController dependencies from 11 to 4 (69% reduction)

### Usage in Controllers
```csharp
public class DocumentsController : BaseApiController
{
    private readonly IDocumentFacade _documentFacade;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentsController> _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;

    [HttpGet("{id}/attachments")]
    public async Task<ActionResult> GetAttachments(Guid id, CancellationToken ct)
    {
        var attachments = await _documentFacade.GetAttachmentsAsync(id, ct);
        return Ok(attachments);
    }
}
```

## WarehouseFacade

**Location**: `EventForge.Server/Services/Warehouse/WarehouseFacade.cs`

**Purpose**: Consolidates warehouse operations into a unified interface.

**Consolidated Services**:
- Storage Facilities & Locations (warehouses)
- Inventory Management
- Stock Management & Movements
- Product Operations
- Document Headers (warehouse-specific)
- Export Services

**Methods**: 69 facade methods consolidating 12 individual services

**Impact**: Reduced WarehouseManagementController dependencies significantly

### Usage in Controllers
```csharp
public class WarehouseManagementController : BaseApiController
{
    private readonly IWarehouseFacade _warehouseFacade;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WarehouseManagementController> _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;

    [HttpGet("inventory")]
    public async Task<ActionResult> GetInventory(
        PaginationParameters pagination, 
        CancellationToken ct)
    {
        var inventory = await _warehouseFacade.GetInventoryEntriesAsync(
            pagination, ct);
        return Ok(inventory);
    }
}
```

## When to Create a Facade

### Decision Criteria

**Create a Facade if**:
- ❌ Controller has 7+ constructor dependencies
- ❌ Related services from the same domain
- ❌ Complex orchestration between services
- ❌ Testing requires many mock objects

**Don't Create a Facade if**:
- ✅ Controller has < 7 dependencies
- ✅ Services are unrelated by domain
- ✅ Services don't collaborate frequently
- ✅ Facade would add unnecessary abstraction

### Benefits of Facades
- ✅ **Reduced complexity**: 60-70% fewer constructor parameters
- ✅ **Simplified testing**: Fewer mocks required
- ✅ **Better separation of concerns**: Controllers focus on HTTP, facades handle orchestration
- ✅ **Encapsulated domain logic**: Business rules stay in the service layer
- ✅ **Easier refactoring**: Changes to service composition isolated to facade

### Example Metrics

| Controller | Before Facade | After Facade | Reduction |
|------------|--------------|--------------|-----------|
| DocumentsController | 11 dependencies | 4 dependencies | 69% |
| WarehouseManagementController | 14+ dependencies | 4 dependencies | 71% |

## Service Layer Standards

All services must follow the standards defined in:
- [SERVICE_LAYER_STANDARDS.md](../../docs/architecture/SERVICE_LAYER_STANDARDS.md)

### Key Requirements
- ✅ **CancellationToken** on all async methods
- ✅ **AsNoTracking()** for read operations
- ✅ **Projection-first** pattern (Select before ToList)
- ✅ **Structured logging** with parameters
- ✅ **Tenant isolation** enforced
- ✅ **Audit logging** for mutations

## Architecture Documentation

For comprehensive architecture patterns and best practices, see:
- [DEPENDENCY_INJECTION_PATTERNS.md](../../docs/architecture/DEPENDENCY_INJECTION_PATTERNS.md) - Facade pattern, circular dependency detection
- [SERVICE_LAYER_STANDARDS.md](../../docs/architecture/SERVICE_LAYER_STANDARDS.md) - Service implementation standards

## Adding New Services

When adding a new service:

1. **Create interface** in appropriate domain folder
2. **Implement service** following SERVICE_LAYER_STANDARDS.md
3. **Register in DI** container (Program.cs)
4. **Consider facade** if controller would have 7+ dependencies
5. **Add XML documentation** for all public methods
6. **Add unit tests** for business logic

## Testing Facades

Facades simplify testing by reducing the number of mocks:

```csharp
// BEFORE: Mock 11 services
var headerService = Mock<IDocumentHeaderService>();
var typeService = Mock<IDocumentTypeService>();
// ... 9 more mocks
var controller = new DocumentsController(
    headerService, typeService, ...);

// AFTER: Mock 1 facade
var documentFacade = Mock<IDocumentFacade>();
var controller = new DocumentsController(
    documentFacade, tenantContext, logger, cacheInvalidation);
```

## Performance Considerations

Facades should:
- ✅ Delegate to specialized services (thin orchestration layer)
- ✅ Avoid business logic (keep in specialized services)
- ✅ Pass CancellationToken through
- ✅ Maintain async patterns
- ❌ NOT add extra database calls
- ❌ NOT cache or buffer data (use dedicated caching services)

## Summary

The service layer is organized into:
1. **Specialized services**: Domain-specific business logic
2. **Facade services**: Simplified interfaces for controllers
3. **Shared services**: Cross-cutting concerns (caching, audit, etc.)

This organization provides:
- ✅ Clean controller code
- ✅ Better testability
- ✅ Clear separation of concerns
- ✅ Maintainable architecture
