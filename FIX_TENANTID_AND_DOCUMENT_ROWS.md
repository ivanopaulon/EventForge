# Fix: TenantId Assignment and Document Row Retrieval

## Problem Statement (Italian)
> controlla la procedura di inventario nuovamente, manca la corretta valorizzazione del campo tenantid sia nella testa che nelle righe del documento, correggi questo comportamente, inoltre controlla i servizi per il recupero dei documenti, non recupera correttametne le righe

**Translation:**
Check the inventory procedure again, the correct assignment of the tenantid field is missing in both the document header and rows, fix this behavior, also check the services for document retrieval, it doesn't correctly retrieve rows.

## Issues Identified

### 1. Missing TenantId in Document Header Creation
**File:** `EventForge.Server/Services/Documents/DocumentHeaderService.cs`

**Problem:**
- `DocumentHeaderService` did not have access to `ITenantContext`
- `CreateDocumentHeaderAsync` method created document headers without setting `TenantId`
- This broke multi-tenancy isolation

### 2. Missing TenantId in Document Row Creation (Inline)
**File:** `EventForge.Server/Services/Documents/DocumentHeaderService.cs`

**Problem:**
- When creating document rows inline with the header (lines 123-132), the `TenantId` was not set
- Only the `AddDocumentRowAsync` method correctly set `TenantId` from parent document
- This created orphaned rows without proper tenant context

### 3. Document Rows Not Mapped in ToDto
**File:** `EventForge.Server/Extensions/MappingExtensions.cs`

**Problem:**
- The `DocumentHeader.ToDto()` mapping method did NOT include the `Rows` property
- Even though `DocumentHeaderDto` has a `Rows` property (line 311), it was never populated
- This caused `GetDocumentHeaderByIdAsync` to return headers without rows, even when `includeRows: true`

## Solutions Implemented

### 1. Added ITenantContext to DocumentHeaderService

**Changes in `DocumentHeaderService.cs`:**

```csharp
// BEFORE
public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentHeaderService> _logger;

    public DocumentHeaderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentHeaderService> logger)
    {
        // ...
    }
}

// AFTER
public class DocumentHeaderService : IDocumentHeaderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;  // ✅ Added
    private readonly ILogger<DocumentHeaderService> _logger;

    public DocumentHeaderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,  // ✅ Added
        ILogger<DocumentHeaderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));  // ✅ Added
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### 2. Fixed TenantId Assignment in CreateDocumentHeaderAsync

**Changes in `CreateDocumentHeaderAsync` method:**

```csharp
// BEFORE
public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
    CreateDocumentHeaderDto createDto,
    string currentUser,
    CancellationToken cancellationToken = default)
{
    try
    {
        var documentHeader = createDto.ToEntity();
        documentHeader.CreatedBy = currentUser;  // ❌ TenantId NOT set
        documentHeader.CreatedAt = DateTime.UtcNow;

        _ = _context.DocumentHeaders.Add(documentHeader);

        if (createDto.Rows?.Any() == true)
        {
            foreach (var rowDto in createDto.Rows)
            {
                var row = rowDto.ToEntity();
                row.DocumentHeaderId = documentHeader.Id;
                row.CreatedBy = currentUser;  // ❌ TenantId NOT set
                row.CreatedAt = DateTime.UtcNow;
                documentHeader.Rows.Add(row);
            }
        }
        // ...
    }
}

// AFTER
public async Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
    CreateDocumentHeaderDto createDto,
    string currentUser,
    CancellationToken cancellationToken = default)
{
    try
    {
        // ✅ Get TenantId from context
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("Cannot create document header without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var documentHeader = createDto.ToEntity();
        documentHeader.TenantId = tenantId.Value;  // ✅ Set TenantId
        documentHeader.CreatedBy = currentUser;
        documentHeader.CreatedAt = DateTime.UtcNow;

        _ = _context.DocumentHeaders.Add(documentHeader);

        if (createDto.Rows?.Any() == true)
        {
            foreach (var rowDto in createDto.Rows)
            {
                var row = rowDto.ToEntity();
                row.DocumentHeaderId = documentHeader.Id;
                row.TenantId = tenantId.Value;  // ✅ Set TenantId
                row.CreatedBy = currentUser;
                row.CreatedAt = DateTime.UtcNow;
                documentHeader.Rows.Add(row);
            }
        }
        // ...
    }
}
```

### 3. Fixed Document Rows Mapping in ToDto

**Changes in `MappingExtensions.cs`:**

```csharp
// BEFORE
public static DocumentHeaderDto ToDto(this DocumentHeader entity)
{
    return new DocumentHeaderDto
    {
        Id = entity.Id,
        DocumentTypeId = entity.DocumentTypeId,
        DocumentTypeName = entity.DocumentType?.Name,
        // ... other properties ...
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        ModifiedAt = entity.ModifiedAt,
        ModifiedBy = entity.ModifiedBy
        // ❌ Rows property NOT mapped
    };
}

// AFTER
public static DocumentHeaderDto ToDto(this DocumentHeader entity)
{
    return new DocumentHeaderDto
    {
        Id = entity.Id,
        DocumentTypeId = entity.DocumentTypeId,
        DocumentTypeName = entity.DocumentType?.Name,
        // ... other properties ...
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        ModifiedAt = entity.ModifiedAt,
        ModifiedBy = entity.ModifiedBy,
        Rows = entity.Rows?.Select(r => r.ToDto()).ToList()  // ✅ Added Rows mapping
    };
}
```

## Impact Analysis

### Multi-Tenancy
- **Before:** Document headers and rows were created without TenantId, breaking tenant isolation
- **After:** All documents and rows are properly scoped to the current tenant
- **Security:** Multi-tenancy is now properly enforced

### Document Retrieval
- **Before:** `GetDocumentHeaderByIdAsync` with `includeRows: true` returned headers without rows
- **After:** Document headers now correctly include their rows in the DTO
- **API Behavior:** Inventory documents and other document types now return complete data

### Inventory Procedure
- **Before:** Inventory documents were created without proper tenant context
- **After:** Inventory documents (header + rows) are correctly associated with the tenant
- **Data Integrity:** No more orphaned records

## Testing

### Build Status
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Results
```
Passed!  - Failed: 0, Passed: 214, Skipped: 0, Total: 214
```

All existing tests continue to pass, confirming backward compatibility.

## Files Modified

1. **EventForge.Server/Services/Documents/DocumentHeaderService.cs**
   - Added ITenantContext dependency injection
   - Modified CreateDocumentHeaderAsync to set TenantId on header and rows

2. **EventForge.Server/Extensions/MappingExtensions.cs**
   - Modified DocumentHeader.ToDto() to include Rows mapping

## Verification Steps

To verify the fixes work correctly:

1. **Create an inventory document:**
   ```
   POST /api/v1/warehouse/inventory/document/start
   ```
   - Verify documentHeader.TenantId is set correctly
   - Verify the response includes the correct tenant context

2. **Add rows to inventory document:**
   ```
   POST /api/v1/warehouse/inventory/document/{documentId}/row
   ```
   - Verify row.TenantId is set correctly
   - Verify it matches the document header's TenantId

3. **Retrieve an inventory document:**
   ```
   GET /api/v1/warehouse/inventory/document/{documentId}
   ```
   - Verify the response includes all rows
   - Verify each row has complete data (ProductId, LocationId, etc.)

4. **Database verification:**
   ```sql
   SELECT Id, TenantId FROM DocumentHeaders WHERE Id = '{documentId}'
   SELECT Id, DocumentHeaderId, TenantId FROM DocumentRows WHERE DocumentHeaderId = '{documentId}'
   ```
   - Verify TenantId is populated in both tables
   - Verify TenantId values match between header and rows

## Notes

- The `AddDocumentRowAsync` method already correctly set TenantId from parent document (line 572)
- This fix ensures consistency across all document creation paths
- The solution follows the existing pattern used in other services (e.g., DocumentTypeService)

## Related Documentation

- `INVENTORY_DOCUMENT_BEFORE_AFTER_COMPARISON.md` - Documents previous inventory improvements
- `AuditableEntity.cs` - Base class that defines TenantId field
- `ITenantContext` interface - Provides current tenant context

## Conclusion

The fixes ensure:
1. ✅ TenantId is correctly set in document headers
2. ✅ TenantId is correctly set in document rows (both inline and via AddDocumentRowAsync)
3. ✅ Document retrieval correctly returns rows
4. ✅ Multi-tenancy isolation is maintained
5. ✅ All existing tests pass
6. ✅ No breaking changes introduced
