# Startup Configuration and Validation

## Overview
This directory contains startup configuration and validation services that run during application initialization.

## DependencyValidationService

**Location**: `EventForge.Server/Startup/DependencyValidationService.cs`

**Purpose**: Validates the dependency injection container at application startup to detect circular dependencies early.

### What It Does

The DependencyValidationService performs comprehensive validation of the DI container:

1. **Extracts service descriptors** from IServiceProvider using reflection
2. **Builds dependency graph** by analyzing constructor parameters of each service
3. **Runs DFS algorithm** to detect cycles in the dependency graph
4. **Throws detailed error** with the complete cycle path if circular dependencies are found

### How It Works

```csharp
// Called during application startup in Program.cs
public static void ValidateDependencies(
    IServiceProvider services,
    ILogger? logger = null)
{
    logger?.LogInformation("Starting dependency validation...");

    var serviceDescriptors = GetServiceDescriptors(services);
    var graph = BuildDependencyGraph(serviceDescriptors);
    var cycles = DetectCycles(graph);
    
    if (cycles.Any())
    {
        var errorMessage = FormatCycleError(cycles);
        logger?.LogCritical("Circular dependencies detected:\n{ErrorMessage}", 
            errorMessage);
        throw new InvalidOperationException(
            $"Circular dependencies detected:\n{errorMessage}");
    }

    logger?.LogInformation(
        "Dependency validation completed. No circular dependencies found.");
}
```

### Example Error Output

When a circular dependency is detected, the service provides a clear diagnostic message:

```
Circular dependency detected:

DocumentHeaderService
  → IDocumentFacade
    → DocumentAnalyticsService
      → IDocumentHeaderService ❌ CYCLE!

Solution: Introduce an interface or refactor dependencies to break the cycle.
```

### Algorithm: Depth-First Search (DFS)

The validation uses DFS with two tracking structures:

1. **Visited Set**: Tracks all nodes visited during the entire traversal
2. **Recursion Stack**: Tracks the current path to detect back edges

**Pseudo-code**:
```
For each service in the DI container:
    If service not visited:
        Run DFS from service:
            Mark service as visited and in recursion stack
            For each dependency of service:
                If dependency in recursion stack:
                    CYCLE DETECTED! ❌
                If dependency not visited:
                    Recursively visit dependency
            Remove service from recursion stack
```

### Integration with Application Startup

The validation runs automatically during application startup:

```csharp
// In Program.cs
var app = builder.Build();

// Validate DI container after building
if (app.Environment.IsDevelopment() || 
    app.Environment.IsStaging())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    DependencyValidationService.ValidateDependencies(app.Services, logger);
}
```

### Benefits

- ✅ **Early detection**: Catches circular dependencies at startup, not runtime
- ✅ **Clear diagnostics**: Shows exact dependency path causing the cycle
- ✅ **Prevents production issues**: Fails fast in development/staging
- ✅ **Better architecture**: Encourages proper dependency design
- ✅ **Zero runtime overhead**: Only runs at startup

### Common Circular Dependency Patterns

#### Pattern 1: Direct Circular Dependency
```
Service A → Service B → Service A ❌
```

**Solution**: Extract interface
```
Service A → IServiceB (implemented by Service B)
Service B → IServiceA (implemented by Service A)
```

#### Pattern 2: Indirect Circular Dependency
```
Service A → Service B → Service C → Service A ❌
```

**Solution**: Refactor to shared service
```
Service A → Shared Service
Service B → Shared Service
Service C → Shared Service
```

#### Pattern 3: Facade Circular Dependency
```
DocumentFacade → DocumentService → IDocumentFacade ❌
```

**Solution**: Service should not depend on its own facade
```
DocumentFacade → DocumentService ✅
(DocumentService should not reference IDocumentFacade)
```

### Resolving Circular Dependencies

When the validator detects a cycle, use one of these strategies:

1. **Introduce an interface**
   - Extract shared functionality to an interface
   - Each service depends on the interface, not the concrete type

2. **Refactor to shared service**
   - Move common logic to a third service
   - Both services depend on the shared service

3. **Use events/mediator pattern**
   - Replace direct dependencies with event-driven communication
   - Services publish and subscribe to events

4. **Lazy initialization**
   - Use `Lazy<T>` or factory pattern
   - Delay dependency resolution until needed

5. **Redesign responsibilities**
   - Re-evaluate service boundaries
   - Split or merge services to eliminate circular references

### Performance Considerations

**Startup Impact**:
- Validation runs only once at application startup
- Typical validation time: 50-200ms for 100-300 services
- No runtime performance impact
- Can be disabled in production if needed (though not recommended)

**Memory Usage**:
- Builds temporary dependency graph in memory
- Graph is discarded after validation
- No ongoing memory overhead

### Configuration

The validation is enabled by default in Development and Staging environments. To configure:

```csharp
// Disable validation (not recommended)
// In Program.cs - remove or comment out validation call

// Enable in all environments
var logger = app.Services.GetRequiredService<ILogger<Program>>();
DependencyValidationService.ValidateDependencies(app.Services, logger);

// Enable only in specific environments
if (app.Environment.IsDevelopment() || 
    app.Environment.IsStaging() ||
    app.Environment.IsProduction())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    DependencyValidationService.ValidateDependencies(app.Services, logger);
}
```

### Troubleshooting

**Q: Validation is too slow**
- A: Consider disabling in production, keep in dev/staging
- A: Optimize by excluding certain service types if needed

**Q: False positives**
- A: Check if you're using `Lazy<T>` or factory patterns
- A: These might appear as cycles but aren't runtime issues

**Q: Validation fails in production but not locally**
- A: Different services may be registered in different environments
- A: Ensure consistent DI registration across environments

## Architecture Documentation

For more details on DI patterns and standards:
- [DEPENDENCY_INJECTION_PATTERNS.md](../../docs/architecture/DEPENDENCY_INJECTION_PATTERNS.md) - Comprehensive DI patterns guide
- [SERVICE_LAYER_STANDARDS.md](../../docs/architecture/SERVICE_LAYER_STANDARDS.md) - Service implementation standards

## Summary

The DependencyValidationService ensures:
- ✅ Clean dependency architecture
- ✅ Early detection of circular dependencies
- ✅ Clear error messages for quick resolution
- ✅ Better overall code quality

This validation is a key part of EventForge's quality assurance strategy, preventing architectural issues before they reach production.
