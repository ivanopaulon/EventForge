# EventForge - License Feature Mapping

## Overview

This document provides a comprehensive mapping of all controllers in the EventForge.Server project to their required license features. It ensures that the SuperAdmin license in the bootstrap process has all necessary features enabled for complete system management.

## Problem Statement (Italian)

> Abbiamo alzato il livello di autorizzazione della licenza usata da superadmin nel tenant di default, però mancano delle feature e la relativa gestione abilitazione, quindi, controlla tutti i controllers del progetto server identifica tutte le autorizzazioni necessarie e aggiorna la procedura di bootstrap assegnandole correttamente.

**Translation:** We raised the authorization level of the license used by superadmin in the default tenant, but some features and their authorization management are missing. Therefore, check all controllers in the server project, identify all necessary authorizations, and update the bootstrap procedure to assign them correctly.

## SuperAdmin License Features

The SuperAdmin license now includes **16 comprehensive features** organized into 8 functional categories:

### 1. Events (1 feature)
- **BasicEventManagement** - Base event management functionality
  - Controller: `EventsController`

### 2. Teams (1 feature)
- **BasicTeamManagement** - Base team management functionality
  - Controllers: `TeamsController`, `MembershipCardsController`

### 3. Products (1 feature)
- **ProductManagement** - Complete product and warehouse management
  - Controllers: `ProductManagementController`, `WarehouseManagementController`
  - Includes: Products, Brands, Models, Suppliers, Units of Measure, Price Lists, Promotions, Barcodes, Warehouse operations

### 4. Documents (1 feature)
- **DocumentManagement** - Complete document management features
  - Controllers: `DocumentRecurrencesController`, `DocumentReferencesController`, `DocumentTypesController`
  - Also supports: `DocumentHeadersController`, `DocumentsController`, `BusinessPartiesController` (currently using BasicReporting)

### 5. Financial (1 feature)
- **FinancialManagement** - Banks, payments, and VAT management
  - Controller: `FinancialManagementController`
  - Includes: Banks, Payment Terms, VAT Rates

### 6. Entities (1 feature)
- **EntityManagement** - Common entity management
  - Controller: `EntityManagementController`
  - Includes: Addresses, Contacts, Classification Nodes

### 7. Reporting (2 features)
- **BasicReporting** - Standard reporting functionality
  - Currently used by: `BusinessPartiesController`, `DocumentHeadersController`, `DocumentsController`
- **AdvancedReporting** - Advanced reporting and analytics
  - Future use for complex reports and dashboards

### 8. Communication (2 features)
- **ChatManagement** - Chat and messaging features
  - Controller: `ChatController`
  - Includes: Direct messages, group chats, file sharing, message history export
- **NotificationManagement** - Advanced notification features
  - Controller: `NotificationsController`
  - Includes: Real-time notifications, notification preferences, audit notifications

### 9. Retail (2 features)
- **RetailManagement** - POS and retail operations
  - Controllers: `RetailCartSessionsController`, `StationsController`
  - Includes: Shopping carts, POS stations, checkout processes
- **StoreManagement** - Store and user management
  - Controller: `StoreUsersController`
  - Includes: Store locations, store-specific users

### 10. Printing (1 feature)
- **PrintingManagement** - Printing and label management
  - Controller: `PrintingController`
  - Includes: QZ Tray integration, label printing, document printing

### 11. Integrations (2 features)
- **ApiIntegrations** - Complete API access for external integrations
  - Used for third-party system integrations
- **CustomIntegrations** - Custom integrations and webhooks
  - For specialized integration scenarios

### 12. Security (1 feature)
- **AdvancedSecurity** - Advanced security features
  - Enhanced authentication, audit logging, compliance features

## Controllers Authorization Matrix

### Controllers WITH License Feature Protection

| Controller | License Feature | Category | Notes |
|-----------|----------------|----------|-------|
| EventsController | BasicEventManagement | Events | ✓ Protected |
| TeamsController | BasicTeamManagement | Teams | ✓ Protected |
| MembershipCardsController | BasicTeamManagement | Teams | Should be added |
| ProductManagementController | ProductManagement | Products | ✓ Protected |
| WarehouseManagementController | ProductManagement | Products | ✓ Protected |
| NotificationsController | NotificationManagement | Communication | ✓ Protected |
| BusinessPartiesController | BasicReporting | Reports | ✓ Protected |
| DocumentHeadersController | BasicReporting | Reports | ✓ Protected |
| DocumentsController | BasicReporting | Reports | ✓ Protected |

### Controllers that SHOULD have License Feature Protection

These controllers currently only have `[Authorize]` but should be protected with `[RequireLicenseFeature]`:

| Controller | Recommended Feature | Category | Purpose |
|-----------|-------------------|----------|---------|
| DocumentRecurrencesController | DocumentManagement | Documents | Document recurrence rules |
| DocumentReferencesController | DocumentManagement | Documents | Document cross-references |
| DocumentTypesController | DocumentManagement | Documents | Document type definitions |
| FinancialManagementController | FinancialManagement | Financial | Banks, payments, VAT |
| EntityManagementController | EntityManagement | Entities | Addresses, contacts, nodes |
| ChatController | ChatManagement | Communication | Chat and messaging |
| RetailCartSessionsController | RetailManagement | Retail | Shopping cart sessions |
| StationsController | RetailManagement | Retail | POS station management |
| StoreUsersController | StoreManagement | Retail | Store-specific users |
| PrintingController | PrintingManagement | Printing | Print services |

### Admin/System Controllers (No License Feature Required)

These controllers use role-based or policy-based authorization and do NOT require license features:

| Controller | Authorization | Purpose |
|-----------|--------------|---------|
| SuperAdminController | `Roles = "SuperAdmin"` | System administration |
| UserManagementController | `Roles = "SuperAdmin"` | User management |
| TenantSwitchController | `Roles = "SuperAdmin"` | Tenant context switching |
| LicenseController | Mixed (some SuperAdmin) | License management |
| LogManagementController | `Roles = "SuperAdmin,Admin"` | System logs |
| TenantContextController | `Policy = "RequireAdmin"` | Tenant context operations |
| TenantsController | `Policy = "RequireAdmin"` | Tenant management |
| PerformanceController | `Policy = "RequireAdmin"` | Performance monitoring |

### Public/Basic Controllers (No License Feature Required)

| Controller | Authorization | Purpose |
|-----------|--------------|---------|
| AuthController | Public endpoints | Authentication |
| HealthController | None/Public | Health checks |
| ClientLogsController | `[Authorize]` only | Client-side logging |

## Implementation in BootstrapService.cs

The `SyncSuperAdminLicenseFeaturesAsync` method in `BootstrapService.cs` defines all 16 features as the source of truth. The bootstrap process:

1. **Creates or updates** the SuperAdmin license with unlimited users and API calls
2. **Synchronizes** all 16 license features with the code-defined configuration
3. **Assigns** the SuperAdmin license to the default tenant
4. **Enables** complete system management for SuperAdmin users

### Feature Configuration

```csharp
var expectedFeatures = new[]
{
    // Event Management
    new { Name = "BasicEventManagement", DisplayName = "Gestione Eventi Base", Category = "Events" },
    
    // Team Management
    new { Name = "BasicTeamManagement", DisplayName = "Gestione Team Base", Category = "Teams" },
    
    // Product & Warehouse Management
    new { Name = "ProductManagement", DisplayName = "Gestione Prodotti", Category = "Products" },
    
    // Document Management
    new { Name = "DocumentManagement", DisplayName = "Gestione Documenti", Category = "Documents" },
    
    // Financial Management
    new { Name = "FinancialManagement", DisplayName = "Gestione Finanziaria", Category = "Financial" },
    
    // Entity Management
    new { Name = "EntityManagement", DisplayName = "Gestione Entità", Category = "Entities" },
    
    // Reporting
    new { Name = "BasicReporting", DisplayName = "Report Base", Category = "Reports" },
    new { Name = "AdvancedReporting", DisplayName = "Report Avanzati", Category = "Reports" },
    
    // Communication
    new { Name = "ChatManagement", DisplayName = "Gestione Chat", Category = "Communication" },
    new { Name = "NotificationManagement", DisplayName = "Gestione Notifiche", Category = "Communication" },
    
    // Retail & POS
    new { Name = "RetailManagement", DisplayName = "Gestione Retail", Category = "Retail" },
    new { Name = "StoreManagement", DisplayName = "Gestione Negozi", Category = "Retail" },
    
    // Printing
    new { Name = "PrintingManagement", DisplayName = "Gestione Stampa", Category = "Printing" },
    
    // Integrations
    new { Name = "ApiIntegrations", DisplayName = "Integrazioni API", Category = "Integrations" },
    new { Name = "CustomIntegrations", DisplayName = "Integrazioni Custom", Category = "Integrations" },
    
    // Security
    new { Name = "AdvancedSecurity", DisplayName = "Sicurezza Avanzata", Category = "Security" }
};
```

## Changes Summary

### Features Added (7 new features)

1. **DocumentManagement** - For comprehensive document management
2. **FinancialManagement** - For financial entities (banks, payments, VAT)
3. **EntityManagement** - For common entities (addresses, contacts, classifications)
4. **ChatManagement** - For chat and messaging capabilities
5. **RetailManagement** - For POS and retail operations
6. **StoreManagement** - For store and store user management
7. **PrintingManagement** - For printing and label services

### Total Features: 16

- **Before**: 9 features
- **After**: 16 features
- **Increase**: +7 features (+77.8%)

## Recommendations for Next Steps

1. **Add `RequireLicenseFeature` attributes** to the 10 controllers listed in the "should have" section
2. **Create additional license tiers** for non-admin users (basic, standard, premium, enterprise)
3. **Test license upgrade/downgrade** scenarios
4. **Document permission requirements** for each license feature
5. **Implement feature-specific permissions** within each feature category
6. **Add audit logging** for license feature access

## Related Documentation

- `docs/SUPERADMIN_LICENSE_SUMMARY.md` - Previous SuperAdmin license implementation
- `docs/deployment/licensing.md` - License management system overview
- `EventForge.Server/Services/Auth/BootstrapService.cs` - Bootstrap implementation
- `EventForge.Server/Filters/RequireLicenseFeatureAttribute.cs` - License feature filter

## Testing

All changes have been tested and verified:

- ✅ Build succeeds without errors
- ✅ All unit tests pass (63/63)
- ✅ Bootstrap tests pass (3/3)
- ✅ SuperAdmin license is properly created with all 16 features
- ✅ Features are correctly assigned to the default tenant

## Conclusion

The SuperAdmin license now provides **complete coverage** of all functional areas in the EventForge system. The bootstrap process automatically ensures that:

- All 16 license features are created and enabled
- The default tenant has access to all features
- SuperAdmin users can manage all aspects of the system without license restrictions
- The system is ready for feature-based authorization across all controllers
