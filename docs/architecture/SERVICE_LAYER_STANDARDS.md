# Service Layer Standards - EventForge

## Overview
This document defines the coding standards and best practices for service layer implementations in EventForge. All service classes should follow these patterns for consistency, performance, and maintainability.

## Standard Service Template

```csharp
public class MyEntityService : IMyEntityService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MyEntityService> _logger;

    public MyEntityService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<MyEntityService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // READ operation example
    public async Task<MyEntityDto?> GetByIdAsync(
        Guid id, 
        CancellationToken ct = default)
    {
        try
        {
            var entity = await _context.MyEntities
                .AsNoTracking() // ✅ Read-only
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync(ct);

            return entity != null ? MapToDto(entity) : null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetByIdAsync cancelled for {EntityId}", id);
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity {EntityId}", id);
            throw;
        }
    }

    // WRITE operation example
    public async Task<MyEntityDto> CreateAsync(
        CreateMyEntityDto createDto,
        string currentUser,
        CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = new MyEntity
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.CurrentTenantId 
                    ?? throw new InvalidOperationException("Tenant required"),
                Name = createDto.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.MyEntities.Add(entity); // ✅ Tracking enabled
            await _context.SaveChangesAsync(ct);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                nameof(MyEntity),
                entity.Id,
                "Entity",
                "Create",
                null,
                entity.Name,
                currentUser,
                $"Entity '{entity.Name}' created"
            );

            return MapToDto(entity);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CreateAsync cancelled for {EntityName}", createDto.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity");
            throw;
        }
    }

    // UPDATE operation example
    public async Task<MyEntityDto> UpdateAsync(
        Guid id,
        UpdateMyEntityDto updateDto,
        string currentUser,
        CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            // Must load with tracking for updates
            var entity = await _context.MyEntities
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (entity == null)
                throw new NotFoundException($"Entity {id} not found");

            var oldValue = entity.Name;
            
            // Update properties
            entity.Name = updateDto.Name;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = currentUser;

            await _context.SaveChangesAsync(ct);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                nameof(MyEntity),
                entity.Id,
                "Entity",
                "Update",
                oldValue,
                entity.Name,
                currentUser,
                $"Entity '{entity.Name}' updated"
            );

            return MapToDto(entity);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UpdateAsync cancelled for {EntityId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity {EntityId}", id);
            throw;
        }
    }

    // LIST operation example with projection
    public async Task<PagedResult<MyEntityDto>> GetPagedAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            var query = _context.MyEntities
                .AsNoTracking() // ✅ Read-only
                .Where(e => !e.IsDeleted);

            var totalCount = await query.CountAsync(ct);

            // SQL-side projection (Projection-First pattern)
            var items = await query
                .OrderBy(e => e.Name)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(e => new MyEntityDto  // ✅ Project in SQL
                {
                    Id = e.Id,
                    Name = e.Name,
                    // Only needed fields
                })
                .ToListAsync(ct);

            return new PagedResult<MyEntityDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetPagedAsync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged entities");
            throw;
        }
    }

    private static MyEntityDto MapToDto(MyEntity entity)
    {
        return new MyEntityDto
        {
            Id = entity.Id,
            Name = entity.Name,
            // Map other properties
        };
    }
}
```

## Mandatory Patterns

### 1. CancellationToken Parameter
**ALL** async methods MUST accept `CancellationToken ct = default` as the last parameter.

**✅ CORRECT**:
```csharp
Task<ProductDto> GetAsync(Guid id, CancellationToken ct = default);
```

**❌ WRONG**:
```csharp
Task<ProductDto> GetAsync(Guid id); // Missing CancellationToken
```

### 2. AsNoTracking for Read Operations
Use `.AsNoTracking()` for ALL read-only queries.

**✅ CORRECT**:
```csharp
return await _context.Products
    .AsNoTracking()
    .Where(p => p.Id == id)
    .FirstOrDefaultAsync(ct);
```

**❌ WRONG**:
```csharp
return await _context.Products
    .Where(p => p.Id == id)
    .FirstOrDefaultAsync(ct); // Missing AsNoTracking
```

### 3. Projection-First Pattern
Project data in SQL, not in memory.

**✅ CORRECT - SQL projection**:
```csharp
return await _context.Products
    .Select(p => new ProductDto { Id = p.Id, Name = p.Name })
    .ToListAsync(ct);
```

**❌ WRONG - Memory projection**:
```csharp
var products = await _context.Products.ToListAsync(ct);
return products.Select(p => new ProductDto { Id = p.Id, Name = p.Name }).ToList();
```

### 4. Constructor Null Checks
ALL constructor parameters MUST be null-checked.

**✅ CORRECT**:
```csharp
public MyService(IRepository repo, ILogger<MyService> logger)
{
    _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**❌ WRONG**:
```csharp
public MyService(IRepository repo, ILogger<MyService> logger)
{
    _repo = repo;  // No null check
    _logger = logger;  // No null check
}
```

### 5. Tenant Isolation
ALL queries MUST filter by tenant (where applicable).

**✅ CORRECT**:
```csharp
var products = await _context.Products
    .Where(p => p.TenantId == _tenantContext.CurrentTenantId)
    .ToListAsync(ct);
```

**❌ WRONG**:
```csharp
var products = await _context.Products
    .ToListAsync(ct); // Missing tenant filter - SECURITY ISSUE!
```

### 6. Audit Logging for Mutations
ALL create/update/delete operations MUST log audit entries.

**✅ CORRECT**:
```csharp
await _context.SaveChangesAsync(ct);
await _auditLogService.LogEntityChangeAsync(
    nameof(Product), id, "Product", "Update", 
    oldValue, newValue, currentUser, description);
```

**❌ WRONG**:
```csharp
await _context.SaveChangesAsync(ct);
// Missing audit log - COMPLIANCE ISSUE!
```

## Exception Handling Rules

### 1. OperationCanceledException
Log as **Information**, then re-throw.

```csharp
catch (OperationCanceledException)
{
    _logger.LogInformation("Operation cancelled for {EntityId}", id);
    throw; // Re-throw to propagate cancellation
}
```

### 2. Business Validation Exceptions
Log as **Warning**, then re-throw.

```csharp
catch (BusinessValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for {EntityId}", id);
    throw; // Let controller handle validation errors
}
```

### 3. All Other Exceptions
Log as **Error** with context, then re-throw.

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing entity {EntityId}", id);
    throw; // Let error middleware handle
}
```

## Performance Checklist

When writing service methods, ensure:

- [ ] **CancellationToken** parameter on all async methods
- [ ] **AsNoTracking()** on read-only queries
- [ ] **Projection-first** (Select before ToList)
- [ ] **Structured logging** with parameters (not string interpolation)
- [ ] **Tenant isolation** enforced in queries
- [ ] **Audit logging** for mutations
- [ ] **Null checks** in constructor
- [ ] **Try-catch** with appropriate logging levels

## Logging Best Practices

### Use Structured Logging
**✅ CORRECT**:
```csharp
_logger.LogError(ex, "Error retrieving product {ProductId} for tenant {TenantId}", 
    productId, tenantId);
```

**❌ WRONG**:
```csharp
_logger.LogError(ex, $"Error retrieving product {productId} for tenant {tenantId}");
```

### Use Appropriate Log Levels
- **LogTrace**: Detailed flow tracking (rarely used)
- **LogDebug**: Diagnostic information for developers
- **LogInformation**: General flow of application
- **LogWarning**: Abnormal or unexpected events
- **LogError**: Errors and exceptions
- **LogCritical**: Failures requiring immediate attention

## Common Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: Loading entire entity for projection
```csharp
// BAD: Loads all columns, then projects
var products = await _context.Products.ToListAsync();
return products.Select(p => new ProductDto { Id = p.Id }).ToList();
```

**✅ Solution**:
```csharp
// GOOD: Projects in SQL
return await _context.Products
    .Select(p => new ProductDto { Id = p.Id })
    .ToListAsync(ct);
```

### ❌ Anti-Pattern 2: Multiple database round trips
```csharp
// BAD: N+1 query problem
foreach (var order in orders)
{
    order.Customer = await _context.Customers.FindAsync(order.CustomerId);
}
```

**✅ Solution**:
```csharp
// GOOD: Single query with Include
var orders = await _context.Orders
    .Include(o => o.Customer)
    .ToListAsync(ct);
```

### ❌ Anti-Pattern 3: Synchronous database calls
```csharp
// BAD: Blocking call
var product = _context.Products.Find(id);
```

**✅ Solution**:
```csharp
// GOOD: Async call
var product = await _context.Products.FindAsync(id, ct);
```

## Summary

Following these service layer standards ensures:
- ✅ **Consistent code quality** across the codebase
- ✅ **Better performance** through AsNoTracking and projection
- ✅ **Improved cancellation support** via CancellationToken
- ✅ **Enhanced security** through tenant isolation
- ✅ **Better compliance** via audit logging
- ✅ **Easier debugging** through structured logging
