# Knowledge Base - Issue #614 Implementation

## Per Future Development Sessions

Questo documento contiene informazioni importanti apprese durante l'implementazione della issue #614 che saranno utili per future modifiche e enhancement.

---

## 🎯 Lezioni Apprese

### 1. Backend Merge Logic Already Exists

**Fatto Importante:**
Il backend già supporta il merge automatico delle righe duplicate tramite il flag `MergeDuplicateProducts` nel DTO `CreateDocumentRowDto`.

**Ubicazione Codice:**
- File: `EventForge.Server/Services/Documents/DocumentHeaderService.cs`
- Linee: 686-733
- Metodo: `AddDocumentRowAsync()`

**Implementazione:**
```csharp
if (createDto.MergeDuplicateProducts && createDto.ProductId.HasValue)
{
    var existingRow = await _context.DocumentRows
        .FirstOrDefaultAsync(r =>
            r.DocumentHeaderId == createDto.DocumentHeaderId &&
            r.ProductId == createDto.ProductId &&
            !r.IsDeleted);
    
    if (existingRow != null)
    {
        // Merge: sum base quantities and recalculate with conversion factor
        existingRow.BaseQuantity += baseQuantity.Value;
        // ... conversion logic ...
    }
}
```

**Implicazioni:**
- NON reimplementare questa logica
- Basta attivare il flag client-side
- La logica gestisce correttamente i conversion factor
- Transaction-safe e tenant-isolated

---

### 2. Build & Test Commands

**Comandi Verificati:**

```bash
# Build completo
dotnet build EventForge.sln --no-incremental

# Run tutti i test
dotnet test EventForge.Tests/EventForge.Tests.csproj

# Run test specifici (merge)
dotnet test EventForge.Tests/EventForge.Tests.csproj \
  --filter "FullyQualifiedName~DocumentRowMergeTests"

# Run test specifici (warehouse)
dotnet test EventForge.Tests/EventForge.Tests.csproj \
  --filter "FullyQualifiedName~Warehouse|FullyQualifiedName~Inventory"

# Check compilazione (veloce)
dotnet build EventForge.sln --no-incremental 2>&1 | grep -E "(Error|Build succeeded|Build FAILED)"
```

**Note:**
- `--no-incremental` assicura build pulita
- Tempo build completo: ~30-40 secondi
- Tempo test warehouse/inventory: ~18 secondi
- Tempo test completo: ~70 secondi

---

### 3. UI Component Patterns (Inventory/Warehouse)

**Standard Components:**

```razor
<!-- Collapsible Panel -->
<MudPaper Elevation="2" Class="pa-4 mb-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween">
        <MudIcon Icon="@Icons.Material.Outlined.Assignment" />
        <MudBadge Content="@count" Color="Color.Success">
            <MudIconButton Icon="@(_isExpanded ? ... : ...)" />
        </MudBadge>
    </MudStack>
    <MudCollapse Expanded="@_isExpanded">
        <!-- Content -->
    </MudCollapse>
</MudPaper>

<!-- Data Table (Dense) -->
<MudTable Items="@items" Dense="true" Striped="true" Hover="true">
    <HeaderContent>
        <MudTh>Column Name</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Value</MudTd>
    </RowTemplate>
</MudTable>

<!-- Status Chips -->
<MudChip T="string" Size="Size.Small" Color="Color.Info">
    Status Text
</MudChip>

<!-- Conversion Factor Display -->
@if (conversionFactor != 1m)
{
    <MudText Style="font-weight: 600; color: var(--mud-palette-warning);">
        x @conversionFactor.ToString("0.###")
    </MudText>
}
```

**Riferimenti:**
- `InventoryBarcodeAuditPanel.razor` - Esempio completo
- `CommonTrendWrapper.razor` - Pattern collapsible panel
- `InventoryProcedure.razor` - Pattern tabelle e form

---

### 4. Translation Service Pattern

**Come Usare:**

```csharp
@inject ITranslationService TranslationService

// In Razor
@TranslationService.GetTranslation("warehouse.key", "Default Text")

// In C# code
var text = TranslationService.GetTranslation("warehouse.key", "Default Text");
```

**Naming Convention:**
- Formato: `{area}.{feature}` o `{area}.{feature}{Detail}`
- Esempi:
  - `warehouse.barcodeAuditPanel` → "Codici Assegnati"
  - `warehouse.noBarcodeAssignments` → "Nessun codice..."
  - `products.productCode` → "Codice Prodotto"
  - `field.assignedAt` → "Assegnato il"

**Buone Pratiche:**
- Sempre fornire default text
- Usare chiavi lowercase con punti
- Raggruppare per area (warehouse, products, field, common, etc.)

---

### 5. Logging Best Practices

**Pattern Usato:**

```csharp
@inject ILogger<ComponentName> Logger

// Information (normal operations)
Logger.LogInformation(
    "Barcode {Barcode} assigned to product {ProductId} with factor {Factor}", 
    barcode, productId, conversionFactor);

// Warning (non-critical issues)
Logger.LogWarning(
    "Barcode assignments limit reached ({Limit}), removing oldest", 
    500);

// Error (critical issues)
Logger.LogError(ex, 
    "Error tracking barcode assignment for product {ProductId}", 
    productId);
```

**Regole:**
- Usare structured logging (parametri named)
- NO informazioni sensibili (password, token, etc.)
- Information: operazioni normali
- Warning: situazioni anomale ma gestibili
- Error: eccezioni e failure

---

### 6. DTO Validation Pattern

**Server-side:**
```csharp
public class AddInventoryDocumentRowDto
{
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [StringLength(200)]
    public string? Notes { get; set; }
    
    public bool MergeDuplicateProducts { get; set; } = false;
}
```

**Client-side:**
```razor
<MudForm @ref="_form" @bind-IsValid="@_isFormValid">
    <MudNumericField @bind-Value="_quantity"
                     Label="Quantity"
                     Required="true"
                     Min="0"
                     HelperText="Required" />
</MudForm>

<MudButton Disabled="@(!_isFormValid)">
    Submit
</MudButton>
```

**Regole:**
- Sempre validare sia client che server
- Client = UX, Server = security
- Usare DataAnnotations standard
- Default values per backward compatibility

---

### 7. Dynamic Type Usage (When Acceptable)

**Scenario:** Component compatibility senza duplicare classi

**Esempio:**
```csharp
// Component parameter
[Parameter]
public object? BarcodeAssignments { get; set; }

// Safe extraction
private IEnumerable<dynamic> GetAssignments()
{
    if (BarcodeAssignments == null)
        return Enumerable.Empty<dynamic>();
    
    try
    {
        return ((IEnumerable<dynamic>)BarcodeAssignments);
    }
    catch
    {
        return Enumerable.Empty<dynamic>();
    }
}
```

**Quando OK:**
- Dati interni (non external input)
- Wrapped in try-catch
- Component compatibility
- Alternative peggiori (code duplication)

**Quando NO:**
- External input (user, API)
- Business logic critical
- Security-sensitive operations

---

### 8. In-Memory Collections (Performance)

**Pattern Usato:**
```csharp
private List<BarcodeAssignmentInfo> _barcodeAssignments = new();

private void TrackItem(...)
{
    // Limit to prevent memory issues
    if (_barcodeAssignments.Count >= 500)
    {
        Logger.LogWarning("Limit reached, removing oldest");
        _barcodeAssignments.RemoveAt(0); // FIFO
    }
    
    _barcodeAssignments.Add(new BarcodeAssignmentInfo { ... });
}
```

**Regole:**
- Sempre avere un limite (es. 500 items)
- FIFO removal strategy (RemoveAt(0))
- Log quando limite raggiunto
- Considerare size medio item per calcolare max memory

**Calcolo Memory:**
- BarcodeAssignmentInfo size: ~200 bytes
- 500 items × 200 bytes = 100 KB
- Acceptable per client-side

---

### 9. Component Integration Pattern

**Parent Component:**
```csharp
// State management
private List<BarcodeAssignmentInfo> _barcodeAssignments = new();

// Child component integration
<InventoryBarcodeAuditPanel 
    BarcodeAssignments="_barcodeAssignments"
    OnViewProduct="@NavigateToProduct" />

// Event handler
private void NavigateToProduct(Guid productId)
{
    NavigationManager.NavigateTo($"/products/detail/{productId}");
}
```

**Child Component:**
```csharp
[Parameter]
public object? BarcodeAssignments { get; set; }

[Parameter]
public EventCallback<Guid> OnViewProduct { get; set; }

// Trigger parent event
await OnViewProduct.InvokeAsync(productId);
```

**Regole:**
- EventCallback per eventi up (child → parent)
- Parameter per dati down (parent → child)
- Parent gestisce navigation
- Child è stateless quando possibile

---

### 10. Error Handling Strategy

**Levels:**

1. **UI Level (Blazor):**
```csharp
try
{
    await PerformOperation();
    Snackbar.Add("Success!", Severity.Success);
}
catch (Exception ex)
{
    Logger.LogError(ex, "Operation failed");
    Snackbar.Add("Error occurred", Severity.Error);
    // NO throw - show friendly message
}
```

2. **Service Level:**
```csharp
public async Task<Result> PerformOperation()
{
    try
    {
        // Business logic
        return Result.Success();
    }
    catch (DbUpdateException ex)
    {
        Logger.LogError(ex, "Database error");
        return Result.Failure("Database error");
    }
}
```

3. **Controller Level:**
```csharp
[HttpPost]
public async Task<IActionResult> Action([FromBody] Dto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    try
    {
        var result = await _service.PerformOperation();
        return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Unhandled error");
        return StatusCode(500, "Internal server error");
    }
}
```

**Regole:**
- UI: catch + log + friendly message
- Service: catch specific + return Result
- Controller: catch all + return proper status code
- NEVER expose stack traces to client

---

### 11. Test Naming Convention

**Pattern:**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

**Esempi:**
```csharp
public async Task AddDocumentRowAsync_WithMerge_WhenDuplicateExists_UpdatesQuantity()
public void MergeDuplicateProducts_DefaultsToFalse()
public async Task TrackBarcodeAssignment_WhenLimitReached_RemovesOldest()
```

**Buone Pratiche:**
- Sempre AAA (Arrange-Act-Assert)
- Nome descrive scenario completo
- Un assertion per test (quando possibile)
- Test edge cases (0, null, max, etc.)

---

### 12. Database Query Optimization

**Pattern Merge Query:**
```csharp
var existingRow = await _context.DocumentRows
    .FirstOrDefaultAsync(r =>
        r.DocumentHeaderId == createDto.DocumentHeaderId &&
        r.ProductId == createDto.ProductId &&
        !r.IsDeleted,
        cancellationToken);
```

**Index Necessario:**
```sql
CREATE NONCLUSTERED INDEX IX_DocumentRows_Merge
ON DocumentRows (DocumentHeaderId, ProductId, IsDeleted)
INCLUDE (Quantity, BaseQuantity, UnitOfMeasureId);
```

**Perché:**
- Query frequente (ogni add row con merge)
- Covering index (no lookup needed)
- Drammaticamente migliora performance

**Measurement:**
```csharp
var sw = Stopwatch.StartNew();
var row = await _context.DocumentRows.FirstOrDefaultAsync(...);
sw.Stop();
Logger.LogInformation("Query took {Ms}ms", sw.ElapsedMilliseconds);
```

Target: < 10ms per merge query

---

### 13. Blazor Component Lifecycle

**Hooks Usati:**

```csharp
protected override void OnInitialized()
{
    // Component initialization
    // One-time setup
}

protected override async Task OnInitializedAsync()
{
    // Async initialization
    // API calls, data loading
}

protected override void OnParametersSet()
{
    // When parameters change
    // Recalculate derived state
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // First render only
        // JS interop, focus elements
    }
}
```

**Regole:**
- OnInitialized: synchronous setup
- OnInitializedAsync: async operations
- OnParametersSet: react to parameter changes
- OnAfterRenderAsync(firstRender): DOM operations

---

### 14. Security Checklist (Always)

Prima di committare qualsiasi modifica:

- [ ] Input validation (client + server)
- [ ] Output encoding (auto in Blazor)
- [ ] Authorization checks maintained
- [ ] No SQL injection (use EF Core)
- [ ] No sensitive data in logs
- [ ] Error messages don't expose internals
- [ ] No hardcoded secrets
- [ ] Tenant isolation verified

**Tools:**
```bash
# Security scan (if available)
dotnet tool install --global security-scan
security-scan . --ignore-msbuild-errors
```

---

## 🎓 Resources

### Documentazione Creata (Issue #614)
1. `ISSUE_614_COMPLETION_REPORT.md` - Technical deep dive
2. `GUIDA_UTENTE_ISSUE_614_IT.md` - User guide (IT)
3. `DEVELOPER_GUIDE_ISSUE_614.md` - Developer guide (EN)
4. `SECURITY_SUMMARY_ISSUE_614_MERGE_AUDIT.md` - Security review
5. `PR_SUMMARY_ISSUE_614.md` - PR summary
6. `KNOWLEDGE_BASE_ISSUE_614.md` - This document

### Codice Riferimento
- Backend merge: `DocumentHeaderService.cs:686-733`
- Client tracking: `InventoryProcedure.razor`
- Audit component: `InventoryBarcodeAuditPanel.razor`
- DTO: `AddInventoryDocumentRowDto.cs`
- Tests: `DocumentRowMergeTests.cs`, `AddInventoryDocumentRowDtoTests.cs`

### External Links
- [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
- [Blazor Docs](https://docs.microsoft.com/aspnet/core/blazor/)
- [MudBlazor Components](https://mudblazor.com/)
- [xUnit Testing](https://xunit.net/)

---

## 📝 Future Enhancements

### Identificate Durante Implementazione

1. **Server-side Audit Persistence**
   - Tabella BarcodeAssignmentAudit
   - API endpoint per query storico
   - Filtri avanzati

2. **Export CSV Audit Panel**
   - Bottone export
   - Formato CSV standard
   - Include tutti i campi

3. **Real-time User Info**
   - Username reale da auth context
   - Replace "Current User" placeholder

4. **Advanced Filtering**
   - Filter by code type
   - Filter by product
   - Filter by date range
   - Search box

5. **Batch Operations**
   - POST /api/documents/{id}/rows/batch
   - Riduce round trips
   - SignalR per real-time updates

---

**Last Updated:** 2025-11-20  
**Maintainer:** Development Team  
**Version:** 1.0  
**Status:** Production Ready
