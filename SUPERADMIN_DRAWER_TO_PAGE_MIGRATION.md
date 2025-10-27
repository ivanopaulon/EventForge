# SuperAdmin Drawer to Page Migration

## Overview

This document tracks the migration of SuperAdmin management pages from drawer-based editing to full-page detail views, following the pattern established in PRs #487 and #488.

## Pattern Applied

Each detail page implements:
- **Route-based create/edit mode detection**: `/superadmin/{entity}/new` and `/superadmin/{entity}/{id}`
- **JSON snapshot-based unsaved changes detection**: Compares current form state with original snapshot
- **Navigation guards**: Save/Discard/Cancel dialog when navigating away with unsaved changes
- **Header with actions**: Back button, entity name, unsaved indicator, save button
- **Full-page form layout**: Replaces drawer component with dedicated page

## Migration Status

### ✅ Completed

#### TenantManagement → TenantDetail
- **Routes**: `/superadmin/tenants/new` | `/superadmin/tenants/{TenantId:guid}`
- **Form Fields**: Name, Code, DisplayName, Description, Domain, ContactEmail, MaxUsers
- **Features**:
  - Create/edit mode detection
  - User count display for existing tenants
  - Form validation with required fields
  - Unsaved changes tracking
  - Navigation guards
- **Files Modified**:
  - Created: `EventForge.Client/Pages/SuperAdmin/TenantDetail.razor`
  - Updated: `EventForge.Client/Pages/SuperAdmin/TenantManagement.razor`
    - Removed `TenantDrawer` component
    - Changed `OpenCreateTenantDrawer()` → `CreateTenant()` (navigation)
    - Changed `EditTenant(TenantResponseDto)` → `EditTenant(Guid)` (navigation)
    - Removed `ViewTenant()` (Edit serves both purposes)
    - Removed drawer state variables and callback methods
    - Updated ActionButtonGroup to not show View button

### 📋 Remaining Work

#### LicenseManagement → LicenseDetail
- **Routes**: `/superadmin/licenses/new` | `/superadmin/licenses/{LicenseId:guid}`
- **Form Fields**: Name, DisplayName, Description, MaxUsers, MaxApiCallsPerMonth, TierLevel, IsActive, Features
- **Reference**: `EventForge.Client/Shared/Components/LicenseDrawer.razor`
- **Complexity**: Medium - includes feature management
- **Estimated Effort**: 2-3 hours

#### UserManagement → UserDetail (SuperAdmin context)
- **Routes**: `/superadmin/users/new` | `/superadmin/users/{UserId:guid}`  
- **Form Fields**: Username, Email, FirstName, LastName, Roles, TenantId, IsActive
- **Reference**: User management in `EventForge.Client/Pages/SuperAdmin/UserManagement.razor`
- **Complexity**: High - multi-tenant user management with role assignment
- **Estimated Effort**: 4-6 hours
- **Recommendation**: Consider separate PR due to complexity

## Implementation Guide

### Step 1: Create Detail Page

```razor
@page "/superadmin/{entity}/new"
@page "/superadmin/{entity}/{EntityId:guid}"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.*
@using EventForge.Client.Shared.Components
@attribute [Authorize(Roles = "SuperAdmin")]
@inject ISuperAdminService SuperAdminService
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<EntityDetail> Logger

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    @if (_isLoading) { /* Loading indicator */ }
    else if (_entity == null && !_isCreateMode) { /* Not found alert */ }
    else
    {
        <!-- Page Header with back button, title, unsaved indicator, save button -->
        <!-- Form Section with MudForm and field validation -->
    }
</MudContainer>

@code {
    [Parameter] public Guid? EntityId { get; set; }
    
    private EntityDto? _entity;
    private bool _isCreateMode => EntityId == null || EntityId == Guid.Empty;
    private bool _hasLocalChanges = false;
    private string _originalSnapshot = string.Empty;
    
    // Implement: LoadEntityAsync, SaveEntityAsync, HasUnsavedChanges, TryNavigateAway
}
```

### Step 2: Update Management Page

```csharp
// REMOVE drawer component and references
// <EntityDrawer @ref="_drawer" ... />

// REMOVE drawer state variables
// private EntityDrawerMode _drawerMode = EntityDrawerMode.Create;
// private EntityDto? _selectedEntity;

// CHANGE Create method
private void CreateEntity()
{
    NavigationManager.NavigateTo("/superadmin/{entity}/new");
}

// CHANGE Edit method  
private void EditEntity(Guid id)
{
    NavigationManager.NavigateTo($"/superadmin/{entity}/{id}");
}

// REMOVE View method (Edit serves both purposes now)
// REMOVE callback methods (OnEntityCreated, OnEntityUpdated)

// UPDATE ActionButtonGroup
<ActionButtonGroup ShowView="false"  // Changed from true
                   ShowEdit="true"
                   OnEdit="@(() => EditEntity(context.Id))" />  // Changed signature
```

### Step 3: Testing Checklist

- [ ] Build succeeds without errors
- [ ] Navigation to create page works (click Create button)
- [ ] Navigation to edit page works (click Edit button on table row)
- [ ] Form fields populate correctly in edit mode
- [ ] Form validation works (required fields, format validation)
- [ ] Save button creates/updates entity successfully
- [ ] Back button navigates to management page
- [ ] Unsaved changes dialog appears when navigating away with changes
- [ ] Save from unsaved changes dialog works
- [ ] Discard from unsaved changes dialog works
- [ ] Cancel from unsaved changes dialog keeps user on page

## Service Methods Reference

### ISuperAdminService

**Tenant Operations:**
- `GetTenantsAsync()` → `IEnumerable<TenantResponseDto>`
- `GetTenantAsync(Guid id)` → `TenantResponseDto?`
- `CreateTenantAsync(CreateTenantDto)` → `TenantResponseDto`
- `UpdateTenantAsync(Guid id, UpdateTenantDto)` → `TenantResponseDto`
- `DeleteTenantAsync(Guid id, string reason)`
- `EnableTenantAsync(Guid id, string reason)`
- `DisableTenantAsync(Guid id, string reason)`

**User Operations:**
- `GetUsersAsync(Guid? tenantId)` → `IEnumerable<UserManagementDto>`
- `GetUserAsync(Guid id)` → `UserManagementDto?`
- `CreateUserAsync(CreateUserManagementDto)` → `UserManagementDto`
- `UpdateUserAsync(Guid id, UpdateUserManagementDto)` → `UserManagementDto`
- `DeleteUserAsync(Guid id)`
- `ResetUserPasswordAsync(Guid id, ResetPasswordDto)` → `PasswordResetResultDto`

### ILicenseService

**License Operations:**
- `GetLicensesAsync()` → `IEnumerable<LicenseDto>`
- `GetLicenseAsync(Guid id)` → `LicenseDto?`
- `CreateLicenseAsync(CreateLicenseDto)` → `LicenseDto`
- `UpdateLicenseAsync(Guid id, UpdateLicenseDto)` → `LicenseDto`
- `DeleteLicenseAsync(Guid id)`
- `GetLicenseFeaturesAsync(Guid licenseId)` → `IEnumerable<LicenseFeatureDto>`

## DTO Reference

### Tenant DTOs
- **TenantResponseDto**: For reading tenant data (includes all fields + metadata)
- **CreateTenantDto**: For creating new tenants (Name, Code, DisplayName, Description, Domain, ContactEmail, MaxUsers)
- **UpdateTenantDto**: For updating tenants (DisplayName, Description, Domain, ContactEmail, MaxUsers, SubscriptionExpiresAt)

### License DTOs
- **LicenseDto**: For reading license data
- **CreateLicenseDto**: For creating new licenses
- **UpdateLicenseDto**: For updating licenses

### User DTOs
- **UserManagementDto**: For reading user data in SuperAdmin context
- **CreateUserManagementDto**: For creating new users
- **UpdateUserManagementDto**: For updating users

## Benefits of This Migration

1. **Better UX**: Full-page forms provide more space and better context
2. **URL-based navigation**: Users can bookmark specific entities, browser back/forward works
3. **Consistent patterns**: Matches ProductDetail, BrandDetail, and other entity detail pages
4. **Easier maintenance**: Dedicated pages are easier to understand and modify than drawer components
5. **Better mobile experience**: Full-page forms work better on mobile devices than drawers

## Notes

- The migration maintains all existing functionality while improving the UX
- No backend API changes required - only UI/frontend changes
- All validation, permissions, and business logic remain unchanged
- The pattern is consistent with the drawer-to-page migrations in PRs #487 and #488
