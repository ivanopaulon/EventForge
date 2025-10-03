# Verifica Completa Servizi Client - Report Dettagliato

**Data Generazione**: 2025-10-03 15:11:20

```
================================================================================
VERIFICA COMPLETA SERVIZI CLIENT - EventForge
================================================================================

📊 RIEPILOGO
--------------------------------------------------------------------------------
Totale servizi client analizzati: 36
Servizi conformi ✅: 22
Warning ⚠️: 20
Errori critici ❌: 1
Controller server analizzati: 34

✅ SERVIZI CONFORMI (Pattern IHttpClientService)
--------------------------------------------------------------------------------
  • BackupService
    Endpoints: 5
  • BrandService
    Base URL: api/v1/product-management/brands
    Endpoints: 4
  • BusinessPartyService
    Endpoints: 6
  • ChatService
    Endpoints: 11
  • ConfigurationService
    Endpoints: 7
  • EntityManagementService
    Endpoints: 20
  • EventService
    Endpoints: 5
  • FinancialService
    Endpoints: 12
  • HttpClientService
    Endpoints: 0
  • LogsService
    Endpoints: 6
  • LotService
    Base URL: api/v1/warehouse/lots
    Endpoints: 8
  • ModelService
    Base URL: api/v1/product-management/models
    Endpoints: 5
  • NoteFlagService
    Base URL: api/v1/note-flags
    Endpoints: 4
  • NotificationService
    Endpoints: 4
  • PaymentMethodService
    Base URL: api/v1/payment-methods
    Endpoints: 4
  • ProductService
    Base URL: api/v1/product-management/products
    Endpoints: 10
  • SalesService
    Base URL: api/v1/sales/sessions
    Endpoints: 15
  • StorageLocationService
    Base URL: api/v1/warehouse/locations
    Endpoints: 5
  • SuperAdminService
    Endpoints: 28
  • TableManagementService
    Base URL: api/v1/tables
    Endpoints: 13
  • UMService
    Base URL: api/v1/product-management/units
    Endpoints: 4
  • WarehouseService
    Base URL: api/v1/warehouse/facilities
    Endpoints: 4

❌ PROBLEMI CRITICI
--------------------------------------------------------------------------------
  ERROR: ClientLogService
    Uses direct HttpClient injection - should use IHttpClientService
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/ClientLogService.cs

⚠️  WARNING (Non bloccanti)
--------------------------------------------------------------------------------
  WARNING: EntityManagementService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/EntityManagementService.cs

  WARNING: OptimizedSignalRService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/OptimizedSignalRService.cs

  WARNING: HealthService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/HealthService.cs

  WARNING: EventService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/EventService.cs

  WARNING: FinancialService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/FinancialService.cs

  WARNING: BusinessPartyService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/BusinessPartyService.cs

  WARNING: NotificationService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/NotificationService.cs

  WARNING: ClientLogService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/ClientLogService.cs

  WARNING: SignalRService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/SignalRService.cs

  WARNING: LogsService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/LogsService.cs

  WARNING: PrintingService
    Uses IHttpClientFactory - consider migrating to IHttpClientService
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/PrintingService.cs

  WARNING: PrintingService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/PrintingService.cs

  WARNING: AuthService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/AuthService.cs

  WARNING: SuperAdminService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/SuperAdminService.cs

  WARNING: ConfigurationService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/ConfigurationService.cs

  WARNING: LicenseService
    Uses IHttpClientFactory - consider migrating to IHttpClientService
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/LicenseService.cs

  WARNING: BackupService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/BackupService.cs

  WARNING: HttpClientService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/HttpClientService.cs

  WARNING: ChatService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/ChatService.cs

  WARNING: TranslationService
    No BaseUrl constant defined
    File: /home/runner/work/EventForge/EventForge/EventForge.Client/Services/TranslationService.cs

🔗 MAPPATURA ENDPOINT CLIENT → SERVER
--------------------------------------------------------------------------------

  Base URL: api/v1/License
    Service: LicenseService
      DELETE  {...}/tenant/{...}
      DELETE  {...}/{...}

  Base URL: api/v1/note-flags
    Service: NoteFlagService
      DELETE  {...}/{...}
      GET     {...}/active
      GET     {...}/{...}
      PUT     {...}/{...}

  Base URL: api/v1/payment-methods
    Service: PaymentMethodService
      DELETE  {...}/{...}
      GET     {...}/active
      GET     {...}/{...}
      PUT     {...}/{...}

  Base URL: api/v1/product-management/brands
    Service: BrandService
      DELETE  {...}/{...}
      GET     {...}/{...}
      GET     {...}?page={...}&pageSize={...}
      PUT     {...}/{...}

  Base URL: api/v1/product-management/models
    Service: ModelService
      DELETE  {...}/{...}
      GET     {...}/{...}
      GET     {...}?brandId={...}&page={...}&pageSize={...}
      GET     {...}?page={...}&pageSize={...}
      PUT     {...}/{...}

  Base URL: api/v1/product-management/products
    Service: ProductService
      DELETE  api/v1/product-management/product-suppliers/{...}
      GET     api/v1/product-management/product-suppliers/{...}
      GET     {...}/by-code/{...}
      GET     {...}/{...}
      GET     {...}/{...}/suppliers
      GET     {...}?page={...}&pageSize={...}
      POST    api/v1/product-management/product-suppliers
      POST    {...}/{...}/codes
      PUT     api/v1/product-management/product-suppliers/{...}
      PUT     {...}/{...}

  Base URL: api/v1/product-management/units
    Service: UMService
      DELETE  {...}/{...}
      GET     {...}/{...}
      GET     {...}?page={...}&pageSize={...}
      PUT     {...}/{...}

  Base URL: api/v1/sales/sessions
    Service: SalesService
      DELETE  {...}/{...}
      DELETE  {...}/{...}/items/{...}
      DELETE  {...}/{...}/payments/{...}
      GET     {...}/operator/{...}
      GET     {...}/{...}
      POST    {...}/{...}/close
      POST    {...}/{...}/items
      POST    {...}/{...}/notes
      POST    {...}/{...}/payments
      POST    {...}/{...}/totals
      PUT     {...}/{...}
      PUT     {...}/{...}/items/{...}

  Base URL: api/v1/tables
    Service: TableManagementService
      DELETE  {...}/reservations/{...}
      DELETE  {...}/{...}
      GET     {...}/available
      GET     {...}/reservations/{...}
      GET     {...}/reservations?date={...}
      GET     {...}/{...}
      POST    {...}/reservations
      PUT     {...}/reservations/{...}
      PUT     {...}/reservations/{...}/arrived
      PUT     {...}/reservations/{...}/confirm
      PUT     {...}/reservations/{...}/no-show
      PUT     {...}/{...}
      PUT     {...}/{...}/status

  Base URL: api/v1/warehouse/facilities
    Service: WarehouseService
      DELETE  {...}/{...}
      GET     {...}/{...}
      GET     {...}?page={...}&pageSize={...}
      PUT     {...}/{...}

  Base URL: api/v1/warehouse/locations
    Service: StorageLocationService
      DELETE  {...}/{...}
      GET     {...}/{...}
      GET     {...}?facilityId={...}&page={...}&pageSize={...}
      GET     {...}?page={...}&pageSize={...}
      PUT     {...}/{...}

  Base URL: api/v1/warehouse/lots
    Service: LotService
      DELETE  {...}/{...}
      GET     {...}/code/{...}
      GET     {...}/expiring?daysAhead={...}
      GET     {...}/{...}
      GET     {...}?{...}
      POST    {...}/{...}/block?reason={...}
      POST    {...}/{...}/unblock
      PUT     {...}/{...}

🔧 ENDPOINT SERVER DISPONIBILI
--------------------------------------------------------------------------------

  Controller: AuthController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/me
      POST    api/v1/[controller]/change-password
      POST    api/v1/[controller]/login
      POST    api/v1/[controller]/logout
      POST    api/v1/[controller]/validate-token

  Controller: BusinessPartiesController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/accounting/{id:guid}
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/accounting
      GET     api/v1/[controller]/accounting/{id:guid}
      GET     api/v1/[controller]/by-type/{partyType}
      GET     api/v1/[controller]/{businessPartyId:guid}/accounting
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/accounting
      PUT     api/v1/[controller]/accounting/{id:guid}
      PUT     api/v1/[controller]/{id:guid}

  Controller: ChatController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/messages/{messageId:guid}
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/export/{exportId:guid}/download
      GET     api/v1/[controller]/export/{exportId:guid}/status
      GET     api/v1/[controller]/files/{attachmentId:guid}/download
      GET     api/v1/[controller]/files/{attachmentId:guid}/info
      GET     api/v1/[controller]/messages
      GET     api/v1/[controller]/messages/{messageId:guid}
      GET     api/v1/[controller]/statistics
      GET     api/v1/[controller]/system/health
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/export
      POST    api/v1/[controller]/messages
      POST    api/v1/[controller]/messages/bulk-read
      POST    api/v1/[controller]/messages/{messageId:guid}/read
      POST    api/v1/[controller]/upload
      PUT     api/v1/[controller]/messages/{messageId:guid}
      PUT     api/v1/[controller]/{id:guid}

  Controller: ClientLogsController
    Route: api/[controller]
      POST    api/[controller]
      POST    api/[controller]/batch

  Controller: DocumentHeadersController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/business-party/{businessPartyId:guid}
      GET     api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/{id:guid}/exists
      POST    api/v1/[controller]
      POST    api/v1/[controller]/{id:guid}/approve
      POST    api/v1/[controller]/{id:guid}/calculate-totals
      POST    api/v1/[controller]/{id:guid}/close
      PUT     api/v1/[controller]/{id:guid}

  Controller: DocumentRecurrencesController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/active
      GET     api/v1/[controller]/{id:guid}
      PATCH   api/v1/[controller]/{id:guid}/enabled
      POST    api/v1/[controller]
      PUT     api/v1/[controller]/{id:guid}

  Controller: DocumentReferencesController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/owner/{ownerId:guid}
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      PUT     api/v1/[controller]/{id:guid}

  Controller: DocumentTypesController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      PUT     api/v1/[controller]/{id:guid}

  Controller: DocumentsController
    Route: api/v1/documents
      DELETE  api/v1/documents/attachments/{attachmentId:guid}
      DELETE  api/v1/documents/comments/{commentId:guid}
      DELETE  api/v1/documents/templates/{templateId:guid}
      DELETE  api/v1/documents/types/{id:guid}
      DELETE  api/v1/documents/workflows/{workflowId:guid}
      DELETE  api/v1/documents/{id:guid}
      GET     api/v1/documents
      GET     api/v1/documents/analytics/kpi
      GET     api/v1/documents/analytics/summary
      GET     api/v1/documents/attachments/category/{category}
      GET     api/v1/documents/attachments/document-row/{documentRowId:guid}
      GET     api/v1/documents/attachments/{attachmentId:guid}
      GET     api/v1/documents/attachments/{attachmentId:guid}/exists
      GET     api/v1/documents/attachments/{attachmentId:guid}/versions
      GET     api/v1/documents/business-party/{businessPartyId:guid}
      GET     api/v1/documents/comments/assigned
      GET     api/v1/documents/comments/document-row/{documentRowId:guid}
      GET     api/v1/documents/comments/{commentId:guid}
      GET     api/v1/documents/comments/{commentId:guid}/exists
      GET     api/v1/documents/export/{exportId:guid}/status
      GET     api/v1/documents/templates
      GET     api/v1/documents/templates/by-category/{category}
      GET     api/v1/documents/templates/by-document-type/{documentTypeId:guid}
      GET     api/v1/documents/templates/public
      GET     api/v1/documents/templates/{templateId:guid}
      GET     api/v1/documents/types
      GET     api/v1/documents/types/{id:guid}
      GET     api/v1/documents/workflows
      GET     api/v1/documents/workflows/{workflowId:guid}
      GET     api/v1/documents/{documentId:guid}/analytics
      GET     api/v1/documents/{documentId:guid}/attachments
      GET     api/v1/documents/{documentId:guid}/comments
      GET     api/v1/documents/{documentId:guid}/comments/stats
      GET     api/v1/documents/{documentId:guid}/workflows
      GET     api/v1/documents/{id:guid}
      GET     api/v1/documents/{id:guid}/exists
      PATCH   api/v1/documents/templates/{templateId:guid}/usage
      POST    api/v1/documents
      POST    api/v1/documents/attachments
      POST    api/v1/documents/attachments/{attachmentId:guid}/sign
      POST    api/v1/documents/attachments/{attachmentId:guid}/versions
      POST    api/v1/documents/comments
      POST    api/v1/documents/comments/{commentId:guid}/reopen
      POST    api/v1/documents/comments/{commentId:guid}/resolve
      POST    api/v1/documents/export
      POST    api/v1/documents/templates
      POST    api/v1/documents/types
      POST    api/v1/documents/workflows
      POST    api/v1/documents/{documentId:guid}/analytics/events
      POST    api/v1/documents/{documentId:guid}/analytics/refresh
      POST    api/v1/documents/{documentId:guid}/attachments
      POST    api/v1/documents/{documentId:guid}/comments
      POST    api/v1/documents/{id:guid}/approve
      POST    api/v1/documents/{id:guid}/calculate-totals
      POST    api/v1/documents/{id:guid}/close
      PUT     api/v1/documents/attachments/{attachmentId:guid}
      PUT     api/v1/documents/comments/{commentId:guid}
      PUT     api/v1/documents/templates/{templateId:guid}
      PUT     api/v1/documents/types/{id:guid}
      PUT     api/v1/documents/workflows/{workflowId:guid}
      PUT     api/v1/documents/{id:guid}

  Controller: EntityManagementController
    Route: api/v1/entities
      DELETE  api/v1/entities/addresses/{id:guid}
      DELETE  api/v1/entities/classification-nodes/{id:guid}
      DELETE  api/v1/entities/contacts/{id:guid}
      DELETE  api/v1/entities/references/{id:guid}
      GET     api/v1/entities/addresses
      GET     api/v1/entities/addresses/owner/{ownerId:guid}
      GET     api/v1/entities/addresses/{id:guid}
      GET     api/v1/entities/classification-nodes
      GET     api/v1/entities/classification-nodes/root
      GET     api/v1/entities/classification-nodes/{id:guid}
      GET     api/v1/entities/classification-nodes/{id:guid}/children
      GET     api/v1/entities/contacts
      GET     api/v1/entities/contacts/owner/{ownerId:guid}
      GET     api/v1/entities/contacts/{id:guid}
      GET     api/v1/entities/references
      GET     api/v1/entities/references/owner/{ownerId:guid}
      GET     api/v1/entities/references/{id:guid}
      POST    api/v1/entities/addresses
      POST    api/v1/entities/classification-nodes
      POST    api/v1/entities/contacts
      POST    api/v1/entities/references
      PUT     api/v1/entities/addresses/{id:guid}
      PUT     api/v1/entities/classification-nodes/{id:guid}
      PUT     api/v1/entities/contacts/{id:guid}
      PUT     api/v1/entities/references/{id:guid}

  Controller: EventsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/{id:guid}/details
      POST    api/v1/[controller]
      PUT     api/v1/[controller]/{id:guid}

  Controller: FinancialManagementController
    Route: api/v1/financial
      DELETE  api/v1/financial/banks/{id:guid}
      DELETE  api/v1/financial/payment-terms/{id:guid}
      DELETE  api/v1/financial/vat-rates/{id:guid}
      GET     api/v1/financial/banks
      GET     api/v1/financial/banks/{id:guid}
      GET     api/v1/financial/payment-terms
      GET     api/v1/financial/payment-terms/{id:guid}
      GET     api/v1/financial/vat-rates
      GET     api/v1/financial/vat-rates/{id:guid}
      POST    api/v1/financial/banks
      POST    api/v1/financial/payment-terms
      POST    api/v1/financial/vat-rates
      PUT     api/v1/financial/banks/{id:guid}
      PUT     api/v1/financial/payment-terms/{id:guid}
      PUT     api/v1/financial/vat-rates/{id:guid}

  Controller: HealthController
    Route: api/v1/[controller]
      GET     api/v1/[controller]
      GET     api/v1/[controller]/detailed

  Controller: LicenseController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/tenant-licenses
      GET     api/v1/[controller]/tenant/{tenantId}
      GET     api/v1/[controller]/{id}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/assign
      PUT     api/v1/[controller]/{id}

  Controller: LogManagementController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/audit-logs
      GET     api/v1/[controller]/audit-logs/statistics
      GET     api/v1/[controller]/health
      GET     api/v1/[controller]/levels
      GET     api/v1/[controller]/logs
      GET     api/v1/[controller]/logs/recent-errors
      GET     api/v1/[controller]/logs/statistics
      GET     api/v1/[controller]/logs/{id:int}
      GET     api/v1/[controller]/monitoring/configuration
      POST    api/v1/[controller]/cache/clear
      POST    api/v1/[controller]/client-logs
      POST    api/v1/[controller]/client-logs/batch
      POST    api/v1/[controller]/export
      PUT     api/v1/[controller]/monitoring/configuration

  Controller: MembershipCardsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/member/{teamMemberId:guid}
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      PUT     api/v1/[controller]/{id:guid}

  Controller: NoteFlagsController
    Route: api/v1/note-flags
      DELETE  api/v1/note-flags/{id:guid}
      GET     api/v1/note-flags
      GET     api/v1/note-flags/active
      GET     api/v1/note-flags/{id:guid}
      POST    api/v1/note-flags
      PUT     api/v1/note-flags/{id:guid}

  Controller: NotificationsController
    Route: api/v1/[controller]
      GET     api/v1/[controller]
      GET     api/v1/[controller]/export/{exportId:guid}/download
      GET     api/v1/[controller]/export/{exportId:guid}/status
      GET     api/v1/[controller]/statistics
      GET     api/v1/[controller]/system/health
      GET     api/v1/[controller]/{id:guid}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/bulk
      POST    api/v1/[controller]/bulk-action
      POST    api/v1/[controller]/export
      POST    api/v1/[controller]/{id:guid}/acknowledge
      POST    api/v1/[controller]/{id:guid}/archive
      POST    api/v1/[controller]/{id:guid}/silence

  Controller: PaymentMethodsController
    Route: api/v1/payment-methods
      DELETE  api/v1/payment-methods/{id:guid}
      GET     api/v1/payment-methods
      GET     api/v1/payment-methods/active
      GET     api/v1/payment-methods/by-code/{code}
      GET     api/v1/payment-methods/check-code/{code}
      GET     api/v1/payment-methods/{id:guid}
      POST    api/v1/payment-methods
      PUT     api/v1/payment-methods/{id:guid}

  Controller: PerformanceController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/slow-queries
      GET     api/v1/[controller]/statistics
      GET     api/v1/[controller]/summary

  Controller: PrintingController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/jobs/{jobId:guid}
      GET     api/v1/[controller]/qz/certificate
      POST    api/v1/[controller]/discover
      POST    api/v1/[controller]/jobs/{jobId:guid}/cancel
      POST    api/v1/[controller]/print
      POST    api/v1/[controller]/qz/demo-sha512-signing
      POST    api/v1/[controller]/qz/sign
      POST    api/v1/[controller]/status
      POST    api/v1/[controller]/test-connection
      POST    api/v1/[controller]/test-signature
      POST    api/v1/[controller]/version

  Controller: ProductManagementController
    Route: api/v1/product-management
      DELETE  api/v1/product-management/brands/{id:guid}
      DELETE  api/v1/product-management/models/{id:guid}
      DELETE  api/v1/product-management/price-lists/{id:guid}
      DELETE  api/v1/product-management/product-suppliers/{id:guid}
      DELETE  api/v1/product-management/products/{id:guid}
      DELETE  api/v1/product-management/products/{id}/image
      DELETE  api/v1/product-management/promotions/{id:guid}
      DELETE  api/v1/product-management/units/{id:guid}
      GET     api/v1/product-management/brands
      GET     api/v1/product-management/brands/{id:guid}
      GET     api/v1/product-management/models
      GET     api/v1/product-management/models/{id:guid}
      GET     api/v1/product-management/price-lists
      GET     api/v1/product-management/price-lists/{id:guid}
      GET     api/v1/product-management/product-suppliers/{id:guid}
      GET     api/v1/product-management/products
      GET     api/v1/product-management/products/by-code/{code}
      GET     api/v1/product-management/products/{id:guid}
      GET     api/v1/product-management/products/{id}/image
      GET     api/v1/product-management/products/{productId:guid}/codes
      GET     api/v1/product-management/products/{productId:guid}/suppliers
      GET     api/v1/product-management/promotions
      GET     api/v1/product-management/promotions/{id:guid}
      GET     api/v1/product-management/units
      GET     api/v1/product-management/units/{id:guid}
      POST    api/v1/product-management/barcodes/generate
      POST    api/v1/product-management/brands
      POST    api/v1/product-management/models
      POST    api/v1/product-management/price-lists
      POST    api/v1/product-management/product-suppliers
      POST    api/v1/product-management/products
      POST    api/v1/product-management/products/upload-image
      POST    api/v1/product-management/products/{id}/image
      POST    api/v1/product-management/products/{productId:guid}/codes
      POST    api/v1/product-management/promotions
      POST    api/v1/product-management/units
      PUT     api/v1/product-management/brands/{id:guid}
      PUT     api/v1/product-management/models/{id:guid}
      PUT     api/v1/product-management/price-lists/{id:guid}
      PUT     api/v1/product-management/product-suppliers/{id:guid}
      PUT     api/v1/product-management/products/{id:guid}
      PUT     api/v1/product-management/promotions/{id:guid}
      PUT     api/v1/product-management/units/{id:guid}

  Controller: RetailCartSessionsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id:guid}/items/{itemId:guid}
      GET     api/v1/[controller]/{id:guid}
      PATCH   api/v1/[controller]/{id:guid}/items/{itemId:guid}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/{id:guid}/clear
      POST    api/v1/[controller]/{id:guid}/coupons
      POST    api/v1/[controller]/{id:guid}/items

  Controller: SalesController
    Route: api/v1/sales
      DELETE  api/v1/sales/sessions/{sessionId:guid}
      DELETE  api/v1/sales/sessions/{sessionId:guid}/items/{itemId:guid}
      DELETE  api/v1/sales/sessions/{sessionId:guid}/payments/{paymentId:guid}
      GET     api/v1/sales/sessions
      GET     api/v1/sales/sessions/operator/{operatorId:guid}
      GET     api/v1/sales/sessions/{sessionId:guid}
      POST    api/v1/sales/sessions
      POST    api/v1/sales/sessions/{sessionId:guid}/close
      POST    api/v1/sales/sessions/{sessionId:guid}/items
      POST    api/v1/sales/sessions/{sessionId:guid}/notes
      POST    api/v1/sales/sessions/{sessionId:guid}/payments
      POST    api/v1/sales/sessions/{sessionId:guid}/totals
      PUT     api/v1/sales/sessions/{sessionId:guid}
      PUT     api/v1/sales/sessions/{sessionId:guid}/items/{itemId:guid}

  Controller: StationsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/printers/{id:guid}
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/printers
      GET     api/v1/[controller]/printers/{id:guid}
      GET     api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/{stationId:guid}/printers
      POST    api/v1/[controller]
      POST    api/v1/[controller]/printers
      PUT     api/v1/[controller]/printers/{id:guid}
      PUT     api/v1/[controller]/{id:guid}

  Controller: StoreUsersController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/groups/{id:guid}
      DELETE  api/v1/[controller]/groups/{id:guid}/logo
      DELETE  api/v1/[controller]/pos/{id:guid}/image
      DELETE  api/v1/[controller]/privileges/{id:guid}
      DELETE  api/v1/[controller]/{id:guid}
      DELETE  api/v1/[controller]/{id:guid}/photo
      GET     api/v1/[controller]
      GET     api/v1/[controller]/by-group/{groupId:guid}
      GET     api/v1/[controller]/groups
      GET     api/v1/[controller]/groups/{id:guid}
      GET     api/v1/[controller]/groups/{id:guid}/logo
      GET     api/v1/[controller]/pos/{id:guid}/image
      GET     api/v1/[controller]/privileges
      GET     api/v1/[controller]/privileges/by-group/{groupId:guid}
      GET     api/v1/[controller]/privileges/{id:guid}
      GET     api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/{id:guid}/photo
      POST    api/v1/[controller]
      POST    api/v1/[controller]/groups
      POST    api/v1/[controller]/groups/{id:guid}/logo
      POST    api/v1/[controller]/pos/{id:guid}/image
      POST    api/v1/[controller]/privileges
      POST    api/v1/[controller]/{id:guid}/photo
      PUT     api/v1/[controller]/groups/{id:guid}
      PUT     api/v1/[controller]/privileges/{id:guid}
      PUT     api/v1/[controller]/{id:guid}

  Controller: SuperAdminController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/backup/{backupId}
      DELETE  api/v1/[controller]/configuration/{key}
      GET     api/v1/[controller]/backup
      GET     api/v1/[controller]/backup/{backupId}
      GET     api/v1/[controller]/backup/{backupId}/download
      GET     api/v1/[controller]/configuration
      GET     api/v1/[controller]/configuration/categories
      GET     api/v1/[controller]/configuration/category/{category}
      GET     api/v1/[controller]/configuration/{key}
      POST    api/v1/[controller]/backup
      POST    api/v1/[controller]/backup/{backupId}/cancel
      POST    api/v1/[controller]/configuration
      POST    api/v1/[controller]/configuration/reload
      POST    api/v1/[controller]/configuration/test-smtp
      PUT     api/v1/[controller]/configuration/{key}

  Controller: TableManagementController
    Route: api/v1/tables
      DELETE  api/v1/tables/reservations/{id}
      DELETE  api/v1/tables/{id}
      GET     api/v1/tables
      GET     api/v1/tables/available
      GET     api/v1/tables/reservations
      GET     api/v1/tables/reservations/{id}
      GET     api/v1/tables/{id}
      POST    api/v1/tables
      POST    api/v1/tables/reservations
      PUT     api/v1/tables/reservations/{id}
      PUT     api/v1/tables/reservations/{id}/arrived
      PUT     api/v1/tables/reservations/{id}/confirm
      PUT     api/v1/tables/reservations/{id}/no-show
      PUT     api/v1/tables/{id}
      PUT     api/v1/tables/{id}/status

  Controller: TeamsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/members/{memberId:guid}
      DELETE  api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/by-event/{eventId:guid}
      GET     api/v1/[controller]/members/{memberId:guid}
      GET     api/v1/[controller]/{id:guid}
      GET     api/v1/[controller]/{id:guid}/details
      GET     api/v1/[controller]/{teamId:guid}/members
      POST    api/v1/[controller]
      POST    api/v1/[controller]/members
      PUT     api/v1/[controller]/members/{memberId:guid}
      PUT     api/v1/[controller]/{id:guid}

  Controller: TenantContextController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/audit-trail
      GET     api/v1/[controller]/current
      GET     api/v1/[controller]/validate-access/{tenantId}
      POST    api/v1/[controller]/end-impersonation
      POST    api/v1/[controller]/start-impersonation
      POST    api/v1/[controller]/switch-tenant

  Controller: TenantSwitchController
    Route: api/v1/[controller]
      GET     api/v1/[controller]/context
      GET     api/v1/[controller]/history/impersonations
      GET     api/v1/[controller]/history/tenant-switches
      GET     api/v1/[controller]/statistics
      POST    api/v1/[controller]/end-impersonation
      POST    api/v1/[controller]/impersonate
      POST    api/v1/[controller]/switch
      POST    api/v1/[controller]/validate

  Controller: TenantsController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{id}/admins/{userId}
      DELETE  api/v1/[controller]/{id}/soft
      GET     api/v1/[controller]
      GET     api/v1/[controller]/activity/live
      GET     api/v1/[controller]/available
      GET     api/v1/[controller]/statistics
      GET     api/v1/[controller]/statistics/live
      GET     api/v1/[controller]/{id}
      GET     api/v1/[controller]/{id}/admins
      GET     api/v1/[controller]/{id}/details
      GET     api/v1/[controller]/{id}/limits
      POST    api/v1/[controller]
      POST    api/v1/[controller]/search
      POST    api/v1/[controller]/with-admin
      POST    api/v1/[controller]/{id}/admins/{userId}
      POST    api/v1/[controller]/{id}/disable
      POST    api/v1/[controller]/{id}/enable
      POST    api/v1/[controller]/{id}/users/{userId}/force-password-change
      PUT     api/v1/[controller]/{id}
      PUT     api/v1/[controller]/{id}/limits

  Controller: UserManagementController
    Route: api/v1/[controller]
      DELETE  api/v1/[controller]/{userId}
      GET     api/v1/[controller]
      GET     api/v1/[controller]/roles
      GET     api/v1/[controller]/statistics
      GET     api/v1/[controller]/{userId}
      POST    api/v1/[controller]
      POST    api/v1/[controller]/management
      POST    api/v1/[controller]/quick-actions
      POST    api/v1/[controller]/search
      POST    api/v1/[controller]/{userId}/force-password-change
      POST    api/v1/[controller]/{userId}/reset-password
      PUT     api/v1/[controller]/{userId}
      PUT     api/v1/[controller]/{userId}/roles
      PUT     api/v1/[controller]/{userId}/status

  Controller: WarehouseManagementController
    Route: api/v1/warehouse
      DELETE  api/v1/warehouse/lots/{id:guid}
      GET     api/v1/warehouse/facilities
      GET     api/v1/warehouse/facilities/{id:guid}
      GET     api/v1/warehouse/inventory
      GET     api/v1/warehouse/inventory/document/{documentId:guid}
      GET     api/v1/warehouse/inventory/documents
      GET     api/v1/warehouse/locations
      GET     api/v1/warehouse/locations/{id:guid}
      GET     api/v1/warehouse/lots
      GET     api/v1/warehouse/lots/code/{code}
      GET     api/v1/warehouse/lots/expiring
      GET     api/v1/warehouse/lots/{id:guid}
      GET     api/v1/warehouse/serials
      GET     api/v1/warehouse/serials/{id:guid}
      GET     api/v1/warehouse/stock
      GET     api/v1/warehouse/stock/{id:guid}
      PATCH   api/v1/warehouse/lots/{id:guid}/quality-status
      POST    api/v1/warehouse/facilities
      POST    api/v1/warehouse/inventory
      POST    api/v1/warehouse/inventory/document/start
      POST    api/v1/warehouse/inventory/document/{documentId:guid}/finalize
      POST    api/v1/warehouse/inventory/document/{documentId:guid}/row
      POST    api/v1/warehouse/locations
      POST    api/v1/warehouse/lots
      POST    api/v1/warehouse/lots/{id:guid}/block
      POST    api/v1/warehouse/lots/{id:guid}/unblock
      POST    api/v1/warehouse/serials
      POST    api/v1/warehouse/stock
      POST    api/v1/warehouse/stock/reserve
      PUT     api/v1/warehouse/lots/{id:guid}
      PUT     api/v1/warehouse/serials/{id:guid}/status

================================================================================
Fine Report
================================================================================
```

## 📋 Analisi Dettagliata per Servizio

### AuthService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### BackupService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/super-admin/backup/{backupId}`
- `GET api/v1/super-admin/backup/{backupId}`
- `GET api/v1/super-admin/backup?limit={limit}`
- `POST api/v1/super-admin/backup`
- `POST api/v1/super-admin/backup/{backupId}/cancel`

---

### BrandService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/product-management/brands`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `PUT {BaseUrl}/{id}`

---

### BusinessPartyService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/businessparties/{id}`
- `GET api/v1/businessparties/by-type/{partyType}`
- `GET api/v1/businessparties/{id}`
- `GET api/v1/businessparties?page={page}&pageSize={pageSize}`
- `POST api/v1/businessparties`
- `PUT api/v1/businessparties/{id}`

---

### ChatService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/chat/messages/{messageId}`
- `DELETE api/v1/chat/{chatId}/members/{userId}`
- `GET api/v1/chat/stats`
- `GET api/v1/chat/{chatId}/messages?{query}`
- `GET api/v1/chat/{id}`
- `GET api/v1/chat?{query}`
- `POST api/v1/chat`
- `POST api/v1/chat/messages/{reactionDto.MessageId}/reactions`
- `POST api/v1/chat/{chatId}/members`
- `POST api/v1/chat/{messageDto.ChatId}/messages`
- `PUT api/v1/chat/messages/{messageId}`

---

### ClientLogService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory
- ❌ Direct HttpClient

**Base URL:** ❌ Non definito

---

### ConfigurationService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/super-admin/configuration/{key}`
- `GET api/v1/super-admin/configuration/category/{category}`
- `GET api/v1/super-admin/configuration/{key}`
- `POST api/v1/super-admin/configuration`
- `POST api/v1/super-admin/configuration/reload`
- `POST api/v1/super-admin/configuration/test-smtp`
- `PUT api/v1/super-admin/configuration/{key}`

---

### CustomAuthenticationStateProvider

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### EntityManagementService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/entities/addresses/{id}`
- `DELETE api/v1/entities/classification-nodes/{id}`
- `DELETE api/v1/entities/contacts/{id}`
- `DELETE api/v1/entities/references/{id}`
- `GET api/v1/entities/addresses/owner/{ownerId}`
- `GET api/v1/entities/addresses/{id}`
- `GET api/v1/entities/classification-nodes/{id}`
- `GET api/v1/entities/classification-nodes/{parentId}/children`
- `GET api/v1/entities/contacts/owner/{ownerId}`
- `GET api/v1/entities/contacts/{id}`
- `GET api/v1/entities/references/owner/{ownerId}`
- `GET api/v1/entities/references/{id}`
- `POST api/v1/entities/addresses`
- `POST api/v1/entities/classification-nodes`
- `POST api/v1/entities/contacts`
- `POST api/v1/entities/references`
- `PUT api/v1/entities/addresses/{id}`
- `PUT api/v1/entities/classification-nodes/{id}`
- `PUT api/v1/entities/contacts/{id}`
- `PUT api/v1/entities/references/{id}`

---

### EventService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/events/{id}`
- `GET api/v1/events/{id}`
- `GET api/v1/events/{id}/details`
- `POST api/v1/events`
- `PUT api/v1/events/{id}`

---

### FinancialService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/financial/banks/{id}`
- `DELETE api/v1/financial/payment-terms/{id}`
- `DELETE api/v1/financial/vat-rates/{id}`
- `GET api/v1/financial/banks/{id}`
- `GET api/v1/financial/payment-terms/{id}`
- `GET api/v1/financial/vat-rates/{id}`
- `POST api/v1/financial/banks`
- `POST api/v1/financial/payment-terms`
- `POST api/v1/financial/vat-rates`
- `PUT api/v1/financial/banks/{id}`
- `PUT api/v1/financial/payment-terms/{id}`
- `PUT api/v1/financial/vat-rates/{id}`

---

### HealthService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### HelpService

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### HttpClientService

**Pattern utilizzato:**
- ✅ IHttpClientService
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### LicenseService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** `api/v1/License`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/tenant/{tenantId}`
- `DELETE {BaseUrl}/{id}`

---

### LoadingDialogService

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### LogsService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `GET api/v1/application-logs/statistics`
- `GET api/v1/application-logs/{id}`
- `GET api/v1/application-logs?{queryString}`
- `GET api/v1/audit-logs/statistics`
- `GET api/v1/audit-logs/{id}`
- `GET api/v1/audit-logs?{queryString}`

---

### LotService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/warehouse/lots`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/code/{Uri.EscapeDataString(code)}`
- `GET {BaseUrl}/expiring?daysAhead={daysAhead}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?{query}`
- `POST {BaseUrl}/{id}/block?reason={Uri.EscapeDataString(reason)}`
- `POST {BaseUrl}/{id}/unblock`
- `PUT {BaseUrl}/{id}`

---

### ModelService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/product-management/models`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?brandId={brandId}&page={page}&pageSize={pageSize}`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `PUT {BaseUrl}/{id}`

---

### NoteFlagService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/note-flags`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/active`
- `GET {BaseUrl}/{id}`
- `PUT {BaseUrl}/{id}`

---

### NotificationService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `GET api/v1/notifications/stats`
- `GET api/v1/notifications/{id}`
- `GET api/v1/notifications?{query}`
- `POST api/v1/notifications`

---

### OptimizedSignalRService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### PaymentMethodService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/payment-methods`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/active`
- `GET {BaseUrl}/{id}`
- `PUT {BaseUrl}/{id}`

---

### PerformanceOptimizationService

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### PrintingService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `GET api/printing/jobs/{jobId}`
- `POST api/printing/jobs/{jobId}/cancel`

---

### ProductService

**Pattern utilizzato:**
- ✅ IHttpClientService
- ⚠️  IHttpClientFactory

**Base URL:** `api/v1/product-management/products`

**Endpoints chiamati:**

- `DELETE api/v1/product-management/product-suppliers/{id}`
- `GET api/v1/product-management/product-suppliers/{id}`
- `GET {BaseUrl}/by-code/{Uri.EscapeDataString(code)}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}/{productId}/suppliers`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `POST api/v1/product-management/product-suppliers`
- `POST {BaseUrl}/{createDto.ProductId}/codes`
- `PUT api/v1/product-management/product-suppliers/{id}`
- `PUT {BaseUrl}/{id}`

---

### SalesService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/sales/sessions`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{sessionId}`
- `DELETE {BaseUrl}/{sessionId}/items/{itemId}`
- `DELETE {BaseUrl}/{sessionId}/payments/{paymentId}`
- `GET {BaseUrl}/operator/{operatorId}`
- `GET {BaseUrl}/{sessionId}`
- `POST {BaseUrl}/{sessionId}/close`
- `POST {BaseUrl}/{sessionId}/items`
- `POST {BaseUrl}/{sessionId}/notes`
- `POST {BaseUrl}/{sessionId}/payments`
- `POST {BaseUrl}/{sessionId}/totals`
- `PUT {BaseUrl}/{sessionId}`
- `PUT {BaseUrl}/{sessionId}/items/{itemId}`

---

### SignalRService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### StorageLocationService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/warehouse/locations`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?facilityId={warehouseId}&page={page}&pageSize={pageSize}`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `PUT {BaseUrl}/{id}`

---

### SuperAdminService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** ❌ Non definito

**Endpoints chiamati:**

- `DELETE api/v1/super-admin/backup/{backupId}`
- `DELETE api/v1/super-admin/configuration/{key}`
- `DELETE api/v1/tenants/{id}/soft`
- `DELETE api/v1/user-management/{id}`
- `GET api/v1/super-admin/backup/{backupId}`
- `GET api/v1/super-admin/configuration/category/{category}`
- `GET api/v1/super-admin/configuration/{key}`
- `GET api/v1/tenant-context/current`
- `GET api/v1/tenants/statistics`
- `GET api/v1/tenants/{id}`
- `GET api/v1/tenants/{id}/details`
- `GET api/v1/tenants/{id}/limits`
- `GET api/v1/user-management/{id}`
- `POST api/v1/super-admin/backup`
- `POST api/v1/super-admin/backup/{backupId}/cancel`
- `POST api/v1/super-admin/configuration`
- `POST api/v1/super-admin/configuration/test-smtp`
- `POST api/v1/tenant-switch/impersonate`
- `POST api/v1/tenant-switch/switch`
- `POST api/v1/tenants`
- `POST api/v1/tenants/{id}/disable`
- `POST api/v1/tenants/{id}/enable`
- `POST api/v1/user-management/management`
- `POST api/v1/user-management/{id}/reset-password`
- `PUT api/v1/super-admin/configuration/{key}`
- `PUT api/v1/tenants/{id}`
- `PUT api/v1/tenants/{id}/limits`
- `PUT api/v1/user-management/{id}`

---

### TableManagementService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/tables`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/reservations/{id}`
- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/available`
- `GET {BaseUrl}/reservations/{id}`
- `GET {BaseUrl}/reservations?date={dateStr}`
- `GET {BaseUrl}/{id}`
- `POST {BaseUrl}/reservations`
- `PUT {BaseUrl}/reservations/{id}`
- `PUT {BaseUrl}/reservations/{id}/arrived`
- `PUT {BaseUrl}/reservations/{id}/confirm`
- `PUT {BaseUrl}/reservations/{id}/no-show`
- `PUT {BaseUrl}/{id}`
- `PUT {BaseUrl}/{id}/status`

---

### TenantContextService

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### ThemeService

**Pattern utilizzato:**

**Base URL:** ❌ Non definito

---

### TranslationService

**Pattern utilizzato:**
- ⚠️  IHttpClientFactory

**Base URL:** ❌ Non definito

---

### UMService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/product-management/units`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `PUT {BaseUrl}/{id}`

---

### WarehouseService

**Pattern utilizzato:**
- ✅ IHttpClientService

**Base URL:** `api/v1/warehouse/facilities`

**Endpoints chiamati:**

- `DELETE {BaseUrl}/{id}`
- `GET {BaseUrl}/{id}`
- `GET {BaseUrl}?page={page}&pageSize={pageSize}`
- `PUT {BaseUrl}/{id}`

---

