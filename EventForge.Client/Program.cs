using EventForge.Client;
using EventForge.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Use component type from the current project namespace
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load API base URL from configuration (appsettings.json)
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

// Add memory cache for performance optimization
builder.Services.AddMemoryCache(options =>
{
    // Configure cache for mobile-optimized performance
    options.SizeLimit = 50 * 1024 * 1024; // 50MB limit for mobile devices
    options.CompactionPercentage = 0.25; // Remove 25% of cache when limit is reached
});

// Add centralized HTTP client service
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// Add custom services
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthenticationDialogService, AuthenticationDialogService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPerformanceOptimizationService, PerformanceOptimizationService>();
builder.Services.AddScoped<IRealtimeService, OptimizedSignalRService>();
// Register SignalRService for backward compatibility (marked as Obsolete)
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddScoped<SignalRService>();
#pragma warning restore CS0618 // Type or member is obsolete
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IInventorySessionService, InventorySessionService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();
builder.Services.AddScoped<IClientLogService, ClientLogService>();
builder.Services.AddScoped<IHelpService, HelpService>();
builder.Services.AddScoped<ILoadingDialogService, LoadingDialogService>();
builder.Services.AddScoped<IPrintingService, PrintingService>();
builder.Services.AddScoped<ITablePreferencesService, TablePreferencesService>();

// Add warehouse management services
builder.Services.AddScoped<ILotService, LotService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IStorageLocationService, StorageLocationService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ITransferOrderService, TransferOrderService>();

// Add product management services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierSuggestionService, SupplierSuggestionService>();
builder.Services.AddScoped<IUMService, UMService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<ILookupCacheService, LookupCacheService>();

// Add DevTools services
builder.Services.AddScoped<IDevToolsService, DevToolsService>();

// Add External services
builder.Services.AddScoped<EventForge.Client.Services.External.IVatLookupService, EventForge.Client.Services.External.VatLookupService>();

// Add ViewModels
builder.Services.AddScoped<EventForge.Client.ViewModels.ProductDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.InventoryDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.WarehouseDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.StorageLocationDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.LotDetailViewModel>();

// Onda 2: Documents & Financial ViewModels
builder.Services.AddScoped<EventForge.Client.ViewModels.DocumentTypeDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.DocumentHeaderDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.DocumentCounterDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.VatRateDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.VatNatureDetailViewModel>();
builder.Services.AddScoped<EventForge.Client.ViewModels.PaymentTermDetailViewModel>();

// Onda 3: BusinessParty ViewModels
builder.Services.AddScoped<EventForge.Client.ViewModels.BusinessPartyDetailViewModel>();

// Add dashboard configuration service
builder.Services.AddScoped<IDashboardConfigurationService, DashboardConfigurationService>();

// Add entity schema provider for dashboard metrics
builder.Services.AddScoped<EventForge.Client.Services.Schema.IEntitySchemaProvider, EventForge.Client.Services.Schema.EntitySchemaProvider>();

// Add document management services
builder.Services.AddScoped<IDocumentHeaderService, DocumentHeaderService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IDocumentCounterService, DocumentCounterService>();

// Add SuperAdmin services
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<ILogManagementService, LogManagementService>();
builder.Services.AddScoped<IEntityManagementService, EntityManagementService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IBusinessPartyService, BusinessPartyService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

// Add Event management services
builder.Services.AddScoped<IEventService, EventService>();

// Add Sales management services
builder.Services.AddScoped<EventForge.Client.Services.Sales.ISalesService, EventForge.Client.Services.Sales.SalesService>();
builder.Services.AddScoped<EventForge.Client.Services.Sales.IPaymentMethodService, EventForge.Client.Services.Sales.PaymentMethodService>();
builder.Services.AddScoped<EventForge.Client.Services.Sales.INoteFlagService, EventForge.Client.Services.Sales.NoteFlagService>();
builder.Services.AddScoped<EventForge.Client.Services.Sales.ITableManagementService, EventForge.Client.Services.Sales.TableManagementService>();

// Register AuthenticatedHttpClientHandler for Store services
builder.Services.AddTransient<EventForge.Client.Services.Http.AuthenticatedHttpClientHandler>();
// Register authenticated HTTP client handler for Store services
builder.Services.AddTransient<EventForge.Client.Services.Store.AuthenticatedHttpClientHandler>();

// Add Store management services with configured HttpClient and authentication handler
builder.Services.AddHttpClient<EventForge.Client.Services.Store.IStoreUserService, EventForge.Client.Services.Store.StoreUserService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<EventForge.Client.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<EventForge.Client.Services.Store.IStorePosService, EventForge.Client.Services.Store.StorePosService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<EventForge.Client.Services.Store.AuthenticatedHttpClientHandler>();

builder.Services.AddHttpClient<EventForge.Client.Services.Store.IStoreUserGroupService, EventForge.Client.Services.Store.StoreUserGroupService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<EventForge.Client.Services.Store.AuthenticatedHttpClientHandler>();

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
