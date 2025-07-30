# EventForge Controller Extension Pattern Guide

This document outlines the standardized patterns for creating and extending controllers in EventForge after the multi-tenant refactoring.

## 🏗️ Controller Structure Pattern

### Basic Multi-Tenant Controller Template

```csharp
using EventForge.DTOs.{Domain};
using EventForge.Server.Services.{Domain};
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for {entity} management with multi-tenant support.
/// Provides CRUD operations for {entities} within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class {Entity}Controller : BaseApiController
{
    private readonly I{Entity}Service _{entity}Service;
    private readonly ITenantContext _tenantContext;

    public {Entity}Controller(I{Entity}Service {entity}Service, ITenantContext tenantContext)
    {
        _{entity}Service = {entity}Service ?? throw new ArgumentNullException(nameof({entity}Service));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    // Methods follow patterns below...
}
```

## 📋 Method Patterns

### 1. GET Collection with Pagination

```csharp
/// <summary>
/// Gets all {entities} with optional pagination.
/// </summary>
/// <param name="page">Page number (1-based)</param>
/// <param name="pageSize">Number of items per page</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Paginated list of {entities}</returns>
/// <response code="200">Returns the paginated list of {entities}</response>
/// <response code="400">If the query parameters are invalid</response>
/// <response code="403">If the user doesn't have access to the current tenant</response>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<{Entity}Dto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<PagedResult<{Entity}Dto>>> Get{Entities}(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    // Validate pagination parameters
    var paginationError = ValidatePaginationParameters(page, pageSize);
    if (paginationError != null) return paginationError;

    // Validate tenant access
    var tenantError = await ValidateTenantAccessAsync(_tenantContext);
    if (tenantError != null) return tenantError;

    try
    {
        var result = await _{entity}Service.Get{Entities}Async(page, pageSize, cancellationToken);
        return Ok(result);
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("An error occurred while retrieving {entities}.", ex);
    }
}
```

### 2. GET Single Entity

```csharp
/// <summary>
/// Gets a {entity} by ID.
/// </summary>
/// <param name="id">{Entity} identifier</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>{Entity} details</returns>
/// <response code="200">Returns the {entity}</response>
/// <response code="404">If the {entity} is not found</response>
/// <response code="403">If the user doesn't have access to the current tenant</response>
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<{Entity}Dto>> Get{Entity}(
    Guid id,
    CancellationToken cancellationToken = default)
{
    // Validate tenant access
    var tenantError = await ValidateTenantAccessAsync(_tenantContext);
    if (tenantError != null) return tenantError;

    try
    {
        var {entity} = await _{entity}Service.Get{Entity}ByIdAsync(id, cancellationToken);

        if ({entity} == null)
            return CreateNotFoundProblem($"{Entity} with ID {id} not found.");

        return Ok({entity});
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("An error occurred while retrieving the {entity}.", ex);
    }
}
```

### 3. POST Create Entity

```csharp
/// <summary>
/// Creates a new {entity}.
/// </summary>
/// <param name="createDto">{Entity} creation data</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Created {entity}</returns>
/// <response code="201">Returns the created {entity}</response>
/// <response code="400">If the request data is invalid</response>
/// <response code="403">If the user doesn't have access to the current tenant</response>
[HttpPost]
[ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<{Entity}Dto>> Create{Entity}(
    [FromBody] Create{Entity}Dto createDto,
    CancellationToken cancellationToken = default)
{
    if (!ModelState.IsValid)
        return CreateValidationProblemDetails();

    // Validate tenant access
    var tenantError = await ValidateTenantAccessAsync(_tenantContext);
    if (tenantError != null) return tenantError;

    try
    {
        var currentUser = GetCurrentUser();
        var {entity} = await _{entity}Service.Create{Entity}Async(createDto, currentUser, cancellationToken);

        return CreatedAtAction(
            nameof(Get{Entity}),
            new { id = {entity}.Id },
            {entity});
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("An error occurred while creating the {entity}.", ex);
    }
}
```

### 4. PUT Update Entity

```csharp
/// <summary>
/// Updates an existing {entity}.
/// </summary>
/// <param name="id">{Entity} identifier</param>
/// <param name="updateDto">{Entity} update data</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Updated {entity}</returns>
/// <response code="200">Returns the updated {entity}</response>
/// <response code="400">If the request data is invalid</response>
/// <response code="404">If the {entity} is not found</response>
/// <response code="403">If the user doesn't have access to the current tenant</response>
[HttpPut("{id:guid}")]
[ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<{Entity}Dto>> Update{Entity}(
    Guid id,
    [FromBody] Update{Entity}Dto updateDto,
    CancellationToken cancellationToken = default)
{
    if (!ModelState.IsValid)
        return CreateValidationProblemDetails();

    // Validate tenant access
    var tenantError = await ValidateTenantAccessAsync(_tenantContext);
    if (tenantError != null) return tenantError;

    try
    {
        var currentUser = GetCurrentUser();
        var {entity} = await _{entity}Service.Update{Entity}Async(id, updateDto, currentUser, cancellationToken);

        if ({entity} == null)
            return CreateNotFoundProblem($"{Entity} with ID {id} not found.");

        return Ok({entity});
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("An error occurred while updating the {entity}.", ex);
    }
}
```

### 5. DELETE Soft Delete Entity

```csharp
/// <summary>
/// Deletes a {entity} (soft delete).
/// </summary>
/// <param name="id">{Entity} identifier</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>No content on successful deletion</returns>
/// <response code="204">{Entity} successfully deleted</response>
/// <response code="404">If the {entity} is not found</response>
/// <response code="403">If the user doesn't have access to the current tenant</response>
[HttpDelete("{id:guid}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> Delete{Entity}(
    Guid id,
    CancellationToken cancellationToken = default)
{
    // Validate tenant access
    var tenantError = await ValidateTenantAccessAsync(_tenantContext);
    if (tenantError != null) return tenantError;

    try
    {
        var currentUser = GetCurrentUser();
        var deleted = await _{entity}Service.Delete{Entity}Async(id, currentUser, cancellationToken);

        if (!deleted)
            return CreateNotFoundProblem($"{Entity} with ID {id} not found.");

        return NoContent();
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("An error occurred while deleting the {entity}.", ex);
    }
}
```

## 🚫 Special Controller Types (No Tenant Context)

### Infrastructure Controllers

Some controllers intentionally exclude tenant context:

#### 1. Authentication Controllers
```csharp
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    // No ITenantContext - authentication happens before tenant context
}
```

#### 2. Health Check Controllers
```csharp
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : BaseApiController
{
    // No ITenantContext - health checks are infrastructure-level
}
```

#### 3. Super Admin Controllers
```csharp
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : BaseApiController
{
    // May or may not have ITenantContext depending on whether
    // super admin operations are tenant-specific
}
```

## 🔧 Error Handling Patterns

### Use BaseApiController Methods

Always use the helper methods from `BaseApiController`:

```csharp
// Validation errors
return CreateValidationProblemDetails();
return CreateValidationProblemDetails("Custom validation message");

// Not found errors
return CreateNotFoundProblem($"Entity with ID {id} not found.");

// Internal server errors
return CreateInternalServerErrorProblem("Custom error message", ex);

// Pagination validation
var paginationError = ValidatePaginationParameters(page, pageSize);
if (paginationError != null) return paginationError;

// Tenant access validation
var tenantError = await ValidateTenantAccessAsync(_tenantContext);
if (tenantError != null) return tenantError;
```

## 📚 Documentation Standards

### XML Documentation Requirements

All public methods must include:

```csharp
/// <summary>
/// Brief description of what the method does.
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Description of return value</returns>
/// <response code="200">Success case description</response>
/// <response code="400">Validation error case</response>
/// <response code="403">Authorization error case</response>
/// <response code="404">Not found case (if applicable)</response>
```

### OpenAPI Attributes

Always include appropriate `ProducesResponseType` attributes:

```csharp
[ProducesResponseType(typeof(ReturnType), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
```

## 🧪 Testing Considerations

When creating new controllers, ensure:

1. **Multi-tenant validation tests** - Verify tenant isolation
2. **Error handling tests** - Verify RFC7807 compliance
3. **Authorization tests** - Verify access control
4. **Pagination tests** - Verify parameter validation
5. **Business logic tests** - Verify core functionality

## 🔄 Migration Checklist

When updating existing controllers:

- [ ] Add `ITenantContext` dependency injection
- [ ] Add tenant validation to all business methods
- [ ] Replace old error formats with `BaseApiController` helpers
- [ ] Update XML documentation with tenant-specific notes
- [ ] Add `ProducesResponseType` for 403 Forbidden
- [ ] Use `ValidatePaginationParameters` for pagination
- [ ] Use `GetCurrentUser()` instead of manual user extraction
- [ ] Update route documentation if needed

## 📖 Examples

See the following controllers for complete examples:

- `PriceListsController` - Full CRUD with multi-tenant support
- `BusinessPartiesController` - Complex business entity example
- `ProductsController` - Product management example with multiple entity types
- `EventsController` - Event management with soft delete filtering

## 🔍 Code Review Guidelines

When reviewing new or updated controllers:

1. Verify tenant isolation is properly implemented
2. Check that all error responses use RFC7807 format
3. Ensure XML documentation is complete and accurate
4. Verify OpenAPI response types are correctly specified
5. Check that authorization attributes are appropriate
6. Ensure cancellation tokens are properly propagated