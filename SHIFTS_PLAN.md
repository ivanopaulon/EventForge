# SHIFTS_PLAN.md — Cash Register Operator Shift Management

## Step 1 — Entities and DB Migration

**Dependencies**: none

### Files to CREATE:
- `EventForge.Server/Data/Entities/Store/CashierShift.cs`
  - Inherits from `AuditableEntity` (Guid PK, TenantId, audit fields)
  - `Guid StoreUserId`, nav `StoreUser StoreUser`
  - `Guid? PosId`, nav `StorePos? Pos`
  - `DateTime ShiftStart`, `DateTime ShiftEnd`
  - `ShiftStatus Status` (default Scheduled)
  - `string? Notes` (MaxLength 500)
- `Migrations/20260416_AddCashierShifts.sql`
  - Creates `CashierShifts` table with all `AuditableEntity` columns
  - Indexes on `StoreUserId`, `ShiftStart`, `ShiftEnd`, `TenantId`
  - FK to `StoreUsers(Id)`, FK to `StorePoses(Id)` (SET NULL)
- `Migrations/ROLLBACK_20260416_AddCashierShifts.sql`
  - Drops FK constraints then drops table

### Files to MODIFY:
- `EventForge.Server/Data/EventForgeDbContext.cs`
  - Add `public DbSet<CashierShift> CashierShifts { get; set; }` in Store Entities region
  - Add call `ConfigureShiftRelationships(modelBuilder);` in `OnModelCreating`
- `EventForge.Server/Data/EventForgeDbContext.Shifts.cs` *(new partial)*
  - Create new partial file with `ConfigureShiftRelationships` private static method
  - Configure FK: StoreUser (Restrict), StorePos (SetNull)
  - Configure indexes on StoreUserId, ShiftStart, ShiftEnd

---

## Step 2 — Service Layer

**Dependencies**: Step 1 (entities must exist)

### Files to CREATE:
- `Prym.DTOs/Store/CashierShiftDto.cs`
  - `ShiftStatus` enum: `Scheduled, InProgress, Completed, Cancelled`
  - `CashierShiftDto` class: `Guid Id`, `Guid StoreUserId`, `string StoreUserName`, `Guid? PosId`, `string? PosName`, `DateTime ShiftStart`, `DateTime ShiftEnd`, `ShiftStatus Status`, `string? Notes`, `DateTime CreatedAt`
  - `CreateCashierShiftDto`: `Guid StoreUserId`, `Guid? PosId`, `DateTime ShiftStart`, `DateTime ShiftEnd`, `string? Notes`
  - `UpdateCashierShiftDto`: `Guid? PosId`, `DateTime ShiftStart`, `DateTime ShiftEnd`, `ShiftStatus Status`, `string? Notes`
- `EventForge.Server/Services/Store/IShiftService.cs`
  - `GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct)`
  - `GetShiftByIdAsync(Guid id, CancellationToken ct)`
  - `CreateShiftAsync(CreateCashierShiftDto dto, string currentUser, CancellationToken ct)`
  - `UpdateShiftAsync(Guid id, UpdateCashierShiftDto dto, string currentUser, CancellationToken ct)`
  - `DeleteShiftAsync(Guid id, string currentUser, CancellationToken ct)`
  - `GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct)`
- `EventForge.Server/Services/Store/ShiftService.cs`
  - Primary constructor: `EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<ShiftService> logger`
  - Implement all interface methods following `FiscalDrawerService` patterns
  - Use `WhereActiveTenant()`, `RequireTenantId()` helpers
  - Manual mapping (`MapToDto` private static method)
  - Validate shift overlap on create/update

### Files to MODIFY:
- `EventForge.Server/Extensions/ServiceCollectionExtensions.cs`
  - Add `_ = services.AddScoped<IShiftService, ShiftService>();` in the Store section

---

## Step 3 — API Controller and DTOs

**Dependencies**: Step 2 (service interface must exist)

### Files to CREATE:
- `EventForge.Server/Controllers/ShiftsController.cs`
  - `[Route("api/v1/shifts")]`
  - `[Authorize]` on class
  - Constructor: `IShiftService shiftService, ITenantContext tenantContext`
  - `GET /api/v1/shifts?from=&to=` → `GetShiftsAsync` (`[Authorize]`)
  - `GET /api/v1/shifts/{id:guid}` → `GetShiftByIdAsync` (`[Authorize]`)
  - `POST /api/v1/shifts` → `CreateShiftAsync` (`[Authorize(Policy="RequireStoreConfig")]`)
  - `PUT /api/v1/shifts/{id:guid}` → `UpdateShiftAsync` (`[Authorize(Policy="RequireStoreConfig")]`)
  - `DELETE /api/v1/shifts/{id:guid}` → `DeleteShiftAsync` (`[Authorize(Policy="RequireStoreConfig")]`)
  - `GET /api/v1/shifts/operator/{storeUserId:guid}?from=&to=` → `GetShiftsByOperatorAsync` (`[Authorize]`)
  - Returns `ProblemDetails` on error, uses `CreateNotFoundProblem`, `CreateInternalServerErrorProblem`

---

## Step 4 — Blazor HTTP Client Service

**Dependencies**: Step 3 (API endpoints must exist)

### Files to CREATE:
- `Prym.Web/Services/Store/IShiftService.cs` *(client)*
  - `GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct)`
  - `GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct)`
  - `GetByIdAsync(Guid id, CancellationToken ct)`
  - `CreateAsync(CreateCashierShiftDto dto, CancellationToken ct)`
  - `UpdateAsync(Guid id, UpdateCashierShiftDto dto, CancellationToken ct)`
  - `DeleteAsync(Guid id, CancellationToken ct)`
- `Prym.Web/Services/Store/ShiftService.cs` *(client)*
  - `const string ApiBase = "api/v1/shifts"`
  - Constructor: `HttpClient httpClient, ILogger<ShiftService> logger`
  - Implement all interface methods following `FiscalDrawerService` client pattern
  - Use `GetFromJsonAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`

### Files to MODIFY:
- `Prym.Web/Program.cs`
  - Add: `builder.Services.AddHttpClient<IShiftService, ShiftService>(...)..AddHttpMessageHandler<AuthenticatedHttpClientHandler>()`

---

## Step 5 — Shift Management Page (CRUD)

**Dependencies**: Step 4 (client service must be registered)

### Files to CREATE:
- `Prym.Web/Pages/Management/Store/ShiftManagement.razor`
  - Route: `@page "/store/shifts"`
  - `@attribute [Authorize]`
  - Uses `MudTable` with `ServerData` or client-side list
  - Filter panel: `MudDatePicker` for from/to range, `MudSelect` for operator filter
  - Columns: Operator Name, POS, Shift Start, Shift End, Status (MudChip with color), Notes
  - Actions: Edit button → `ShiftDetailDialog`, Delete button with confirmation
  - Toolbar: "New Shift" button → `ShiftDetailDialog`
  - Status chip colors: Scheduled=Info, InProgress=Warning, Completed=Success, Cancelled=Error
  - Follows `FiscalDrawerManagement.razor` structural pattern
- `Prym.Web/Shared/Components/Dialogs/Store/ShiftDetailDialog.razor`
  - `[CascadingParameter] IMudDialogInstance MudDialog { get; set; }`
  - `[Parameter] public Guid? ShiftId { get; set; }` (null = create)
  - Inject `IShiftService`, `IStoreUserService` (operator selector), `IStorePosService` (POS selector)
  - `MudForm` with fields: MudSelect for operator, MudSelect for POS, MudDateTimePicker for start/end, MudSelect for Status (edit mode only), MudTextField for Notes
  - Buttons: Cancel (`Variant.Text`) and Save (`Variant.Filled, Color.Primary`)
  - Uses `EFDialog` wrapper component

### Files to MODIFY:
- None for this step (dialog/page are new files)

---

## Step 6 — Shift Calendar Page (Syncfusion Scheduler)

**Dependencies**: Step 4 (client service must be registered)

### Files to CREATE:
- `Prym.Web/Pages/Management/Store/ShiftCalendar.razor`
  - Route: `@page "/store/shifts/calendar"`
  - `@attribute [Authorize]`
  - Local model `SchedulerShiftItem` with string properties: `Id`, `Subject`, `StartTime`, `EndTime`, `StatusCssClass`, `Description`
  - Maps `CashierShiftDto` to `SchedulerShiftItem` (ShiftStart→StartTime, ShiftEnd→EndTime, StoreUserName→Subject)
  - `SfSchedule<SchedulerShiftItem>` with Day/Week/Month views
  - `ScheduleEventSettings` with `IdField`, `SubjectField`, `StartTimeField`, `EndTimeField`
  - `ScheduleEvents` with `OnPopupOpen` to intercept and open MudBlazor dialog
  - `OnEventRendered` to color events by ShiftStatus
  - Toolbar: date range filter and operator selector for loading data
  - Reactively refreshes after dialog close

### Files to MODIFY:
- `Prym.Web/Layout/NavMenu.razor`
  - Add two `MudNavLink` items inside the "Configurazione Store" `MudNavGroup`:
    - `/store/shifts` → "Turni" with `Icons.Material.Outlined.Schedule`
    - `/store/shifts/calendar` → "Calendario Turni" with `Icons.Material.Outlined.CalendarMonth`

---

## Step 7 — Integration Testing and Verification

**Dependencies**: All previous steps

### Checks:
1. Build `EventForge.Server` project — no compilation errors
2. Build `Prym.Web` project — no compilation errors
3. Build `Prym.DTOs` project — no compilation errors
4. Verify `EventForge.Tests` still build and pass
5. Verify SQL migration file is syntactically valid T-SQL
6. Verify no existing routes are modified (only `/api/v1/shifts*` added)
7. Verify nav links are gated behind `_isStoreManager || _isAdmin || _isManager || _isSuperAdmin`
8. Run `parallel_validation` for code review and CodeQL scan

### Files Summary (all new unless noted):

| File | Action |
|---|---|
| `EventForge.Server/Data/Entities/Store/CashierShift.cs` | CREATE |
| `EventForge.Server/Data/EventForgeDbContext.Shifts.cs` | CREATE |
| `EventForge.Server/Data/EventForgeDbContext.cs` | MODIFY |
| `Migrations/20260416_AddCashierShifts.sql` | CREATE |
| `Migrations/ROLLBACK_20260416_AddCashierShifts.sql` | CREATE |
| `Prym.DTOs/Store/CashierShiftDto.cs` | CREATE |
| `EventForge.Server/Services/Store/IShiftService.cs` | CREATE |
| `EventForge.Server/Services/Store/ShiftService.cs` | CREATE |
| `EventForge.Server/Extensions/ServiceCollectionExtensions.cs` | MODIFY |
| `EventForge.Server/Controllers/ShiftsController.cs` | CREATE |
| `Prym.Web/Services/Store/IShiftService.cs` | CREATE |
| `Prym.Web/Services/Store/ShiftService.cs` | CREATE |
| `Prym.Web/Program.cs` | MODIFY |
| `Prym.Web/Pages/Management/Store/ShiftManagement.razor` | CREATE |
| `Prym.Web/Shared/Components/Dialogs/Store/ShiftDetailDialog.razor` | CREATE |
| `Prym.Web/Pages/Management/Store/ShiftCalendar.razor` | CREATE |
| `Prym.Web/Layout/NavMenu.razor` | MODIFY |
