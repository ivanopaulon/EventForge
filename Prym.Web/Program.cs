using Blazored.LocalStorage;
using Prym.Web;
using Prym.Web.Services;
using Prym.Web.Services.Documents;
using Prym.Web.Services.Updates;
using Prym.DTOs.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Load environment-specific overrides from "Environments:{env}" section in appsettings.json.
// Development (Blazor DevServer → blazor-environment: Development) → BaseUrl 7241
// Production  (IIS static files → no header sent)                  → BaseUrl 7242
var environment = builder.HostEnvironment.Environment;
var envSection = builder.Configuration.GetSection($"Environments:{environment}");
if (envSection.Exists())
{
    var envOverrides = envSection
        .AsEnumerable(makePathsRelative: true)
        .Where(kvp => kvp.Value is not null)
        .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value));
    builder.Configuration.AddInMemoryCollection(envOverrides);
}

// Use component type from the current project namespace
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load API base URL from configuration — already resolved to the correct environment value above
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7241/";

// Configure HttpClient instances using best practices for performance
// Note: WebAssembly uses BrowserHttpHandler which doesn't support HttpClientHandler configuration
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    // Add default headers for API requests
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
    // Enable compression for better mobile performance
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
});

// Long-running client for operations that may take several minutes (e.g. rebuild movements)
builder.Services.AddHttpClient("LongRunningApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
});

// Configure StaticClient for translation files and static assets
// BaseAddress is set to the host base URL which is known at build time in Blazor WASM
builder.Services.AddHttpClient("StaticClient", client =>
{
    // In Blazor WASM, static files are served from the same origin as the app
    // This ensures BaseAddress is set before any requests are made
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add MudBlazor services with performance optimizations
builder.Services.AddMudServices(config =>
{
    // Optimize for mobile and high-load scenarios
    config.SnackbarConfiguration.PositionClass = "mud-snackbar-location-top-right";
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Add Blazored LocalStorage for state persistence
builder.Services.AddBlazoredLocalStorage();

// Add Syncfusion Blazor services
// The license key is stored only in the server's appsettings.json ("SyncfusionLicenseKey").
// We fetch it from the public endpoint GET /api/v1/branding/client-config before building
// the DI container, so RegisterLicense() is always called before AddSyncfusionBlazor().
try
{
    using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    var clientConfig = await httpClient.GetFromJsonAsync<ClientPublicConfigDto>("api/v1/branding/client-config");
    if (!string.IsNullOrWhiteSpace(clientConfig?.SyncfusionLicenseKey))
    {
        SyncfusionLicenseProvider.RegisterLicense(clientConfig.SyncfusionLicenseKey);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Syncfusion] Could not fetch license key from server: {ex.Message}");
}
builder.Services.AddSyncfusionBlazor();

// ════════════════════════════════════════════════════════════════
// Configure MemoryCache for client-side caching of lookup tables
// ════════════════════════════════════════════════════════════════

builder.Services.AddMemoryCache(options =>
{
    // Maximum 100 cached items across all cache entries
    options.SizeLimit = 100;

    // When size limit is reached, remove 25% of least recently used entries
    options.CompactionPercentage = 0.25;

    // Scan for expired entries every 5 minutes
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

// Add centralized HTTP client service
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// Add server configuration services
builder.Services.AddScoped<IServerConfigService, ServerConfigService>();
builder.Services.AddSingleton<IServerConfigOverlayService, ServerConfigOverlayService>();

// Add custom services
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionKeepaliveService, SessionKeepaliveService>();
builder.Services.AddScoped<IConnectionMonitorService, ConnectionMonitorService>();
builder.Services.AddScoped<IAuthenticationDialogService, AuthenticationDialogService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPerformanceOptimizationService, PerformanceOptimizationService>();
builder.Services.AddScoped<IRealtimeService, OptimizedSignalRService>();
builder.Services.AddScoped<IUpdateNotificationService, UpdateNotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<Prym.Web.Services.WhatsApp.IWhatsAppClientService, Prym.Web.Services.WhatsApp.WhatsAppClientService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IFontPreferencesService, FontPreferencesService>();
builder.Services.AddScoped<IInventorySessionService, InventorySessionService>();
builder.Services.AddScoped<IFilterStateService, FilterStateService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();
builder.Services.AddScoped<IClientLogService, ClientLogService>();
builder.Services.AddScoped<IAppNotificationService, AppNotificationService>();
builder.Services.AddScoped<IHelpService, HelpService>();
builder.Services.AddScoped<ILoadingDialogService, LoadingDialogService>();
builder.Services.AddScoped<ITablePreferencesService, TablePreferencesService>();

// Add warehouse management services
builder.Services.AddScoped<ILotService, LotService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IStorageLocationService, StorageLocationService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockReconciliationService, StockReconciliationService>();
builder.Services.AddScoped<ITransferOrderService, TransferOrderService>();

// Add product management services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierSuggestionService, SupplierSuggestionService>();
builder.Services.AddScoped<IUMService, UMService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<ILookupCacheService, LookupCacheService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPriceResolutionService, PriceResolutionService>();
builder.Services.AddScoped<IPromotionClientService, PromotionClientService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportDefinitionService, ReportDefinitionService>();
builder.Services.AddScoped<IMonitoringClientService, MonitoringClientService>();

// Add DevTools services
builder.Services.AddScoped<IDevToolsService, DevToolsService>();

// Add External services
builder.Services.AddScoped<Prym.Web.Services.External.IVatLookupService, Prym.Web.Services.External.VatLookupService>();

// Add ViewModels
builder.Services.AddScoped<Prym.Web.ViewModels.ProductDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.InventoryDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.WarehouseDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.StorageLocationDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.LotDetailViewModel>();

// Onda 2: Documents & Financial ViewModels
builder.Services.AddScoped<Prym.Web.ViewModels.DocumentTypeDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.DocumentHeaderDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.DocumentCounterDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.VatRateDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.VatNatureDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.PaymentTermDetailViewModel>();

// Onda 3: BusinessParty ViewModels
builder.Services.AddScoped<Prym.Web.ViewModels.BusinessPartyDetailViewModel>();

// Store configuration ViewModels
builder.Services.AddScoped<Prym.Web.ViewModels.OperatorDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.OperatorGroupDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.PosDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.StationDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.FiscalDrawerDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.PaymentTerminalDetailViewModel>();
builder.Services.AddScoped<Prym.Web.ViewModels.PrinterDetailViewModel>();

// POS ViewModel
builder.Services.AddScoped<Prym.Web.ViewModels.POSViewModel>();

// Add dashboard configuration service
builder.Services.AddScoped<IDashboardConfigurationService, DashboardConfigurationService>();

// Add entity schema provider for dashboard metrics
builder.Services.AddScoped<Prym.Web.Services.Schema.IEntitySchemaProvider, Prym.Web.Services.Schema.EntitySchemaProvider>();

// Add document management services
builder.Services.AddScoped<IDocumentHeaderService, DocumentHeaderService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IDocumentCounterService, DocumentCounterService>();
builder.Services.AddScoped<IDocumentRowCalculationService, DocumentRowCalculationService>();
builder.Services.AddScoped<IDocumentDialogCacheService, DocumentDialogCacheService>();
builder.Services.AddScoped<IDocumentRowValidator, DocumentRowValidator>();
builder.Services.AddScoped<IDocumentStatusService, DocumentStatusService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();

// Add Logs services
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<ILogManagementService, LogManagementService>();
builder.Services.AddScoped<IEntityManagementService, EntityManagementService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IBusinessPartyService, BusinessPartyService>();
builder.Services.AddScoped<IBusinessPartyGroupService, BusinessPartyGroupService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

// Add Event management services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICalendarReminderService, CalendarReminderService>();

// Add Sales management services
builder.Services.AddScoped<Prym.Web.Services.Sales.ISalesService, Prym.Web.Services.Sales.SalesService>();
builder.Services.AddScoped<Prym.Web.Services.Sales.IPaymentMethodService, Prym.Web.Services.Sales.PaymentMethodService>();
builder.Services.AddScoped<Prym.Web.Services.Sales.INoteFlagService, Prym.Web.Services.Sales.NoteFlagService>();
builder.Services.AddScoped<Prym.Web.Services.Sales.ITableManagementService, Prym.Web.Services.Sales.TableManagementService>();

// Fiscal printing services
builder.Services.AddScoped<IFiscalPrintingService, FiscalPrintingService>();

// Add Mock services (client-side only, no backend)
builder.Services.AddSingleton<Prym.Web.Services.Mock.IMockFidelityService, Prym.Web.Services.Mock.MockFidelityService>();

// Register authenticated HTTP client handler for Store services
builder.Services.AddTransient<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

// Add Store management services with configured HttpClient and authentication handler
builder.Services.AddHttpClient<Prym.Web.Services.Store.IStoreUserService, Prym.Web.Services.Store.StoreUserService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<Prym.Web.Services.Store.IStorePosService, Prym.Web.Services.Store.StorePosService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<Prym.Web.Services.Store.IStoreUserGroupService, Prym.Web.Services.Store.StoreUserGroupService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<Prym.Web.Services.Store.IFiscalDrawerService, Prym.Web.Services.Store.FiscalDrawerService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<Prym.Web.Services.Store.IPaymentTerminalService, Prym.Web.Services.Store.PaymentTerminalService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<Prym.Web.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddScoped<Prym.Web.Services.Station.IStationService, Prym.Web.Services.Station.StationService>();

// Add authentication services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Initialize Translation Service and Tenant Context Service at startup
using (var scope = app.Services.CreateScope())
{
    var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();
    if (translationService is TranslationService concreteService)
    {
        await concreteService.InitializeAsync();
    }

    var tenantContextService = scope.ServiceProvider.GetRequiredService<ITenantContextService>();
    await tenantContextService.InitializeAsync();
}

await app.RunAsync();
