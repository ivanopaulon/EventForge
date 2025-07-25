# EventForge Multi-Tenancy Implementation - Remaining Work

## Services Requiring Tenant Filtering Implementation

The following services need to be updated with ITenantContext injection and tenant filtering patterns following the examples in EventService, TeamService, and ProductService:

### Business Services
- [ ] **BusinessPartyService** - Customer/supplier management
  - Update GetBusinessPartiesAsync with WhereActiveTenant
  - Add tenant validation in create/update/delete operations
  - Filter related addresses and contacts by tenant

- [ ] **PaymentTermService** - Payment terms management
  - Update GetPaymentTermsAsync with WhereActiveTenant
  - Add tenant context validation

### Document Services
- [ ] **DocumentHeaderService** - Document management
  - Update GetDocumentHeadersAsync with WhereActiveTenant
  - Filter related DocumentRows by tenant
  - Ensure business party relationships respect tenant boundaries

- [ ] **DocumentTypeService** - Document type configuration
  - Update GetDocumentTypesAsync with WhereActiveTenant
  - Add tenant context validation

### Store Services  
- [ ] **StoreUserService** - Store user management
  - Update GetStoreUsersAsync with WhereActiveTenant
  - Filter user groups and privileges by tenant
  - Ensure cashier group assignments respect tenant boundaries

### Warehouse Services
- [ ] **StorageFacilityService** - Warehouse facility management
  - Update GetStorageFacilitiesAsync with WhereActiveTenant
  - Filter storage locations by tenant

- [ ] **StorageLocationService** - Storage location management
  - Update GetStorageLocationsAsync with WhereActiveTenant
  - Ensure warehouse relationships respect tenant boundaries

### Common Services
- [ ] **AddressService** - Address management
  - Update GetAddressesAsync with WhereActiveTenant
  - Handle polymorphic relationships (Bank, BusinessParty addresses)

- [ ] **ContactService** - Contact management
  - Update GetContactsAsync with WhereActiveTenant
  - Handle polymorphic relationships

- [ ] **ClassificationNodeService** - Classification hierarchy
  - Update GetClassificationNodesAsync with WhereActiveTenant
  - Maintain hierarchy integrity within tenants

- [ ] **ReferenceService** - Reference data
  - Update GetReferencesAsync with WhereActiveTenant
  - Handle polymorphic relationships

### Price List Services
- [ ] **PriceListService** - Price list management (already has some tenant awareness via Event relationship)
  - Verify tenant filtering is comprehensive
  - Update GetPriceListsAsync if needed

### Promotion Services
- [ ] **PromotionService** - Promotion management
  - Update GetPromotionsAsync with WhereActiveTenant
  - Filter promotion rules and rule products by tenant

### Station Monitor Services
- [ ] **StationService** - Station monitoring
  - Update GetStationsAsync with WhereActiveTenant
  - Filter station order queue items by tenant

### Unit of Measure Services
- [ ] **UMService** - Unit of measure management
  - Update GetUMsAsync with WhereActiveTenant

### VAT Rate Services
- [ ] **VatRateService** - VAT rate management
  - Update GetVatRatesAsync with WhereActiveTenant

### Bank Services
- [ ] **BankService** - Bank information management
  - Update GetBanksAsync with WhereActiveTenant
  - Filter related addresses and contacts by tenant

## Pattern to Follow

Each service should be updated following this pattern:

1. **Add using directive**: `using EventForge.Server.Extensions;`

2. **Inject ITenantContext**:
   ```csharp
   private readonly ITenantContext _tenantContext;
   
   public ServiceConstructor(..., ITenantContext tenantContext, ...)
   {
       _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
   }
   ```

3. **Update query methods** with tenant validation and filtering:
   ```csharp
   public async Task<PagedResult<EntityDto>> GetEntitiesAsync(...)
   {
       // TODO: Add automated tests for tenant isolation in [entity] queries
       var currentTenantId = _tenantContext.CurrentTenantId;
       if (!currentTenantId.HasValue)
       {
           throw new InvalidOperationException("Tenant context is required for [entity] operations.");
       }

       var query = _context.Entities
           .WhereActiveTenant(currentTenantId.Value)
           .Include(e => e.RelatedEntities.Where(r => !r.IsDeleted && r.TenantId == currentTenantId.Value));
       // ... rest of method
   }
   ```

4. **Update create operations** to set TenantId:
   ```csharp
   var entity = new Entity
   {
       TenantId = currentTenantId.Value,
       // ... other properties
   };
   ```

5. **Update single entity retrieval** with tenant filtering:
   ```csharp
   var entity = await _context.Entities
       .Where(e => e.Id == id && e.TenantId == currentTenantId.Value && !e.IsDeleted)
       .FirstOrDefaultAsync(cancellationToken);
   ```

## Additional Implementation Notes

### Polymorphic Relationships
Services handling polymorphic relationships (Address, Contact, Reference) need special attention:
- These entities are referenced by multiple parent types
- Tenant filtering should be applied based on the parent entity's tenant
- Consider adding helper methods for polymorphic tenant filtering

### Performance Considerations
- Add composite indexes for (TenantId, frequently_queried_fields) in future database optimizations
- Monitor query performance after tenant filtering implementation
- Consider read-only queries with AsNoTracking where appropriate

### Testing Requirements
Each updated service should include TODO comments for automated tests covering:
- Tenant data isolation
- Cross-tenant access prevention
- Proper tenant context validation
- Create/update operations set correct TenantId

## Global Query Filters (Future Enhancement)

Consider implementing global query filters at the DbContext level for automatic tenant filtering:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Add global tenant filters for all AuditableEntity types
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
        {
            var tenantFilter = GetTenantFilter(entityType.ClrType);
            if (tenantFilter != null)
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(tenantFilter);
            }
        }
    }
}
```

This would provide automatic tenant filtering at the database level, but requires careful implementation to handle super admin scenarios where cross-tenant access is needed.

## Validation Checklist

Before marking the multi-tenancy implementation complete, verify:

- [ ] All services inject ITenantContext and validate tenant context
- [ ] All queries filter by current tenant ID
- [ ] All create operations set TenantId from current context  
- [ ] All single entity retrievals include tenant filtering
- [ ] Related entity includes respect tenant boundaries
- [ ] TODO comments added for automated test coverage
- [ ] Documentation updated to reflect completed implementation
- [ ] Performance testing completed for tenant-filtered queries
- [ ] Security review completed for cross-tenant access prevention