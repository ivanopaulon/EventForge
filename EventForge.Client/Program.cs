using EventForge.Client;
using EventForge.Client.Services.Core;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Domain.Warehouse;
using EventForge.Client.Services.Domain.Products;
using EventForge.Client.Services.Domain.Documents;
using EventForge.Client.Services.Domain.Business;
using EventForge.Client.Services.Domain.Financial;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Features;
using EventForge.Client.Services.Admin;
using EventForge.Client.Services.Performance;
using EventForge.Client.Services.Sales;
using EventForge.Client.Services.Schema;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

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
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<IPerformanceOptimizationService, PerformanceOptimizationService>();
builder.Services.AddScoped<OptimizedSignalRService>();
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

// Add product management services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUMService, UMService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IModelService, ModelService>();

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
