using EventForge.Client;
using EventForge.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient instances using best practices for performance
// Note: WebAssembly uses BrowserHttpHandler which doesn't support HttpClientHandler configuration
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
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
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<IPerformanceOptimizationService, PerformanceOptimizationService>();
builder.Services.AddScoped<OptimizedSignalRService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<IClientLogService, ClientLogService>();
builder.Services.AddScoped<IHelpService, HelpService>();
builder.Services.AddScoped<ILoadingDialogService, LoadingDialogService>();
builder.Services.AddScoped<IPrintingService, PrintingService>();

// Add SuperAdmin services
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<IEntityManagementService, EntityManagementService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();

// Add authentication services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
