# SHIFTS_AUDIT.md — Cash Register Operator Shift Management

## Existing Relevant Entities

### Server — `EventForge.Server/Data/Entities/Store/`

| Entity | File | Notes |
|---|---|---|
| `StoreUser` | `StoreUser.cs` | The "Operator/Cashier" entity. Already has `IsOnShift: bool` and `ShiftId: Guid?` fields. Keyed by `Guid` (from `AuditableEntity`). |
| `StorePos` | `StorePos.cs` | The "Register/POS" entity. Referenced as `RegisterId` in the spec → should be `PosId: Guid?`. |
| `FiscalDrawer` | `FiscalDrawer.cs` | Has FK to `StoreUser` (OperatorId). Shows FK pattern. |
| `FiscalDrawerSession` | `FiscalDrawerSession.cs` | Closest existing analogue to a shift: has `OpenedAt`, `ClosedAt`, `Status`, `Notes`, FK to `StoreUser` x2. Shows session/shift pattern. |
| **`CashierShift`** | — | **Does NOT exist.** Must be created. |

### Base Class — `AuditableEntity`
All entities inherit from `AuditableEntity` which provides:
- `Guid Id` (PK, `NEWSEQUENTIALID()`)
- `Guid TenantId` (multi-tenant, required)
- `DateTime CreatedAt`, `string? CreatedBy`
- `DateTime? ModifiedAt`, `string? ModifiedBy`
- `bool IsDeleted`, `DateTime? DeletedAt`, `string? DeletedBy`
- `bool IsActive`
- `byte[]? RowVersion` (optimistic concurrency)

> **CRITICAL**: Task spec uses `int Id` and `int OperatorId` — these MUST be adapted to `Guid` to match all existing conventions.

### Authentication — `EventForge.Server/Data/Entities/Auth/`
JWT-based auth. Roles: `SuperAdmin`, `Admin`, `Manager`, `StoreManager`.
Authorization policies in `ServiceCollectionExtensions.cs`:
- `RequireUser` — any authenticated user
- `RequireAdmin` — Admin, SuperAdmin
- `RequireManager` — Admin, Manager, SuperAdmin
- `RequireStoreConfig` — Admin, Manager, StoreManager, SuperAdmin

---

## Server-Side Gaps

| Gap | File to Create / Modify |
|---|---|
| Missing `CashierShift` entity | CREATE `EventForge.Server/Data/Entities/Store/CashierShift.cs` |
| Missing `ShiftStatus` enum | CREATE in `Prym.DTOs/Store/CashierShiftDto.cs` (shared with client) |
| Missing `DbSet<CashierShift>` | MODIFY `EventForge.Server/Data/EventForgeDbContext.cs` |
| Missing EF relationship config | CREATE `EventForge.Server/Data/EventForgeDbContext.Shifts.cs` + call in `OnModelCreating` |
| Missing SQL migration | CREATE `Migrations/20260416_AddCashierShifts.sql` + rollback |
| Missing `IShiftService` / `ShiftService` | CREATE in `EventForge.Server/Services/Store/` |
| Missing `ShiftsController` | CREATE `EventForge.Server/Controllers/ShiftsController.cs` |
| Missing DTOs | CREATE `Prym.DTOs/Store/CashierShiftDto.cs` |
| Missing DI registration | MODIFY `EventForge.Server/Extensions/ServiceCollectionExtensions.cs` |

---

## Client-Side Gaps

| Gap | File to Create / Modify |
|---|---|
| Missing `IShiftService` (client) | CREATE `Prym.Web/Services/Store/IShiftService.cs` |
| Missing `ShiftService` (client) | CREATE `Prym.Web/Services/Store/ShiftService.cs` |
| Missing Shift CRUD management page | CREATE `Prym.Web/Pages/Management/Store/ShiftManagement.razor` |
| Missing Shift calendar page | CREATE `Prym.Web/Pages/Management/Store/ShiftCalendar.razor` |
| Missing Shift detail dialog | CREATE `Prym.Web/Shared/Components/Dialogs/Store/ShiftDetailDialog.razor` |
| Missing client DI registration | MODIFY `Prym.Web/Program.cs` |
| Missing nav menu entries | MODIFY `Prym.Web/Layout/NavMenu.razor` |
| Missing `_Imports.razor` using | MODIFY `Prym.Web/_Imports.razor` (add Shifts dialogs namespace if needed) |

---

## Dependencies and Risks

| Item | Risk | Mitigation |
|---|---|---|
| `StoreUser.ShiftId` vs new `CashierShift` | StoreUser already has `ShiftId: Guid?` — this field should reference a `CashierShift` but the FK is not configured yet | The shift entity will use Guid PK; `ShiftId` on StoreUser can optionally point to it via convention but no FK constraint is added to avoid breaking changes |
| Task spec uses `int` IDs | All entities use `Guid` | Adapt all IDs to `Guid` in the implementation |
| `RegisterId` in spec | No concept of "Register" exists; closest is `StorePos` | Use `Guid? PosId` referencing `StorePos` |
| Migration pattern | Project uses raw SQL files in `/Migrations/` for documentation, NOT EF Core code-first migration C# files (the `Migrations/` folder in `EventForge.Server.csproj` is empty) | Create raw SQL migration file + rollback file in `/Migrations/` folder; also add `ConfigureShiftRelationships` in DbContext partial |
| Syncfusion `SfSchedule` | Already imported and used in `EventManagement.razor` | Reuse identical pattern from `EventManagement.razor` |
| Dialog pattern | Existing dialogs use `ViewModel` classes; shift dialog will be simpler | Use inline component state (no ViewModel) for the shift dialog to keep it minimal |
| `DateOnly` in service interface | Task spec uses `DateOnly from, DateOnly to` but API query params need special handling | Use `DateOnly` with `[FromQuery]` and `DateOnlyTypeConverter`, or use `DateTime` at API level and convert |

---

## Recommended Architectural Decisions

1. **Entity key**: Use `Guid` (from `AuditableEntity`), named `Id`. FK to `StoreUser` → `Guid StoreUserId`. FK to `StorePos` → `Guid? PosId`.

2. **ShiftStatus enum**: Define in `Prym.DTOs/Store/CashierShiftDto.cs` (shared DTO project) so both server and client see the same enum without a project reference issue.

3. **DTOs**: Use class-based DTOs (not records) following existing convention (`StoreUserDto`, `FiscalDrawerDto`, etc.). Named: `CashierShiftDto`, `CreateCashierShiftDto`, `UpdateCashierShiftDto`.

4. **Service method signatures**: Use `DateOnly` for `from`/`to` parameters in service interface. At the API layer, receive as `string` or `DateTime` and convert, to avoid `DateOnly` serialization issues with older `System.Text.Json` setups.

5. **Migration**: Create `Migrations/20260416_AddCashierShifts.sql` (raw SQL, matching existing project convention). Also create the corresponding `ROLLBACK_20260416_AddCashierShifts.sql`.

6. **Controller route**: `[Route("api/v1/shifts")]` following kebab-case convention (`fiscal-drawers`, not `FiscalDrawers`).

7. **Authorization**: Apply `[Authorize(Policy = "RequireStoreConfig")]` on write operations (POST/PUT/DELETE), `[Authorize]` on read operations — matching `StoreUsersController` pattern.

8. **Client service DI**: Register with `AddHttpClient<IShiftService, ShiftService>(...).AddHttpMessageHandler<AuthenticatedHttpClientHandler>()`.

9. **Calendar page**: Use `SfSchedule<SchedulerShiftItem>` with a local model mapping from `CashierShiftDto`. Use `OnPopupOpen` to intercept Syncfusion popups and open MudBlazor dialog instead — matching the `EventManagement.razor` pattern.

10. **Nav routes**: `/store/shifts` (CRUD list) and `/store/shifts/calendar` (Syncfusion calendar) — consistent with `/store/operators`, `/store/fiscal-drawers`.
