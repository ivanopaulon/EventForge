# Dependency Injection Patterns - EventForge

## Overview
This document describes DI patterns and best practices implemented in EventForge to maintain clean architecture, testability, and prevent common dependency pitfalls.

## Facade Pattern (M-1)

### When to Use
The Facade pattern should be considered when:
- A controller has 7+ constructor dependencies
- Related services are grouped by domain (Documents, Warehouse, etc.)
- You need to simplify controller testing by reducing mock objects
- Multiple services work together to provide cohesive domain functionality

### Example: DocumentFacade
The DocumentFacade consolidates 11 document-related services into a single facade:

**BEFORE**: DocumentsController with 11 dependencies
```csharp
public DocumentsController(
    IDocumentHeaderService headerService,
    IDocumentTypeService typeService,
    IDocumentStatusService statusService,
    IDocumentAttachmentService attachmentService,
    IDocumentCommentService commentService,
    IDocumentTemplateService templateService,
    IDocumentWorkflowService workflowService,
    IDocumentAnalyticsService analyticsService,
    ITenantContext tenantContext,
    ILogger<DocumentsController> logger,
    ICacheInvalidationService cacheInvalidation)
{
    // Constructor with 11 parameters...
}
```

**AFTER**: DocumentsController with 4 dependencies
```csharp
public DocumentsController(
    IDocumentFacade documentFacade,  // Contains all document services
    ITenantContext tenantContext,
    ILogger<DocumentsController> logger,
    ICacheInvalidationService cacheInvalidation)
{
    // Clean constructor with only 4 parameters
}
```

### Benefits
- ✅ **Reduced constructor parameters**: 69% reduction (11 → 4 dependencies)
- ✅ **Easier unit testing**: Only 4 mocks required instead of 11
- ✅ **Better separation of concerns**: Controller focuses on HTTP, facade handles orchestration
- ✅ **Single responsibility**: Each controller has a clear, focused purpose
- ✅ **Improved maintainability**: Changes to service composition are isolated to facade

### Implementation Pattern
```csharp
// Interface definition
public interface IDocumentFacade
{
    // Grouped operations by feature area
    Task<DocumentDto> GetDocumentAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<DocumentAttachmentDto>> GetAttachmentsAsync(Guid id, CancellationToken ct = default);
    // ... more operations
}

// Facade implementation
public class DocumentFacade : IDocumentFacade
{
    private readonly IDocumentHeaderService _headerService;
    private readonly IDocumentAttachmentService _attachmentService;
    // ... other services

    public DocumentFacade(
        IDocumentHeaderService headerService,
        IDocumentAttachmentService attachmentService,
        // ... other services
        ILogger<DocumentFacade> logger)
    {
        _headerService = headerService ?? throw new ArgumentNullException(nameof(headerService));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        // ... null checks for other services
    }

    public async Task<DocumentDto> GetDocumentAsync(Guid id, CancellationToken ct = default)
    {
        // Delegate to appropriate service
        return await _headerService.GetByIdAsync(id, ct);
    }
}
```

### When NOT to Use Facade
- Controller has fewer than 7 dependencies
- Services are not related by domain
- Services don't collaborate frequently
- Over-abstraction would reduce code clarity

## Circular Dependency Detection (M-3)

### Startup Validation
EventForge validates the DI container at application startup to detect circular dependencies early, preventing runtime failures.

**Location**: `EventForge.Server/Startup/DependencyValidationService.cs`

### What It Does
1. **Extracts service descriptors** from the DI container using reflection
2. **Builds dependency graph** by analyzing constructor parameters
3. **Runs DFS algorithm** to detect cycles in the dependency graph
4. **Throws clear error** with the full cycle path if circular dependency is found

### Example Error Output
```
Circular dependency detected:

DocumentHeaderService
  → IDocumentFacade
    → DocumentAnalyticsService
      → IDocumentHeaderService ❌ CYCLE!

Solution: Introduce an interface or refactor dependencies to break the cycle.
```

### How It Works
```csharp
// Called during application startup in Program.cs
public static void ValidateDependencies(
    IServiceProvider services,
    ILogger? logger = null)
{
    var serviceDescriptors = GetServiceDescriptors(services);
    var graph = BuildDependencyGraph(serviceDescriptors);
    var cycles = DetectCycles(graph);
    
    if (cycles.Any())
    {
        var errorMessage = FormatCycleError(cycles);
        throw new InvalidOperationException(
            $"Circular dependencies detected:\n{errorMessage}");
    }
}
```

### Algorithm
The validation uses **Depth-First Search (DFS)** with a visited set and recursion stack:
- **Visited set**: Tracks all nodes visited during traversal
- **Recursion stack**: Tracks current path to detect back edges (cycles)
- **Back edge detection**: If a node in the recursion stack is revisited, a cycle exists

### Benefits
- ✅ **Early detection**: Catches circular dependencies at startup, not runtime
- ✅ **Clear diagnostics**: Shows the exact dependency path causing the cycle
- ✅ **Prevents production issues**: Fails fast in development/testing
- ✅ **Better architecture**: Encourages proper dependency design

### Common Solutions
When a circular dependency is detected:

1. **Introduce an interface**: Extract shared functionality to an interface
```csharp
// Instead of: Service A → Service B → Service A
// Use: Service A → IServiceB, Service B → IServiceA
```

2. **Refactor to shared service**: Move common logic to a third service
```csharp
// Instead of: Service A ⇄ Service B
// Use: Service A → Shared Service ← Service B
```

3. **Use events/mediator pattern**: Replace direct dependencies with event-driven communication
```csharp
// Instead of: Service A → Service B
// Use: Service A → Event → Service B (loosely coupled)
```

## Service Layer Standards (M-2)

### CancellationToken Support
All async service methods **MUST** accept `CancellationToken` to support request cancellation and timeout handling.

**✅ CORRECT**:
```csharp
public async Task<ProductDto> GetProductAsync(
    Guid id, 
    CancellationToken ct = default)
{
    return await _context.Products
        .AsNoTracking()
        .Where(p => p.Id == id)
        .FirstOrDefaultAsync(ct);
}
```

**❌ WRONG**:
```csharp
public async Task<ProductDto> GetProductAsync(Guid id)
{
    // Missing CancellationToken - cannot be cancelled!
    return await _context.Products
        .FirstOrDefaultAsync();
}
```

**Benefits**:
- Supports request timeout and cancellation
- Prevents wasted resources on abandoned requests
- Improves application responsiveness
- Essential for long-running operations

### AsNoTracking for Read Operations
Read-only queries **MUST** use `.AsNoTracking()` to avoid unnecessary change tracking overhead.

**✅ CORRECT - Read operation**:
```csharp
return await _context.Products
    .AsNoTracking()  // No change tracking needed
    .Where(p => p.Id == id)
    .FirstOrDefaultAsync(ct);
```

**✅ CORRECT - Write operation** (needs tracking):
```csharp
var product = await _context.Products
    .Where(p => p.Id == id)
    .FirstOrDefaultAsync(ct);  // Tracking enabled for updates
    
product.Name = "Updated";
await _context.SaveChangesAsync(ct);
```

**Performance Impact**:
- **Significantly faster queries** for read operations (typical improvement: 30-50%)
- **Reduced memory usage** (typical reduction: 30-40%)
- **Better scalability** under high load

*Note: Actual performance improvements vary based on query complexity, entity size, and data volume.*

### Projection-First Pattern
Move `.Select()` **BEFORE** `.ToListAsync()` for SQL-side projection, not in-memory mapping.

**❌ WRONG - In-memory projection**:
```csharp
// Loads entire entity into memory, then projects
var items = await _context.Products.ToListAsync(ct);
return items.Select(MapToDto).ToList();
```

**✅ CORRECT - SQL projection**:
```csharp
// Projects in SQL, returns only needed columns
return await _context.Products
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price
        // Only needed fields - not entire entity!
    })
    .ToListAsync(ct);
```

**Performance Impact**:
- **Faster queries** (typical improvement: 30-50% - less data transferred)
- **Lower memory usage** (typical reduction: 30-40% - only needed fields loaded)
- **Reduced network traffic** (typical reduction: 50-70% - smaller result sets)

*Note: Actual performance gains vary based on entity size, number of columns, and data volume.*

### Exception Handling Standards

**1. OperationCanceledException**: Log as Information, re-throw
```csharp
catch (OperationCanceledException)
{
    _logger.LogInformation("Operation cancelled for {EntityId}", id);
    throw; // Re-throw to propagate cancellation
}
```

**2. Business validation exceptions**: Log as Warning, re-throw
```csharp
catch (BusinessValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for {EntityId}", id);
    throw; // Re-throw for controller to handle
}
```

**3. All other exceptions**: Log as Error with context, re-throw
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error retrieving entity {EntityId}", id);
    throw; // Re-throw to trigger error middleware
}
```

## Summary

These patterns work together to create a maintainable, scalable architecture:

1. **Facade Pattern** reduces complexity and improves testability
2. **Circular Dependency Detection** prevents architectural issues
3. **Service Layer Standards** ensure consistency and performance

Following these patterns results in:
- ✅ Cleaner, more maintainable code
- ✅ Better testability (fewer mocks)
- ✅ Improved performance (AsNoTracking, projection-first)
- ✅ Early problem detection (circular dependency validation)
- ✅ Consistent patterns across the codebase
