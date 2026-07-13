# Findings — Scansione sistemica tenant isolation (Parte C, PROMPT_P01_P02)

Prodotto in esecuzione di `PROMPT_P01_P02_TENANT_ISOLATION_WAREHOUSE_READS.md`, Parte C.
Scope: `EventForge.Server/Services/` **esclusi** i file di `Services/Warehouse/` (corretti in questo stesso intervento) e i service già corretti nei precedenti interventi (PROMPT_21/PROMPT_22 — Documents, Teams, PriceLists, Products/Promotions Level 1 write paths, RetailCart, BackupService, WarehouseFacade, StorageFacilityService, DocumentAnalyticsService).

Questi gap **non sono stati corretti** in questo intervento (fuori scope, richiedono approvazione esplicita per un intervento dedicato). Sono elencati qui come nuovo finding per un audit successivo.

## Gap individuati

- `EventForge.Server/Services/Business/BusinessPartyService.cs:466` — `GetBusinessPartyAccountingByIdAsync`: interroga `context.BusinessPartyAccountings` filtrando solo per `Id` e `!IsDeleted`, senza mai referenziare `tenantContext`. `BusinessPartyAccounting` eredita `AuditableEntity` (ha `TenantId`).
- `EventForge.Server/Services/Products/ProductService.cs:202` — `GetProductByIdAsync`: interroga `context.Products` filtrando solo per `Id` e `!IsDeleted`, senza filtro `TenantId` sul prodotto stesso (nel metodo `tenantContext` viene usato solo per una query correlata su `BusinessParties`, non per il prodotto principale).
- `EventForge.Server/Services/Products/ProductService.cs:240` — `GetProductDetailAsync`: stesso pattern di `GetProductByIdAsync`.
- `EventForge.Server/Services/Products/ProductService.cs:770` — `GetProductCodeByIdAsync`: interroga `context.ProductCodes` per `Id`/`!IsDeleted` senza filtro `TenantId`.
- `EventForge.Server/Services/Products/ProductService.cs:1039` — `GetProductUnitByIdAsync`: interroga `context.ProductUnits` per `Id`/`!IsDeleted` senza filtro `TenantId`.
- `EventForge.Server/Services/Promotions/PromotionService.cs:60` — `GetPromotionByIdAsync`: interroga `context.Promotions` per `Id`/`!IsDeleted` senza filtro `TenantId`.

## Nota

Il gap preesistente `EventForge.Server/Services/Configuration/ConfigurationService.cs:254-268` (`GetValueAsync`, filtra solo per `Key` ignorando `TenantId`) era già stato documentato in una memoria repository precedente e non viene ripetuto qui come nuovo finding, ma resta da correggere in un intervento dedicato.

Questi 6 metodi seguono lo stesso pattern di rischio di P01/P02 (lettura per Id senza filtro tenant, con `ITenantContext` già disponibile nel costruttore) e vanno trattati come nuovo prompt di audit/fix dedicato, non ampliando lo scope di questo intervento.
