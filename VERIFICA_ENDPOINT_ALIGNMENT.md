# Verifica Allineamento Endpoint Client ↔ Server

**Data**: 2025-10-03 15:13:12


## 📊 Riepilogo Generale

- **Endpoint Server**: 91
- **Chiamate Client**: 145
- **Servizi con BaseUrl**: 12


## 🔗 Analisi Dettagliata per Servizio


### BrandService

**Base URL**: `api/v1/product-management/brands`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetBrandByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateBrandAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `UpdateBrandAsync`



---


### LicenseService

**Base URL**: `api/v1/License`


**❌ Endpoint Non Trovati sul Server:**


- `DELETE {BaseUrl}/{id}`

  - Client method: `Unknown`

- `DELETE {BaseUrl}/tenant/{tenantId}`

  - Client method: `Unknown`



---


### LotService

**Base URL**: `api/v1/warehouse/lots`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetLotByIdAsync`

- `GET {BaseUrl}/code/{Uri.EscapeDataString(code)}`

  - Client method: `GetLotByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `CreateLotAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `UpdateLotAsync`



---


### ModelService

**Base URL**: `api/v1/product-management/models`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetModelByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateModelAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `UpdateModelAsync`



---


### NoteFlagService

**Base URL**: `api/v1/note-flags`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `UpdateAsync`



---


### PaymentMethodService

**Base URL**: `api/v1/payment-methods`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `DeleteAsync`



---


### ProductService

**Base URL**: `api/v1/product-management/products`


**✅ Endpoint Matched:**


- ✅ MATCH `DELETE api/v1/product-management/product-suppliers/{id}`

  - Client method: `DeleteProductSupplierAsync`

  - Server: `ProductManagementController.DeleteProductSupplier`



**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/by-code/{Uri.EscapeDataString(code)}`

  - Client method: `GetProductByCodeAsync`

- `GET {BaseUrl}/{id}`

  - Client method: `GetProductByIdAsync`

- `GET api/v1/product-management/product-suppliers/{id}`

  - Client method: `GetProductSupplierByIdAsync`

- `POST {BaseUrl}/{createDto.ProductId}/codes`

  - Client method: `CreateProductCodeAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateProductAsync`

- `PUT api/v1/product-management/product-suppliers/{id}`

  - Client method: `UpdateProductSupplierAsync`

- `POST api/v1/product-management/product-suppliers`

  - Client method: `CreateProductSupplierAsync`



---


### SalesService

**Base URL**: `api/v1/sales/sessions`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{sessionId}`

  - Client method: `GetSessionAsync`

- `GET {BaseUrl}/{sessionId}`

  - Client method: `RemoveItemAsync`

- `GET {BaseUrl}/{sessionId}`

  - Client method: `RemoveItemAsync`

- `GET {BaseUrl}/{sessionId}`

  - Client method: `RemovePaymentAsync`

- `POST {BaseUrl}/{sessionId}/items`

  - Client method: `AddItemAsync`

- `POST {BaseUrl}/{sessionId}/payments`

  - Client method: `AddPaymentAsync`

- `POST {BaseUrl}/{sessionId}/notes`

  - Client method: `AddNoteAsync`

- `POST {BaseUrl}/{sessionId}/totals`

  - Client method: `CalculateTotalsAsync`

- `POST {BaseUrl}/{sessionId}/close`

  - Client method: `CloseSessionAsync`

- `PUT {BaseUrl}/{sessionId}`

  - Client method: `UpdateSessionAsync`

- `PUT {BaseUrl}/{sessionId}/items/{itemId}`

  - Client method: `UpdateItemAsync`

- `DELETE {BaseUrl}/{sessionId}`

  - Client method: `DeleteSessionAsync`

- `DELETE {BaseUrl}/{sessionId}/items/{itemId}`

  - Client method: `RemoveItemAsync`

- `DELETE {BaseUrl}/{sessionId}/payments/{paymentId}`

  - Client method: `RemovePaymentAsync`



---


### StorageLocationService

**Base URL**: `api/v1/warehouse/locations`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetStorageLocationAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateStorageLocationAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `DeleteStorageLocationAsync`



---


### TableManagementService

**Base URL**: `api/v1/tables`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetTableAsync`

- `GET {BaseUrl}/reservations/{id}`

  - Client method: `GetReservationAsync`

- `POST {BaseUrl}/reservations`

  - Client method: `CreateReservationAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateTableAsync`

- `PUT {BaseUrl}/{id}/status`

  - Client method: `UpdateTableStatusAsync`

- `PUT {BaseUrl}/reservations/{id}`

  - Client method: `UpdateReservationAsync`

- `PUT {BaseUrl}/reservations/{id}/confirm`

  - Client method: `ConfirmReservationAsync`

- `PUT {BaseUrl}/reservations/{id}/arrived`

  - Client method: `MarkArrivedAsync`

- `PUT {BaseUrl}/reservations/{id}/no-show`

  - Client method: `CancelReservationAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `DeleteTableAsync`

- `DELETE {BaseUrl}/reservations/{id}`

  - Client method: `CancelReservationAsync`



---


### UMService

**Base URL**: `api/v1/product-management/units`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetUMByIdAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateUMAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `UpdateUMAsync`



---


### WarehouseService

**Base URL**: `api/v1/warehouse/facilities`


**❌ Endpoint Non Trovati sul Server:**


- `GET {BaseUrl}/{id}`

  - Client method: `GetStorageFacilityAsync`

- `PUT {BaseUrl}/{id}`

  - Client method: `UpdateStorageFacilityAsync`

- `DELETE {BaseUrl}/{id}`

  - Client method: `DeleteStorageFacilityAsync`



---


## 📈 Statistiche Finali


- ✅ **Perfect Match**: 1

- ⚠️  **Partial Match**: 0

- ❌ **No Match**: 59
